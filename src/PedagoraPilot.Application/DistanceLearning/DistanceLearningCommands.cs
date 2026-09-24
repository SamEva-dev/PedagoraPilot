using PedagoraPilot.Application.Abstractions.Messaging;
using PedagoraPilot.Contracts.DistanceLearning;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Application.DistanceLearning;
public sealed record CreateDistanceLearningSessionCommand(Guid SiteId, Guid ProgramId, CohortId CohortId, string Title, string TrainerDisplayName, string? TrainerEmail, DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc, string Platform, string JoinUrl, string? Objectives) : ICommand<DistanceLearningSessionDto>;
public sealed record AddDistanceParticipantCommand(DistanceLearningSessionId SessionId, EnrollmentId EnrollmentId, string DisplayName) : ICommand<DistanceLearningSessionDto>;
public sealed record ChangeDistanceSessionStatusCommand(DistanceLearningSessionId SessionId, string Status) : ICommand<DistanceLearningSessionDto>;
public sealed record UpdateDistanceAttendanceCommand(DistanceLearningSessionId SessionId, DistanceParticipantId ParticipantId, string Attendance, DateTimeOffset? ConnectedAtUtc, DateTimeOffset? DisconnectedAtUtc, int ConnectedMinutes, int ParticipationPercent, int CompletedActivities, int ActivityCount) : ICommand<DistanceLearningSessionDto>;
public sealed record CreateAsyncLearningModuleCommand(Guid SiteId, Guid ProgramId, CohortId CohortId, string Title, string? Description, int EstimatedMinutes, DateOnly DueDate, string TrainerDisplayName, int ExpectedStudents, IReadOnlyCollection<CreateAsyncModuleStepRequest>? Steps) : ICommand<AsyncLearningModuleDto>;
public sealed record UpdateAsyncModuleProgressCommand(AsyncLearningModuleId ModuleId, int ProgressPercent, int CompletedStudents, decimal? AverageScore) : ICommand<AsyncLearningModuleDto>;
