using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Domain.Learning.Events;
public sealed record LearnerCompetencyEvaluatedDomainEvent(LearnerCompetencyRecordId RecordId, EnrollmentId EnrollmentId, CompetencyDefinitionId CompetencyDefinitionId, Guid OrganizationId) : DomainEvent;
public sealed record LearnerTopicProgressUpdatedDomainEvent(LearnerTopicProgressId ProgressId, EnrollmentId EnrollmentId, PedagogicalTopicId TopicId, Guid OrganizationId) : DomainEvent;
public sealed record DrivingEvaluationRecordedDomainEvent(DrivingEvaluationId EvaluationId, EnrollmentId EnrollmentId, Guid OrganizationId) : DomainEvent;
