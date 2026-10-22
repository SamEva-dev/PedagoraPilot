using PedagoraPilot.Domain.Branding;
namespace PedagoraPilot.Application.Abstractions.Persistence;
public interface IBrandingRepository
{
    Task<OrganizationBranding?> GetStateAsync(Guid organizationId, bool isTracking = false, CancellationToken ct = default);
    Task<BrandingRevision?> GetRevisionAsync(Guid organizationId, Guid revisionId, CancellationToken ct = default);
    Task<BrandingAsset?> GetAssetAsync(Guid organizationId, Guid assetId, CancellationToken ct = default);
    Task<bool> OrganizationExistsAsync(Guid organizationId, CancellationToken ct = default);
    void Add(OrganizationBranding state);
    void Add(BrandingRevision revision);
    void Add(BrandingAsset asset);
}
