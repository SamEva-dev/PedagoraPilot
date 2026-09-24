using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Domain.Organizations;

namespace PedagoraPilot.Infrastructure.Persistence.Repositories;
public sealed class OrganizationRepository : Repository<Organization>, IOrganizationRepository
{
    public OrganizationRepository(PedagoraPilotDbContext dbContext) : base(dbContext)
    {
    }

    public Task<Organization?> GetByOwnerUserIdAsync(Guid ownerUserId, bool isTracking = false, CancellationToken cancellationToken = default) => Query(isTracking).SingleOrDefaultAsync(x => x.OwnerUserId == ownerUserId, cancellationToken);
    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default) => Query(isTracking: false).AnyAsync(x => x.Code.Value == code, cancellationToken);
}
