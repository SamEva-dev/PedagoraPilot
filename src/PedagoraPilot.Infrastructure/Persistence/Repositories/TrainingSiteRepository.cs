using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Domain.Organizations;

namespace PedagoraPilot.Infrastructure.Persistence.Repositories;
public sealed class TrainingSiteRepository : Repository<TrainingSite>, ITrainingSiteRepository
{
    public TrainingSiteRepository(PedagoraPilotDbContext db) : base(db)
    {
    }

    public IQueryable<TrainingSite> QueryByOrganization(Guid organizationId, bool isTracking = false) => Query(isTracking).Where(x => x.OrganizationId == organizationId);
    public Task<bool> CodeExistsAsync(Guid organizationId, string code, CancellationToken ct = default)
    {
        var n = code.Trim().ToUpperInvariant();
        return Query(false).AnyAsync(x => x.OrganizationId == organizationId && x.Code.Value == n, ct);
    }
}
