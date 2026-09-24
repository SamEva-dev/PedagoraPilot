using DomainRelay.Mapping.Abstractions.Configuration;
using DomainRelay.Mapping.Abstractions.Profiles;
using PedagoraPilot.Domain.Documents;

namespace PedagoraPilot.Application.Mapping;
public sealed class DocumentMappingProfile : MappingProfile
{
    public override void Configure(IMappingConfiguration configuration)
    {
        configuration.CreateMap<ManagedDocument, ManagedDocumentReadModel>();
        configuration.CreateMap<DocumentVersion, DocumentVersionReadModel>();
    }
}

public sealed class ManagedDocumentReadModel
{
    public PedagoraPilot.Domain.Identifiers.DocumentId Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? SiteId { get; set; }
    public Guid? ProgramId { get; set; }
    public Guid? CohortId { get; set; }
    public DocumentOwnerType OwnerType { get; set; }
    public Guid? OwnerId { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public DocumentCategory Category { get; set; }
    public DocumentVisibility Visibility { get; set; }
    public DocumentStatus Status { get; set; }
    public string CreatedByDisplayName { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class DocumentVersionReadModel
{
    public PedagoraPilot.Domain.Identifiers.DocumentVersionId Id { get; set; }
    public int VersionNumber { get; set; }
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }
    public string Sha256 { get; set; } = "";
    public bool BlobAvailable { get; set; }
    public DocumentSecurityStatus SecurityStatus { get; set; }
    public string UploadedByDisplayName { get; set; } = "";
    public DateTimeOffset UploadedAtUtc { get; set; }
}
