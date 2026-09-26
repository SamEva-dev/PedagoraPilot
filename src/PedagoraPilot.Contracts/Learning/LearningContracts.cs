namespace PedagoraPilot.Contracts.Learning;

public sealed record CompetencyDefinitionDto(Guid Id, Guid ReferentialVersionId, Guid? ParentId, string Code, string Title, string Kind, int SortOrder, bool Active);
public sealed record LearnerCompetencyDto(Guid Id, Guid EnrollmentId, Guid CompetencyDefinitionId, string Code, string Title, string Level, decimal? Score, string? Comment, string? EvaluatorDisplayName, DateTimeOffset? EvaluatedAtUtc);
public sealed record CohortCompetencyRowDto(Guid EnrollmentId, string FirstName, string LastName, IReadOnlyCollection<LearnerCompetencyDto> Competencies);
public sealed record EvaluateCompetencyRequest(string Level, decimal? Score, string? Comment, Guid? EvaluatorAuthGateUserId, string? EvaluatorDisplayName, DateTimeOffset? EvaluatedAtUtc);

public sealed record PedagogicalTopicDto(Guid Id, Guid ReferentialVersionId, string Code, int? Number, string Title, string Category, int DurationMinutes, string? Reference, bool Active, string? Objective, string? Example, string? Correction);
public sealed record SavePedagogicalTopicRequest(int Number, string Title, string Category, int DurationMinutes, string? Reference, bool Active, string? Objective, string? Example, string? Correction);
public sealed record TopicEvaluationCriterionRequest(string Code, string Level);
public sealed record TopicEvaluationCriterionDto(Guid Id, string Code, string Level);
public sealed record LearnerTopicProgressDto(
    Guid Id,
    Guid EnrollmentId,
    Guid TopicId,
    string Code,
    int? Number,
    string Title,
    string Category,
    string Status,
    DateOnly? PreparationDate,
    DateOnly? PresentationDate,
    int? PresentationDurationMinutes,
    string? EvaluatorDisplayName,
    string? PositivePoints,
    string? Improvements,
    string? Comment,
    string? NextObjective,
    IReadOnlyCollection<TopicEvaluationCriterionDto> EvaluationCriteria);
public sealed record UpdateTopicProgressRequest(
    string Status,
    DateOnly? PreparationDate,
    DateOnly? PresentationDate,
    int? PresentationDurationMinutes,
    string? EvaluatorDisplayName,
    string? PositivePoints,
    string? Improvements,
    string? Comment,
    string? NextObjective,
    IReadOnlyCollection<TopicEvaluationCriterionRequest>? EvaluationCriteria);

public sealed record DrivingCriterionRequest(string Code, string Label, string Level);
public sealed record DrivingCriterionDto(Guid Id, string Code, string Label, string Level);
public sealed record RecordDrivingEvaluationRequest(Guid CompetencyDefinitionId, Guid? TrainingSessionId, DateTimeOffset EvaluatedAtUtc, string? TrainerAuthGateUserId, string TrainerDisplayName, string Subject, string? Positive, string? Difficulty, string? NextGoal, string? FreeObservation, IReadOnlyCollection<DrivingCriterionRequest> Criteria);
public sealed record DrivingEvaluationDto(Guid Id, Guid EnrollmentId, Guid CompetencyDefinitionId, Guid? TrainingSessionId, DateTimeOffset EvaluatedAtUtc, string? TrainerAuthGateUserId, string TrainerDisplayName, string Subject, string? Positive, string? Difficulty, string? NextGoal, string? FreeObservation, IReadOnlyCollection<DrivingCriterionDto> Criteria);
