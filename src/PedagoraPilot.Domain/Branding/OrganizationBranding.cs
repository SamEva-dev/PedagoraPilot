namespace PedagoraPilot.Domain.Branding;

public sealed class OrganizationBranding
{
    private OrganizationBranding() { }
    public Guid OrganizationId { get; private set; }
    public Guid? PublishedRevisionId { get; private set; }
    public Guid? DraftRevisionId { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public static OrganizationBranding Create(Guid organizationId) => new() { OrganizationId = organizationId, UpdatedAtUtc = DateTime.UtcNow };
    public void SetDraft(Guid revisionId) { DraftRevisionId = revisionId; UpdatedAtUtc = DateTime.UtcNow; }
    public void Publish(Guid revisionId) { if (DraftRevisionId != revisionId) throw new InvalidOperationException("branding.draft_mismatch"); PublishedRevisionId = revisionId; UpdatedAtUtc = DateTime.UtcNow; }
    public void ResetToNativeTheme() { PublishedRevisionId = null; DraftRevisionId = null; UpdatedAtUtc = DateTime.UtcNow; }
    public void Restore(Guid revisionId) { PublishedRevisionId = revisionId; UpdatedAtUtc = DateTime.UtcNow; }
}

public sealed class BrandingRevision
{
    private BrandingRevision() { }
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string ConfigurationJson { get; private set; } = "{}";
    public DateTime CreatedAtUtc { get; private set; }
    public static BrandingRevision Create(Guid organizationId, string json) => new() { Id=Guid.NewGuid(), OrganizationId=organizationId, ConfigurationJson=json, CreatedAtUtc=DateTime.UtcNow };
}

public sealed class BrandingAsset
{
    private BrandingAsset() { }
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string StorageKey { get; private set; } = "";
    public string ContentType { get; private set; } = "";
    public long SizeBytes { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public static BrandingAsset Create(Guid organizationId, string key, string type, long size) => new() { Id=Guid.NewGuid(), OrganizationId=organizationId, StorageKey=key, ContentType=type, SizeBytes=size, CreatedAtUtc=DateTime.UtcNow };
}
