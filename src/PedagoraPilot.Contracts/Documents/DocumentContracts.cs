namespace PedagoraPilot.Contracts.Documents;
public sealed record DocumentVersionDto(Guid Id, int VersionNumber, string FileName, string ContentType, long SizeBytes, string Sha256, bool BlobAvailable, string SecurityStatus, string UploadedByDisplayName, DateTimeOffset UploadedAtUtc);
public sealed record DocumentDto(Guid Id, Guid OrganizationId, Guid? SiteId, Guid? ProgramId, Guid? CohortId, string OwnerType, Guid? OwnerId, string Title, string? Description, string Category, string Visibility, string Status, string CreatedByDisplayName, DateTime CreatedAtUtc, DateTime UpdatedAtUtc, IReadOnlyCollection<DocumentVersionDto> Versions);
public sealed record UpdateDocumentMetadataRequest(string Title, string? Description, string Category, string Visibility);
public sealed record DocumentDownloadDto(string FileName, string ContentType, long SizeBytes, Stream Content);
