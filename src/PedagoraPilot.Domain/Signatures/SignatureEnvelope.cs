using System.Security.Cryptography;
using System.Text;
using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Signatures.Events;

namespace PedagoraPilot.Domain.Signatures;

/// <summary>
/// Immutable, versioned source snapshot plus its acknowledgement lifecycle.
/// No unauthenticated public-signature endpoints are exposed in S14.
/// Sign means taking notice, not approving the trainer's assessment.
/// </summary>
public sealed class SignatureEnvelope : AggregateRoot<SignatureEnvelopeId>
{
    private SignatureEnvelope() { }

    private SignatureEnvelope(SignatureEnvelopeId id, Guid organizationId, Guid siteId, Guid cohortId,
        EnrollmentId enrollmentId, Guid signerAuthGateUserId, SignatureSourceType sourceType, Guid sourceId,
        long sourceVersion, string snapshotJson, string requestedByUserId, DateTimeOffset createdAtUtc) : base(id)
    {
        if (organizationId == Guid.Empty || siteId == Guid.Empty || cohortId == Guid.Empty || enrollmentId.IsEmpty)
            throw new DomainException("SIGNATURE_CONTEXT_INVALID");
        if (signerAuthGateUserId == Guid.Empty || string.IsNullOrWhiteSpace(requestedByUserId))
            throw new DomainException("SIGNATURE_SIGNER_REQUIRED");
        if (!Enum.IsDefined(sourceType) || sourceId == Guid.Empty || sourceVersion < 1)
            throw new DomainException("SIGNATURE_SOURCE_INVALID");
        if (string.IsNullOrWhiteSpace(snapshotJson) || Encoding.UTF8.GetByteCount(snapshotJson) > 128 * 1024)
            throw new DomainException("SIGNATURE_SNAPSHOT_INVALID");
        OrganizationId = organizationId;
        SiteId = siteId;
        CohortId = cohortId;
        EnrollmentId = enrollmentId;
        SignerAuthGateUserId = signerAuthGateUserId;
        SourceType = sourceType;
        SourceId = sourceId;
        SourceVersion = sourceVersion;
        SnapshotJson = snapshotJson;
        ContentSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshotJson))).ToLowerInvariant();
        RequestedByUserId = requestedByUserId.Trim();
        CreatedAtUtc = UpdatedAtUtc = createdAtUtc.UtcDateTime;
        Status = SignatureEnvelopeStatus.Draft;
        Version = 1;
    }

    public Guid OrganizationId { get; private set; }
    public Guid SiteId { get; private set; }
    public Guid CohortId { get; private set; }
    public EnrollmentId EnrollmentId { get; private set; }
    public Guid SignerAuthGateUserId { get; private set; }
    public SignatureSourceType SourceType { get; private set; }
    public Guid SourceId { get; private set; }
    public long SourceVersion { get; private set; }
    public string SnapshotJson { get; private set; } = string.Empty;
    public string ContentSha256 { get; private set; } = string.Empty;
    public string RequestedByUserId { get; private set; } = string.Empty;
    public SignatureEnvelopeStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public DateTimeOffset? RequestedAtUtc { get; private set; }
    public DateTimeOffset? ExpiresAtUtc { get; private set; }
    public DateTimeOffset? ViewedAtUtc { get; private set; }
    public DateTimeOffset? SignedAtUtc { get; private set; }
    public DateTimeOffset? DeclinedAtUtc { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public string? EvidenceReference { get; private set; }
    public string? ResponseComment { get; private set; }
    public long Version { get; private set; }
    public DateTimeOffset? LastReminderAtUtc { get; private set; }
    public int ReminderCount { get; private set; }
    // S18: a finalized response may have one immutable, integrity-checked private archive.
    public string? ArchivePdfStorageKey { get; private set; }
    public string? ArchivePdfSha256 { get; private set; }
    public string? ArchiveManifestStorageKey { get; private set; }
    public string? ArchiveManifestSha256 { get; private set; }
    public DateTimeOffset? ArchivedAtUtc { get; private set; }

    public static SignatureEnvelope CreateDraft(Guid organizationId, Guid siteId, Guid cohortId,
        EnrollmentId enrollmentId, Guid signerAuthGateUserId, SignatureSourceType sourceType, Guid sourceId,
        long sourceVersion, string snapshotJson, string requestedByUserId, DateTimeOffset createdAtUtc)
    {
        var envelope = new SignatureEnvelope(SignatureEnvelopeId.New(), organizationId, siteId, cohortId,
            enrollmentId, signerAuthGateUserId, sourceType, sourceId, sourceVersion, snapshotJson, requestedByUserId, createdAtUtc);
        envelope.RaiseDomainEvent(new SignatureDraftCreatedDomainEvent(envelope.Id, organizationId));
        return envelope;
    }

    // These transitions are internal domain capabilities only in S14.
    // S15/S16 will expose them after OTP, immutable rendering and proof retention are implemented.
    public void Request(DateTimeOffset atUtc, DateTimeOffset expiresAtUtc)
    {
        if (Status != SignatureEnvelopeStatus.Draft || expiresAtUtc <= atUtc)
            throw new DomainException("SIGNATURE_TRANSITION_FORBIDDEN");
        if (atUtc.ToUniversalTime().UtcDateTime < CreatedAtUtc)
            throw new DomainException("SIGNATURE_TIME_INVALID");
        RequestedAtUtc = atUtc.ToUniversalTime();
        ExpiresAtUtc = expiresAtUtc.ToUniversalTime();
        Transition(SignatureEnvelopeStatus.Pending, atUtc);
        RaiseDomainEvent(new SignatureRequestedDomainEvent(Id, OrganizationId));
    }

    /// <summary>A reminder is a new event, not a re-opening or new source snapshot.</summary>
    public void Remind(DateTimeOffset atUtc)
    {
        RequirePending(atUtc);
        var now = atUtc.ToUniversalTime();
        if (ReminderCount >= 3 || (LastReminderAtUtc.HasValue && now - LastReminderAtUtc.Value < TimeSpan.FromHours(24))
            || !RequestedAtUtc.HasValue || now - RequestedAtUtc.Value < TimeSpan.FromHours(24))
            throw new DomainException("SIGNATURE_REMINDER_RATE_LIMITED");
        ReminderCount++;
        LastReminderAtUtc = now;
        UpdatedAtUtc = now.UtcDateTime;
        Version++;
        RaiseDomainEvent(new SignatureReminderRequestedDomainEvent(Id, OrganizationId));
    }

    public void MarkViewed(DateTimeOffset atUtc)
    {
        RequirePending(atUtc);
        if (Status == SignatureEnvelopeStatus.Pending)
        {
            ViewedAtUtc = atUtc.ToUniversalTime();
            Transition(SignatureEnvelopeStatus.Viewed, atUtc);
        }
    }

    public void Sign(DateTimeOffset atUtc, string evidenceReference)
    {
        RequirePending(atUtc);
        if (string.IsNullOrWhiteSpace(evidenceReference) || evidenceReference.Length > 500)
            throw new DomainException("SIGNATURE_EVIDENCE_REQUIRED");
        EvidenceReference = evidenceReference.Trim();
        SignedAtUtc = atUtc.ToUniversalTime();
        Transition(SignatureEnvelopeStatus.Signed, atUtc);
        RaiseDomainEvent(new SignatureCompletedDomainEvent(Id, OrganizationId));
    }

    public void Decline(DateTimeOffset atUtc, string? comment)
    {
        RequirePending(atUtc);
        ResponseComment = NormalizeComment(comment);
        DeclinedAtUtc = atUtc.ToUniversalTime();
        Transition(SignatureEnvelopeStatus.Declined, atUtc);
        RaiseDomainEvent(new SignatureDeclinedDomainEvent(Id, OrganizationId));
    }

    public void RegisterArchive(string pdfStorageKey, string pdfSha256,
        string manifestStorageKey, string manifestSha256, DateTimeOffset archivedAtUtc)
    {
        if (Status is not (SignatureEnvelopeStatus.Signed or SignatureEnvelopeStatus.Declined))
            throw new DomainException("SIGNATURE_ARCHIVE_NOT_FINAL");
        if (ArchivedAtUtc.HasValue)
        {
            if (ArchivePdfStorageKey == pdfStorageKey && ArchivePdfSha256 == pdfSha256
                && ArchiveManifestStorageKey == manifestStorageKey && ArchiveManifestSha256 == manifestSha256)
                return; // Idempotent reprocessing of the exact same proof.
            throw new DomainException("SIGNATURE_ARCHIVE_IMMUTABLE");
        }
        if (string.IsNullOrWhiteSpace(pdfStorageKey) || string.IsNullOrWhiteSpace(manifestStorageKey)
            || !IsSha256(pdfSha256) || !IsSha256(manifestSha256)
            || archivedAtUtc.ToUniversalTime().UtcDateTime < CreatedAtUtc)
            throw new DomainException("SIGNATURE_ARCHIVE_INVALID");
        ArchivePdfStorageKey = pdfStorageKey;
        ArchivePdfSha256 = pdfSha256;
        ArchiveManifestStorageKey = manifestStorageKey;
        ArchiveManifestSha256 = manifestSha256;
        ArchivedAtUtc = archivedAtUtc.ToUniversalTime();
        UpdatedAtUtc = archivedAtUtc.UtcDateTime;
        Version++;
    }

    private static bool IsSha256(string? value) => value is { Length: 64 }
        && value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');

    public void Expire(DateTimeOffset atUtc)
    {
        if (Status is not (SignatureEnvelopeStatus.Pending or SignatureEnvelopeStatus.Viewed)
            || !ExpiresAtUtc.HasValue || atUtc.ToUniversalTime() < ExpiresAtUtc.Value)
            throw new DomainException("SIGNATURE_TRANSITION_FORBIDDEN");
        Transition(SignatureEnvelopeStatus.Expired, atUtc);
    }

    public void Cancel(DateTimeOffset atUtc)
    {
        if (Status is not (SignatureEnvelopeStatus.Draft or SignatureEnvelopeStatus.Pending or SignatureEnvelopeStatus.Viewed))
            throw new DomainException("SIGNATURE_TRANSITION_FORBIDDEN");
        if (atUtc.ToUniversalTime().UtcDateTime < CreatedAtUtc)
            throw new DomainException("SIGNATURE_TIME_INVALID");
        CancelledAtUtc = atUtc.ToUniversalTime();
        Transition(SignatureEnvelopeStatus.Cancelled, atUtc);
    }

    private void RequirePending(DateTimeOffset atUtc)
    {
        if (Status is not (SignatureEnvelopeStatus.Pending or SignatureEnvelopeStatus.Viewed))
            throw new DomainException("SIGNATURE_TRANSITION_FORBIDDEN");
        if (!RequestedAtUtc.HasValue || atUtc.ToUniversalTime() < RequestedAtUtc.Value)
            throw new DomainException("SIGNATURE_TIME_INVALID");
        if (!ExpiresAtUtc.HasValue || atUtc.ToUniversalTime() >= ExpiresAtUtc.Value)
            throw new DomainException("SIGNATURE_EXPIRED");
    }

    private void Transition(SignatureEnvelopeStatus next, DateTimeOffset atUtc)
    {
        Status = next;
        UpdatedAtUtc = atUtc.UtcDateTime;
        Version++;
    }

    private static string? NormalizeComment(string? text)
    {
        var trimmed = text?.Trim();
        if (trimmed?.Length > 2000) throw new DomainException("SIGNATURE_COMMENT_TOO_LONG");
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
