namespace PedagoraPilot.Contracts.Signatures;

/// <summary>Allowed source types: pedagogical-observation, attendance.</summary>
public sealed record CreateSignatureDraftRequest(string SourceType, Guid SourceId, Guid EnrollmentId);

public sealed record SignatureEnvelopeDto(Guid Id, Guid OrganizationId, Guid CohortId, Guid EnrollmentId,
    string SourceType, Guid SourceId, long SourceVersion, string Status, string ContentSha256,
    DateTime CreatedAtUtc, DateTimeOffset? RequestedAtUtc, DateTimeOffset? SignedAtUtc, long Version, DateTimeOffset? ArchivedAtUtc = null);

/// <summary>Authenticated view of a frozen observation, NEVER raw private data in an email.</summary>
public sealed record SignatureContentDto(SignatureEnvelopeDto Envelope, string SnapshotJson,
    DateTimeOffset? ExpiresAtUtc, DateTimeOffset? ViewedAtUtc, string? ResponseComment, bool CanRespond);
public sealed record SignatureAcknowledgementRequest(bool Acknowledge, string? Comment);

/// <summary>Only own recorded observations, with live enrollment references; never mock trainees.</summary>
public sealed record DailyObservationSignatureCandidateDto(Guid SourceId, Guid EnrollmentId, string LearnerName,
    string Subject, DateTimeOffset EvaluatedAtUtc, string SignatureStatus);
