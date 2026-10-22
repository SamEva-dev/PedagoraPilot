using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Domain.Branding;
namespace PedagoraPilot.Infrastructure.Persistence.Repositories;
public sealed class BrandingRepository(PedagoraPilotDbContext db) : IBrandingRepository
{
    public Task<OrganizationBranding?> GetStateAsync(Guid id, bool isTracking = false, CancellationToken ct = default) => (isTracking ? db.OrganizationBrandings : db.OrganizationBrandings.AsNoTracking()).SingleOrDefaultAsync(x => x.OrganizationId == id, ct);
    public Task<BrandingRevision?> GetRevisionAsync(Guid organizationId, Guid revisionId, CancellationToken ct = default) => db.BrandingRevisions.AsNoTracking().SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == revisionId, ct);
    public Task<BrandingAsset?> GetAssetAsync(Guid organizationId, Guid assetId, CancellationToken ct = default) => db.BrandingAssets.AsNoTracking().SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == assetId, ct);
    public Task<bool> OrganizationExistsAsync(Guid id, CancellationToken ct = default) => db.Organizations.AsNoTracking().AnyAsync(x => x.Id == id, ct);
    public void Add(OrganizationBranding state) => db.OrganizationBrandings.Add(state);
    public void Add(BrandingRevision revision) => db.BrandingRevisions.Add(revision);
    public void Add(BrandingAsset asset) => db.BrandingAssets.Add(asset);
}
