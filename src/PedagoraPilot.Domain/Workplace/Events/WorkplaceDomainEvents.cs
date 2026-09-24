using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Domain.Workplace.Events;
public sealed record WorkplacePeriodCreatedDomainEvent(WorkplacePeriodId PeriodId, Guid OrganizationId, EnrollmentId EnrollmentId, CohortId CohortId) : DomainEvent;
public sealed record WorkplacePeriodUpdatedDomainEvent(WorkplacePeriodId PeriodId, Guid OrganizationId) : DomainEvent;
public sealed record WorkplaceHoursUpdatedDomainEvent(WorkplacePeriodId PeriodId, Guid OrganizationId, int CompletedMinutes) : DomainEvent;
public sealed record WorkplaceActivityUpdatedDomainEvent(WorkplacePeriodId PeriodId, Guid OrganizationId, WorkplaceActivityId ActivityId, WorkplaceActivityStatus Status) : DomainEvent;
public sealed record WorkplaceDocumentStatusUpdatedDomainEvent(WorkplacePeriodId PeriodId, Guid OrganizationId, WorkplaceDocumentChecklistItemId ItemId, WorkplaceDocumentStatus Status) : DomainEvent;
public sealed record WorkplaceEvaluationRecordedDomainEvent(WorkplacePeriodId PeriodId, Guid OrganizationId, WorkplaceEvaluationId EvaluationId, WorkplaceEvaluationKind Kind) : DomainEvent;
