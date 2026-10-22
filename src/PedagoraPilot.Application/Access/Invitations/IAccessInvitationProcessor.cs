namespace PedagoraPilot.Application.Access.Invitations;

public interface IAccessInvitationProcessor
{
    Task ProcessCreatedAsync(Guid invitationId, string token, Guid eventId, CancellationToken ct = default);
    Task<Guid> AcceptAsync(string token, string? password, CancellationToken ct = default);
}
