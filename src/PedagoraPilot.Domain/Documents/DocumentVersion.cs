using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Domain.Documents;
public sealed class DocumentVersion : Entity<DocumentVersionId>
{
    private DocumentVersion()
    {
    }

    internal DocumentVersion(DocumentVersionId id, DocumentId documentId, int versionNumber, string fileName, string contentType, long sizeBytes, string sha256, string storageKey, bool blobAvailable, DocumentSecurityStatus securityStatus, string uploadedByUserId, string uploadedByDisplayName, DateTimeOffset uploadedAtUtc) : base(id)
    {
        DocumentId = documentId;
        VersionNumber = versionNumber;
        FileName = fileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        Sha256 = sha256;
        StorageKey = storageKey;
        BlobAvailable = blobAvailable;
        SecurityStatus = securityStatus;
        UploadedByUserId = uploadedByUserId;
        UploadedByDisplayName = uploadedByDisplayName;
        UploadedAtUtc = uploadedAtUtc;
    }

    public DocumentId DocumentId { get; private set; }
    public int VersionNumber { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string Sha256 { get; private set; } = string.Empty;
    public string StorageKey { get; private set; } = string.Empty;
    public bool BlobAvailable { get; private set; }
    public DocumentSecurityStatus SecurityStatus { get; private set; }
    public string UploadedByUserId { get; private set; } = string.Empty;
    public string UploadedByDisplayName { get; private set; } = string.Empty;
    public DateTimeOffset UploadedAtUtc { get; private set; }

    internal void MarkBlobMissing() => BlobAvailable = false;
}
