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

/// <summary>Live per-attendee hour reconciliation for the current saved attendance revision.
/// Signed confirmation of a previous revision never confirms changed hours.</summary>
public sealed record AttendanceHoursRowDto(Guid EnrollmentId, int RealizedMinutes, int ConfirmedMinutes,
    int AwaitingMinutes, string ConfirmationStatus);
public sealed record AttendanceHoursSummaryDto(Guid SessionId, long SheetVersion, int RealizedMinutes,
    int ConfirmedMinutes, int AwaitingMinutes, IReadOnlyCollection<AttendanceHoursRowDto> Rows);

/// <summary>Own attendance totals for the current organization. Pedagogical progress is separate.</summary>
public sealed record LearnerAttendanceHoursDto(int RealizedMinutes, int ConfirmedMinutes, int AwaitingMinutes);
