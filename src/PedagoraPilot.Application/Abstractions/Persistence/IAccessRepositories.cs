using PedagoraPilot.Domain.Access;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Application.Abstractions.Persistence;

public interface IOrganizationMemberRepository : IRepository<OrganizationMember, OrganizationMemberId>
{
    Task<OrganizationMember?> GetByAuthGateUserAsync(Guid organizationId, Guid authGateUserId, bool isTracking = false, CancellationToken cancellationToken = default);
    Task SyncRoleProfilesAsync(OrganizationMember member, IReadOnlyCollection<string> roles, bool active, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<MemberDirectoryEntry>> GetTrainerDirectoryAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<MemberDirectoryEntry>> GetStudentDirectoryAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<string>> GetRoleCodesAsync(Guid organizationId, Guid authGateUserId, CancellationToken cancellationToken = default);
    Task<int> CountActiveRoleAsync(Guid organizationId, string roleCode, CancellationToken cancellationToken = default);
}

public sealed record MemberDirectoryEntry(Guid AuthGateUserId, string Email, string FirstName, string LastName, string DisplayName, IReadOnlyCollection<string> Roles);

public interface IAccessInvitationRepository : IRepository<PedagoraPilot.Domain.Access.AccessInvitation, PedagoraPilot.Domain.Identifiers.AccessInvitationId>
{
    Task<PedagoraPilot.Domain.Access.AccessInvitation?> GetByTokenHashAsync(string tokenHash, bool isTracking = false, CancellationToken cancellationToken = default);
    Task AcquireCreationLockAsync(Guid organizationId, string normalizedEmail, CancellationToken cancellationToken = default);
    Task<bool> HasPendingAsync(Guid organizationId, string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PedagoraPilot.Domain.Access.AccessInvitation>> ListForOrganizationAsync(Guid organizationId, bool isTracking = false, CancellationToken cancellationToken = default);
}
