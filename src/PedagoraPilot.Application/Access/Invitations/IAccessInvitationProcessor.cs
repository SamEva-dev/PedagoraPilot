namespace PedagoraPilot.Application.Access.Invitations;

public sealed record AcceptedAccessInvitationResult(Guid UserId, string Email);

public interface IAccessInvitationProcessor
{
    Task ProcessCreatedAsync(Guid invitationId, string token, Guid eventId, CancellationToken ct = default);
    Task<AcceptedAccessInvitationResult> AcceptAsync(string token, string? password, CancellationToken ct = default);
}
