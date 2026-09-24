using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Domain.DistanceLearning.Events;
public sealed record DistanceLearningSessionCreatedDomainEvent(DistanceLearningSessionId SessionId, Guid OrganizationId, CohortId CohortId, string? RecipientEmail) : DomainEvent;
public sealed record DistanceLearningSessionStatusChangedDomainEvent(DistanceLearningSessionId SessionId, Guid OrganizationId, DistanceLearningSessionStatus Status) : DomainEvent;
public sealed record DistanceParticipantAttendanceUpdatedDomainEvent(DistanceLearningSessionId SessionId, DistanceParticipantId ParticipantId, Guid OrganizationId, DistanceAttendanceStatus Attendance) : DomainEvent;
public sealed record AsyncLearningModuleCreatedDomainEvent(AsyncLearningModuleId ModuleId, Guid OrganizationId, CohortId CohortId) : DomainEvent;
public sealed record AsyncLearningModuleProgressChangedDomainEvent(AsyncLearningModuleId ModuleId, Guid OrganizationId, int ProgressPercent) : DomainEvent;
