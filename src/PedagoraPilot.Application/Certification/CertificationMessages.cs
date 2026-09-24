using DomainRelay.Abstractions;
using PedagoraPilot.Contracts.Certification;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Application.Certification;
public sealed record GetCertificationSchemesQuery(Guid? ReferentialVersionId) : IRequest<IReadOnlyCollection<CertificationSchemeDto>>;
public sealed record GetCertificationExamSessionsQuery(CohortId? CohortId) : IRequest<IReadOnlyCollection<CertificationExamSessionDto>>;
public sealed record GetCertificationCandidatesQuery(CertificationExamSessionId SessionId) : IRequest<IReadOnlyCollection<CertificationCandidateDto>>;
public sealed record CreateCertificationExamSessionCommand(CohortId CohortId, CertificationSchemeId SchemeId, string Title, DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc, string? Venue) : IRequest<CertificationExamSessionDto>;
public sealed record PlanCertificationExamSessionCommand(CertificationExamSessionId SessionId) : IRequest<CertificationExamSessionDto>;
public sealed record RegisterCohortCandidatesCommand(CertificationExamSessionId SessionId) : IRequest<int>;
public sealed record EvaluateCertificationEligibilityCommand(CertificationCandidateId CandidateId, bool Eligible, IReadOnlyCollection<string> Blockers) : IRequest<CertificationCandidateDto>;
public sealed record RecordCertificationAssessmentCommand(CertificationCandidateId CandidateId, CertificationStepDefinitionId StepDefinitionId, string JuryDisplayName, string Outcome, decimal? Score, string? Comment) : IRequest<CertificationCandidateDto>;
public sealed record RecordCertificationDecisionCommand(CertificationCandidateId CandidateId, string Decision, string? Comment) : IRequest<CertificationCandidateDto>;
public sealed record PublishCertificationResultsCommand(CertificationExamSessionId SessionId) : IRequest<CertificationExamSessionDto>;
public sealed record AssignJuryCommand(CertificationExamSessionId SessionId, Guid AuthGateUserId, string DisplayName, string Role) : IRequest<JuryAssignmentDto>;
