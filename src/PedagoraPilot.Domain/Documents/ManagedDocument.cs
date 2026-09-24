using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Documents.Events;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Domain.Documents;
public sealed class ManagedDocument : AggregateRoot<DocumentId>
{
    private readonly List<DocumentVersion> _versions = [];
    private ManagedDocument()
    {
    }

    private ManagedDocument(DocumentId id, Guid organizationId, Guid? siteId, Guid? programId, Guid? cohortId, DocumentOwnerType ownerType, Guid? ownerId, string title, string? description, DocumentCategory category, DocumentVisibility visibility, string createdByUserId, string createdByDisplayName) : base(id)
    {
        if (organizationId == Guid.Empty)
            throw new DomainException("DOCUMENT_ORGANIZATION_REQUIRED");
        if (ownerType != DocumentOwnerType.None && !ownerId.HasValue)
            throw new DomainException("DOCUMENT_OWNER_REQUIRED");
        OrganizationId = organizationId;
        SiteId = siteId;
        ProgramId = programId;
        CohortId = cohortId;
        OwnerType = ownerType;
        OwnerId = ownerId;
        Title = Required(title, "DOCUMENT_TITLE_REQUIRED", 300);
        Description = Optional(description, 4000);
        Category = category;
        Visibility = visibility;
        Status = DocumentStatus.Active;
        CreatedByUserId = Required(createdByUserId, "DOCUMENT_CREATED_BY_REQUIRED", 250);
        CreatedByDisplayName = Required(createdByDisplayName, "DOCUMENT_CREATED_BY_REQUIRED", 250);
        CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
        Version = 1;
    }

    public Guid OrganizationId { get; private set; }
    public Guid? SiteId { get; private set; }
    public Guid? ProgramId { get; private set; }
    public Guid? CohortId { get; private set; }
    public DocumentOwnerType OwnerType { get; private set; }
    public Guid? OwnerId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DocumentCategory Category { get; private set; }
    public DocumentVisibility Visibility { get; private set; }
    public DocumentStatus Status { get; private set; }
    public string CreatedByUserId { get; private set; } = string.Empty;
    public string CreatedByDisplayName { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }
    public IReadOnlyCollection<DocumentVersion> Versions => _versions.AsReadOnly();
    public DocumentVersion? CurrentVersion => _versions.OrderByDescending(x => x.VersionNumber).FirstOrDefault();

    public static ManagedDocument Create(Guid organizationId, Guid? siteId, Guid? programId, Guid? cohortId, DocumentOwnerType ownerType, Guid? ownerId, string title, string? description, DocumentCategory category, DocumentVisibility visibility, string createdByUserId, string createdByDisplayName)
    {
        var document = new ManagedDocument(DocumentId.New(), organizationId, siteId, programId, cohortId, ownerType, ownerId, title, description, category, visibility, createdByUserId, createdByDisplayName);
        document.RaiseDomainEvent(new DocumentCreatedDomainEvent(document.Id, organizationId));
        return document;
    }

    public DocumentVersion AddVersion(string fileName, string contentType, long sizeBytes, string sha256, string storageKey, bool blobAvailable, DocumentSecurityStatus securityStatus, string uploadedByUserId, string uploadedByDisplayName)
    {
        EnsureActive();
        if (sizeBytes <= 0)
            throw new DomainException("DOCUMENT_FILE_EMPTY");
        if (string.IsNullOrWhiteSpace(sha256))
            throw new DomainException("DOCUMENT_HASH_REQUIRED");
        if (string.IsNullOrWhiteSpace(storageKey))
            throw new DomainException("DOCUMENT_STORAGE_KEY_REQUIRED");
        var version = new DocumentVersion(DocumentVersionId.New(), Id, _versions.Count == 0 ? 1 : _versions.Max(x => x.VersionNumber) + 1, Required(fileName, "DOCUMENT_FILE_NAME_REQUIRED", 255), Required(contentType, "DOCUMENT_CONTENT_TYPE_REQUIRED", 200), sizeBytes, sha256, storageKey, blobAvailable, securityStatus, Required(uploadedByUserId, "DOCUMENT_CREATED_BY_REQUIRED", 250), Required(uploadedByDisplayName, "DOCUMENT_CREATED_BY_REQUIRED", 250), DateTimeOffset.UtcNow);
        _versions.Add(version);
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new DocumentVersionAddedDomainEvent(Id, version.Id, OrganizationId));
        return version;
    }

    public void UpdateMetadata(string title, string? description, DocumentCategory category, DocumentVisibility visibility)
    {
        EnsureActive();
        Title = Required(title, "DOCUMENT_TITLE_REQUIRED", 300);
        Description = Optional(description, 4000);
        Category = category;
        Visibility = visibility;
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new DocumentMetadataUpdatedDomainEvent(Id, OrganizationId));
    }

    public void Delete()
    {
        EnsureActive();
        Status = DocumentStatus.Deleted;
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new DocumentDeletedDomainEvent(Id, OrganizationId));
    }

    private void EnsureActive()
    {
        if (Status == DocumentStatus.Deleted)
            throw new DomainException("DOCUMENT_DELETED_LOCKED");
    }

    private static string Required(string? value, string key, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(key);
        var trimmed = value.Trim();
        if (trimmed.Length > max)
            throw new DomainException(key);
        return trimmed;
    }

    private static string? Optional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}
