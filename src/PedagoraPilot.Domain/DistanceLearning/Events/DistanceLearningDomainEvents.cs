using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Domain.DistanceLearning.Events;
public sealed record DistanceLearningSessionCreatedDomainEvent(DistanceLearningSessionId SessionId, Guid OrganizationId, Guid SiteId, Guid ProgramId, CohortId CohortId, string? RecipientEmail) : DomainEvent;
public sealed record DistanceLearningSessionStatusChangedDomainEvent(DistanceLearningSessionId SessionId, Guid OrganizationId, Guid SiteId, Guid ProgramId, CohortId CohortId, DistanceLearningSessionStatus Status) : DomainEvent;
public sealed record DistanceParticipantAddedDomainEvent(DistanceLearningSessionId SessionId, DistanceParticipantId ParticipantId, Guid OrganizationId, Guid SiteId, Guid ProgramId, CohortId CohortId, EnrollmentId EnrollmentId) : DomainEvent;
public sealed record DistanceParticipantAttendanceUpdatedDomainEvent(DistanceLearningSessionId SessionId, DistanceParticipantId ParticipantId, Guid OrganizationId, Guid SiteId, Guid ProgramId, CohortId CohortId, DistanceAttendanceStatus Attendance) : DomainEvent;
public sealed record AsyncLearningModuleCreatedDomainEvent(AsyncLearningModuleId ModuleId, Guid OrganizationId, CohortId CohortId) : DomainEvent;
public sealed record AsyncLearningModuleProgressChangedDomainEvent(AsyncLearningModuleId ModuleId, Guid OrganizationId, int ProgressPercent) : DomainEvent;
