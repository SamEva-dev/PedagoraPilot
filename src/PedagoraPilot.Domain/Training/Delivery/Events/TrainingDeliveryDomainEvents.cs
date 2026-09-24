using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Domain.Training.Delivery.Events;
public sealed record TrainingSessionCreatedDomainEvent(TrainingSessionId SessionId, Guid OrganizationId, CohortId CohortId) : DomainEvent;
public sealed record TrainingSessionUpdatedDomainEvent(TrainingSessionId SessionId, Guid OrganizationId) : DomainEvent;
public sealed record TrainingSessionCancelledDomainEvent(TrainingSessionId SessionId, Guid OrganizationId) : DomainEvent;
public sealed record AttendanceSheetInitializedDomainEvent(AttendanceSheetId AttendanceSheetId, TrainingSessionId SessionId, Guid OrganizationId) : DomainEvent;
public sealed record AttendanceRecordedDomainEvent(AttendanceSheetId AttendanceSheetId, TrainingSessionId SessionId, Guid OrganizationId) : DomainEvent;
