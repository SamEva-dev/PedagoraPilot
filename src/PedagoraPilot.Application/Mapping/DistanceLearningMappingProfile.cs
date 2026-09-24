using DomainRelay.Mapping.Abstractions.Configuration;
using DomainRelay.Mapping.Abstractions.Profiles;
using PedagoraPilot.Domain.DistanceLearning;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Application.Mapping;
public sealed class DistanceLearningMappingProfile : MappingProfile
{
    public override void Configure(IMappingConfiguration configuration)
    {
        configuration.CreateMap<DistanceLearningSession, DistanceLearningSessionReadModel>();
        configuration.CreateMap<DistanceParticipant, DistanceParticipantReadModel>();
        configuration.CreateMap<AsyncLearningModule, AsyncLearningModuleReadModel>();
        configuration.CreateMap<AsyncModuleStep, AsyncModuleStepReadModel>();
    }
}

public sealed class DistanceLearningSessionReadModel
{
    public DistanceLearningSessionId Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid SiteId { get; set; }
    public Guid ProgramId { get; set; }
    public CohortId CohortId { get; set; }
    public string Title { get; set; } = "";
    public string TrainerDisplayName { get; set; } = "";
    public string? TrainerEmail { get; set; }
    public DateTimeOffset StartsAtUtc { get; set; }
    public DateTimeOffset EndsAtUtc { get; set; }
    public DistancePlatform Platform { get; set; }
    public string JoinUrl { get; set; } = "";
    public string? Objectives { get; set; }
    public DistanceLearningSessionStatus Status { get; set; }
}

public sealed class DistanceParticipantReadModel
{
    public DistanceParticipantId Id { get; set; }
    public EnrollmentId EnrollmentId { get; set; }
    public string DisplayName { get; set; } = "";
    public DistanceAttendanceStatus Attendance { get; set; }
    public DateTimeOffset? ConnectedAtUtc { get; set; }
    public DateTimeOffset? DisconnectedAtUtc { get; set; }
    public int ConnectedMinutes { get; set; }
    public int ParticipationPercent { get; set; }
    public int CompletedActivities { get; set; }
    public int ActivityCount { get; set; }
}

public sealed class AsyncLearningModuleReadModel
{
    public AsyncLearningModuleId Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid SiteId { get; set; }
    public Guid ProgramId { get; set; }
    public CohortId CohortId { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public int EstimatedMinutes { get; set; }
    public DateOnly DueDate { get; set; }
    public string TrainerDisplayName { get; set; } = "";
    public AsyncLearningModuleStatus Status { get; set; }
    public int ProgressPercent { get; set; }
    public int CompletedStudents { get; set; }
    public int ExpectedStudents { get; set; }
    public decimal? AverageScore { get; set; }
}

public sealed class AsyncModuleStepReadModel
{
    public AsyncModuleStepId Id { get; set; }
    public string Code { get; set; } = "";
    public string Label { get; set; } = "";
    public int SortOrder { get; set; }
}
