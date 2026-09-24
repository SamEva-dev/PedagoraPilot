using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Training;

namespace PedagoraPilot.Domain.Training.Events;
public sealed record CohortCreatedDomainEvent(CohortId CohortId, Guid OrganizationId, Guid ProgramOfferingId) : DomainEvent;
public sealed record CohortUpdatedDomainEvent(CohortId CohortId, Guid OrganizationId) : DomainEvent;
public sealed record EnrollmentCreatedDomainEvent(EnrollmentId EnrollmentId, LearnerProfileId LearnerProfileId, CohortId CohortId, Guid OrganizationId) : DomainEvent;
public sealed record EnrollmentStatusChangedDomainEvent(EnrollmentId EnrollmentId, Guid OrganizationId, EnrollmentStatus Status) : DomainEvent;
