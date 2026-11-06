using DomainRelay.Abstractions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Messaging;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Domain.Access;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Workplace;
using PedagoraPilot.Security.Contracts;

namespace PedagoraPilot.Application.Workplace;

public sealed record ReassignWorkplaceTutorCommand(Guid PeriodId, string TutorName, string TutorEmail, string? TutorPhone)
    : ICommand<bool>;

public sealed class ReassignWorkplaceTutorCommandValidator : AbstractValidator<ReassignWorkplaceTutorCommand>
{
    public ReassignWorkplaceTutorCommandValidator()
    {
        RuleFor(x => x.PeriodId).NotEmpty();
        RuleFor(x => x.TutorName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TutorEmail).NotEmpty().EmailAddress().MaximumLength(250);
        RuleFor(x => x.TutorPhone).MaximumLength(60);
    }
}

// UnitOfWorkBehavior commits period, revoked assignments and new invitation together.
public sealed class ReassignWorkplaceTutorCommandHandler(
    IWorkplacePeriodRepository periods,
    IWorkplaceTutorAssignmentRepository assignments,
    IAccessInvitationRepository invitations,
    IOrganizationMemberRepository members,
    ICurrentUser current,
    WorkplaceContextualAccess access)
    : IRequestHandler<ReassignWorkplaceTutorCommand, bool>
{
    public async Task<bool> Handle(ReassignWorkplaceTutorCommand r, CancellationToken ct)
    {
        if (current.UserId is not Guid inviter || inviter == Guid.Empty)
            throw new ForbiddenApplicationException(ErrorKeys.WorkplaceForbidden);
        var period = await periods.Query(true)
            .SingleOrDefaultAsync(x => x.Id == new WorkplacePeriodId(r.PeriodId), ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.WorkplacePeriodNotFound);
        if (period.Status is WorkplacePeriodStatus.Cancelled or WorkplacePeriodStatus.Archived or WorkplacePeriodStatus.Deleted or WorkplacePeriodStatus.Completed)
            throw new ConflictApplicationException("WORKPLACE_PERIOD_LOCKED");
        TenantScope.Ensure(current, period.OrganizationId);
        await access.EnsureAsync(period, manage: true, ct);
        WorkplaceDtoFactory.EnsureTrainerVisibility(period, current);
        var email = r.TutorEmail.Trim().ToLowerInvariant();
        if (string.Equals(email, current.Email?.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new ConflictApplicationException("ACCESS_CANNOT_INVITE_SELF");
        // Lock by PERIOD before the email lock to prevent two concurrent reassignments
        // to different email addresses from leaving two live tutors on one period.
        await invitations.AcquireCreationLockAsync(period.OrganizationId, $"workplace-period:{r.PeriodId:N}", ct);
        await invitations.AcquireCreationLockAsync(period.OrganizationId, email, ct);
        var history = await assignments.GetByPeriodAsync(period.OrganizationId, r.PeriodId, ct);
        if (history.Any(x => x.Status != WorkplaceTutorAssignmentStatus.Revoked
            && string.Equals(x.InvitedEmail, email, StringComparison.OrdinalIgnoreCase)))
            throw new ConflictApplicationException("WORKPLACE_TUTOR_ALREADY_ASSIGNED");
        var knownMember = await ExistingTutorMembership.ResolveAsync(members, assignments, period.OrganizationId, email, ct);
        Guid? invitationId = null;
        if (knownMember is null)
        {
            var pending = await invitations.ListForOrganizationAsync(period.OrganizationId, false, ct);
            var existing = pending.FirstOrDefault(x => string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase));
            if (existing is not null && !string.Equals(existing.RequestedRole, PedagoraPilotRoleCodes.InternshipTutor, StringComparison.OrdinalIgnoreCase))
                throw new ConflictApplicationException("ACCESS_INVITATION_ALREADY_EXISTS");
            invitationId = existing?.Id.Value;
            if (!invitationId.HasValue)
            {
                var parts = r.TutorName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                var (invitation, _) = AccessInvitation.Create(period.OrganizationId, email,
                    parts[0], parts.Length > 1 ? parts[1] : "Stage",
                    PedagoraPilotRoleCodes.InternshipTutor, inviter, cohortId: period.CohortId.Value, phone: r.TutorPhone);
                await invitations.AddAsync(invitation, ct);
                invitationId = invitation.Id.Value;
            }
        }
        // Revoke first, then grant exactly one replacement within the UoW transaction.
        foreach (var previous in history.Where(x => x.Status != WorkplaceTutorAssignmentStatus.Revoked))
            previous.Revoke();
        if (knownMember is not null)
            await assignments.AddAsync(WorkplaceTutorAssignment.AssignExisting(period.OrganizationId, r.PeriodId,
                knownMember.AuthGateUserId, knownMember.Email), ct);
        else
            await assignments.AddAsync(WorkplaceTutorAssignment.Invite(period.OrganizationId, r.PeriodId, invitationId!.Value, email), ct);
        // Old invitations must not be accepted once all their assignments were revoked.
        foreach (var oldInvitationId in history.Where(x => x.InvitationId.HasValue)
                     .Select(x => x.InvitationId!.Value).Distinct())
        {
            var associated = await assignments.GetByInvitationAsync(period.OrganizationId, oldInvitationId, ct);
            if (associated.All(x => x.Status != WorkplaceTutorAssignmentStatus.Pending))
            {
                var stale = await invitations.GetByIdAsync(new AccessInvitationId(oldInvitationId), true, ct);
                if (stale?.Status == AccessInvitationStatus.Pending) stale.Cancel();
            }
        }
        period.UpdateDetails(period.Company, period.City, r.TutorName, email, r.TutorPhone,
            period.StartDate, period.EndDate, period.PlannedMinutes,
            period.TrainerVisible, period.Notes);
        return true;
    }
}
