using DomainRelay.Mapping.Abstractions.Configuration;
using DomainRelay.Mapping.Abstractions.Profiles;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Learning;

namespace PedagoraPilot.Application.Mapping;
public sealed class LearningProgressMappingProfile : MappingProfile
{
    public override void Configure(IMappingConfiguration configuration)
    {
        configuration.CreateMap<CompetencyDefinition, CompetencyDefinitionReadModel>();
        configuration.CreateMap<LearnerCompetencyRecord, LearnerCompetencyRecordReadModel>();
        configuration.CreateMap<PedagogicalTopic, PedagogicalTopicReadModel>();
        configuration.CreateMap<LearnerTopicProgress, LearnerTopicProgressReadModel>();
        configuration.CreateMap<DrivingEvaluation, DrivingEvaluationReadModel>();
    }
}

public sealed record CompetencyDefinitionReadModel
{
    public CompetencyDefinitionId Id { get; init; }
    public Guid ReferentialVersionId { get; init; }
    public CompetencyDefinitionId? ParentId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public CompetencyKind Kind { get; init; }
    public int SortOrder { get; init; }
    public bool Active { get; init; }
}

public sealed record LearnerCompetencyRecordReadModel
{
    public LearnerCompetencyRecordId Id { get; init; }
    public Guid OrganizationId { get; init; }
    public EnrollmentId EnrollmentId { get; init; }
    public CompetencyDefinitionId CompetencyDefinitionId { get; init; }
    public CompetencyLevel Level { get; init; }
    public decimal? Score { get; init; }
    public string? Comment { get; init; }
    public string? EvaluatorDisplayName { get; init; }
    public DateTimeOffset? EvaluatedAtUtc { get; init; }
}

public sealed record PedagogicalTopicReadModel
{
    public PedagogicalTopicId Id { get; init; }
    public Guid ReferentialVersionId { get; init; }
    public string Code { get; init; } = string.Empty;
    public int? Number { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public int DurationMinutes { get; init; }
    public string? Reference { get; init; }
    public bool Active { get; init; }
}

public sealed record LearnerTopicProgressReadModel
{
    public LearnerTopicProgressId Id { get; init; }
    public Guid OrganizationId { get; init; }
    public EnrollmentId EnrollmentId { get; init; }
    public PedagogicalTopicId TopicId { get; init; }
    public TopicProgressStatus Status { get; init; }
    public DateOnly? PreparationDate { get; init; }
    public DateOnly? PresentationDate { get; init; }
    public int? PresentationDurationMinutes { get; init; }
    public string? EvaluatorDisplayName { get; init; }
    public string? Comment { get; init; }
}

public sealed record DrivingEvaluationReadModel
{
    public DrivingEvaluationId Id { get; init; }
    public Guid OrganizationId { get; init; }
    public EnrollmentId EnrollmentId { get; init; }
    public CompetencyDefinitionId CompetencyDefinitionId { get; init; }
    public TrainingSessionId? TrainingSessionId { get; init; }
    public DateTimeOffset EvaluatedAtUtc { get; init; }
    public string? TrainerAuthGateUserId { get; init; }
    public string TrainerDisplayName { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string? Positive { get; init; }
    public string? Difficulty { get; init; }
    public string? NextGoal { get; init; }
    public string? FreeObservation { get; init; }
}
