namespace PedagoraPilot.Contracts.Signatures;

public sealed record SignatureCenterItemDto(SignatureEnvelopeDto Envelope, DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset? LastReminderAtUtc, int ReminderCount, string? ResponseComment, string LearnerName);
public sealed record SignatureCenterPageDto(IReadOnlyCollection<SignatureCenterItemDto> Items,
    int Total, int Page, int PageSize, IReadOnlyDictionary<string, int> StatusCounts);
public sealed record SignatureBatchRequest(Guid[] EnvelopeIds);
public sealed record SignatureBatchResultDto(IReadOnlyCollection<SignatureEnvelopeDto> Envelopes);
public sealed record CreateDailyObservationSummaryRequest(Guid EnrollmentId, DateOnly Date, Guid[] EvaluationIds);
