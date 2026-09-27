using DomainRelay.Abstractions;
using PedagoraPilot.Application.Abstractions.Messaging;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Domain.Access;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Security.Contracts;

namespace PedagoraPilot.Application.Access.Invitations;

public sealed record CreateAccessInvitationCommand(string FirstName, string LastName, string Email, string Role,
    Guid? SiteId = null, Guid? ProgramId = null, Guid? CohortId = null, Guid? ExamSessionId = null) : ICommand<CreateAccessInvitationResult>;
public sealed record CreateAccessInvitationResult(Guid Id, string Email, string FirstName, string LastName, string Role, string Status, string Token);

public sealed class CreateAccessInvitationCommandHandler(ICurrentUser current, IAccessInvitationRepository invitations)
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

        var (invitation, token) = AccessInvitation.Create(organizationId, email, request.FirstName, request.LastName, role,
            current.UserId.Value, 7, request.SiteId, request.ProgramId, request.CohortId, request.ExamSessionId);
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
