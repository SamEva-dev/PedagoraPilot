using DomainRelay.Abstractions;
using PedagoraPilot.Application.Abstractions.Messaging;
using FluentValidation;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Training;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Domain.Access;
using PedagoraPilot.Security.Contracts;

namespace PedagoraPilot.Application.Access.Invitations;

public sealed record CreateAccessInvitationCommand(string FirstName, string LastName, string Email, string Role,
    Guid? SiteId = null, Guid? ProgramId = null, Guid? CohortId = null, Guid? ExamSessionId = null,
    string? Phone = null, DateOnly? BirthDate = null, DateOnly? EnrolledOn = null) : ICommand<CreateAccessInvitationResult>;
public sealed record CreateAccessInvitationResult(Guid Id, string Email, string FirstName, string LastName, string Role, string Status, string Token);

/// <summary>Validate the user-supplied attributes before persisting the outbox-backed invitation.</summary>
public sealed class CreateAccessInvitationCommandValidator : AbstractValidator<CreateAccessInvitationCommand>
{
    public CreateAccessInvitationCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Phone).MaximumLength(40);
        RuleFor(x => x.Role).NotEmpty();
        RuleFor(x => x.BirthDate).Must(x => !x.HasValue || x.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("ACCESS_INVITATION_BIRTH_DATE_INVALID");
    }
}

public sealed class CreateAccessInvitationCommandHandler(
    ICurrentUser current,
    IAccessInvitationRepository invitations,
    ICohortRepository cohorts,
    IProgramOfferingRepository offerings,
    IPersonRepository persons,
    ILearnerProfileRepository learnerProfiles,
    IEnrollmentRepository enrollments)
    : IRequestHandler<CreateAccessInvitationCommand, CreateAccessInvitationResult>
{
    public async Task<CreateAccessInvitationResult> Handle(CreateAccessInvitationCommand request, CancellationToken ct)
    {
        var organizationId = current.OrganizationId ?? throw new ForbiddenApplicationException("AUTH_FORBIDDEN");
        if (!current.UserId.HasValue || current.UserId.Value == Guid.Empty) throw new ForbiddenApplicationException("AUTH_FORBIDDEN");
        var role = NormalizeRole(request.Role) ?? throw new ValidationApplicationException("ACCESS_ROLE_INVALID");
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (email.Length == 0) throw new ValidationApplicationException("ACCESS_EMAIL_REQUIRED");
        if (await invitations.HasPendingAsync(organizationId, email, ct)) throw new ConflictApplicationException("ACCESS_INVITATION_ALREADY_EXISTS");

        // Cohort drives the tenant/site/program relationship. Never trust a client-supplied
        // assignment scope, and never persist an invitation pointing to another organization.
        Guid? siteId = request.SiteId;
        Guid? programId = request.ProgramId;
        if (request.CohortId.HasValue)
        {
            var cohort = await cohorts.GetByIdAsync(new CohortId(request.CohortId.Value), false, ct)
                ?? throw new NotFoundApplicationException("COHORT_NOT_FOUND");
            if (cohort.OrganizationId != organizationId)
                throw new ForbiddenApplicationException("AUTH_FORBIDDEN");
            if (cohort.Status is CohortStatus.Completed or CohortStatus.Cancelled)
                throw new ConflictApplicationException("COHORT_CLOSED");
            var offering = await offerings.GetByIdAsync(cohort.ProgramOfferingId, false, ct)
                ?? throw new NotFoundApplicationException("PROGRAM_OFFERING_NOT_FOUND");
            if (!offering.IsActive || offering.SiteId != cohort.SiteId)
                throw new ConflictApplicationException("ACCESS_INVITATION_SCOPE_INVALID");
            ContextualScope.EnsureCanManageCohort(current, cohort.SiteId, offering.ProgramId, cohort.Id.Value);

            // A new invitation must not silently re-enroll an existing learner. The
            // complete enrollment lifecycle (including reinstatement) has its own command.
            if (string.Equals(role, PedagoraPilotRoleCodes.Student, StringComparison.OrdinalIgnoreCase))
            {
                var person = await persons.GetByEmailAsync(email, false, ct);
                if (person is not null)
                {
                    var learner = await learnerProfiles.GetByPersonIdAsync(person.Id, false, ct);
                    if (learner is not null && await enrollments.GetByLearnerAndCohortAsync(learner.Id, cohort.Id, false, ct) is not null)
                        throw new ConflictApplicationException("LEARNER_ALREADY_ENROLLED");
                }
            }
            if (siteId.HasValue && siteId.Value != cohort.SiteId
                || programId.HasValue && programId.Value != offering.ProgramId)
                throw new ValidationApplicationException("ACCESS_INVITATION_SCOPE_INVALID");
            siteId = cohort.SiteId;
            programId = offering.ProgramId;
        }

        var (invitation, token) = AccessInvitation.Create(organizationId, email, request.FirstName, request.LastName, role,
            current.UserId.Value, 7, siteId, programId, request.CohortId, request.ExamSessionId,
            request.Phone, request.BirthDate, request.EnrolledOn);
        await invitations.AddAsync(invitation, ct);
        return new CreateAccessInvitationResult(invitation.Id.Value, invitation.Email, invitation.FirstName, invitation.LastName,
            invitation.RequestedRole, "Invited", token);
    }

    private static string? NormalizeRole(string? raw)
    {
        var value = (raw ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(value)) return null;
        var roles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["administrator"] = PedagoraPilotRoleCodes.OrganizationAdministrator,
            ["direction"] = PedagoraPilotRoleCodes.OrganizationDirection,
            ["site_direction"] = PedagoraPilotRoleCodes.SiteDirection,
            ["pedagogical_manager"] = PedagoraPilotRoleCodes.PedagogicalManager,
            ["secretariat"] = PedagoraPilotRoleCodes.Secretariat,
            ["trainer"] = PedagoraPilotRoleCodes.Trainer,
            ["formateur"] = PedagoraPilotRoleCodes.Trainer,
            ["student"] = PedagoraPilotRoleCodes.Student,
            ["stagiaire"] = PedagoraPilotRoleCodes.Student,
            ["jury"] = PedagoraPilotRoleCodes.Jury,
            ["readonly"] = PedagoraPilotRoleCodes.ReadOnly
        };
        if (roles.TryGetValue(value, out var mapped)) return mapped;
        return PedagoraPilotRoleCodes.All.Contains(value, StringComparer.OrdinalIgnoreCase) &&
               !string.Equals(value, PedagoraPilotRoleCodes.PlatformAdministrator, StringComparison.OrdinalIgnoreCase) ? value : null;
    }
}

public sealed record CancelAccessInvitationCommand(Guid InvitationId) : ICommand<bool>;
public sealed class CancelAccessInvitationCommandHandler(ICurrentUser current, IAccessInvitationRepository invitations)
    : IRequestHandler<CancelAccessInvitationCommand, bool>
{
    public async Task<bool> Handle(CancelAccessInvitationCommand request, CancellationToken ct)
    {
        var organizationId = current.OrganizationId ?? throw new ForbiddenApplicationException("AUTH_FORBIDDEN");
        var invitation = await invitations.GetByIdAsync(new AccessInvitationId(request.InvitationId), true, ct)
            ?? throw new NotFoundApplicationException("ACCESS_INVITATION_NOT_FOUND");
        if (invitation.OrganizationId != organizationId) throw new ForbiddenApplicationException("AUTH_FORBIDDEN");
        invitation.Cancel();
        return true;
    }
}
