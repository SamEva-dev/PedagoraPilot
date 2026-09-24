using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Domain.Certification.Events;
public sealed record CertificationSchemeCreatedDomainEvent(CertificationSchemeId SchemeId, Guid ReferentialVersionId) : DomainEvent;
public sealed record CertificationSchemePublishedDomainEvent(CertificationSchemeId SchemeId, Guid ReferentialVersionId) : DomainEvent;
public sealed record CertificationSchemeArchivedDomainEvent(CertificationSchemeId SchemeId, Guid ReferentialVersionId) : DomainEvent;
public sealed record CertificationExamSessionCreatedDomainEvent(CertificationExamSessionId SessionId, Guid OrganizationId, CohortId CohortId) : DomainEvent;
public sealed record CertificationExamSessionPlannedDomainEvent(CertificationExamSessionId SessionId, Guid OrganizationId, CohortId CohortId) : DomainEvent;
public sealed record CertificationCandidateRegisteredDomainEvent(CertificationCandidateId CandidateId, Guid OrganizationId, CertificationExamSessionId SessionId, EnrollmentId EnrollmentId) : DomainEvent;
public sealed record CertificationEligibilityEvaluatedDomainEvent(CertificationCandidateId CandidateId, Guid OrganizationId, bool Eligible) : DomainEvent;
public sealed record CertificationAssessmentRecordedDomainEvent(CertificationCandidateId CandidateId, Guid OrganizationId, CertificationAssessmentId AssessmentId, CertificationStepDefinitionId StepId) : DomainEvent;
public sealed record CertificationDecisionRecordedDomainEvent(CertificationCandidateId CandidateId, Guid OrganizationId, CertificationDecision Decision) : DomainEvent;
public sealed record CertificationResultsPublishedDomainEvent(CertificationExamSessionId SessionId, Guid OrganizationId, CohortId CohortId) : DomainEvent;
