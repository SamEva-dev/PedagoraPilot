namespace PedagoraPilot.Application.Abstractions.Identity;

public sealed record EnsureApplicationIdentityRequest(
    Guid OrganizationId,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    string? InitialPassword,
    bool EmailConfirmed);

public sealed record EnsureApplicationIdentityResult(Guid UserId, bool ExistingIdentity, bool AccessActive);

public sealed record SetApplicationRoleRequest(
    Guid OrganizationId,
    Guid UserId,
    string Role);

public interface IApplicationIdentityAccess
{
    Task<bool> ExistingActiveIdentityAsync(string email, CancellationToken cancellationToken = default);
    Task<EnsureApplicationIdentityResult> EnsureAsync(EnsureApplicationIdentityRequest request, CancellationToken cancellationToken = default);
    Task SetRoleAsync(SetApplicationRoleRequest request, CancellationToken cancellationToken = default);
}
