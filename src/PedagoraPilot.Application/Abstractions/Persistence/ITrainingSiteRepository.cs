using PedagoraPilot.Domain.Organizations;

namespace PedagoraPilot.Application.Abstractions.Persistence;
public interface ITrainingSiteRepository : IRepository<TrainingSite>
{
    Task<bool> CodeExistsAsync(Guid organizationId, string code, CancellationToken cancellationToken = default);
    IQueryable<TrainingSite> QueryByOrganization(Guid organizationId, bool isTracking = false);
}
