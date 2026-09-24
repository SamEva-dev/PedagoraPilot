using PedagoraPilot.Domain.Organizations;

namespace PedagoraPilot.Application.Abstractions.Persistence;
public interface IOrganizationRepository : IRepository<Organization>
{
    Task<Organization?> GetByOwnerUserIdAsync(Guid ownerUserId, bool isTracking = false, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);
}
