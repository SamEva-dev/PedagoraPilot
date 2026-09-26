using PedagoraPilot.Application.Abstractions.Messaging;
using PedagoraPilot.Contracts.Learning;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Application.Learning.Progress;
public sealed record GetCompetencyDefinitionsQuery(Guid ReferentialVersionId) : IQuery<IReadOnlyCollection<CompetencyDefinitionDto>>;
public sealed record GetLearnerCompetenciesQuery(EnrollmentId EnrollmentId) : IQuery<IReadOnlyCollection<LearnerCompetencyDto>>;
public sealed record GetCohortCompetenciesQuery(CohortId CohortId) : IQuery<IReadOnlyCollection<CohortCompetencyRowDto>>;
public sealed record GetPedagogicalTopicsQuery(Guid ReferentialVersionId) : IQuery<IReadOnlyCollection<PedagogicalTopicDto>>;
public sealed record GetPedagogicalTopicCatalogQuery(Guid ReferentialVersionId) : IQuery<IReadOnlyCollection<PedagogicalTopicDto>>;
public sealed record GetLearnerTopicsQuery(EnrollmentId EnrollmentId) : IQuery<IReadOnlyCollection<LearnerTopicProgressDto>>;
public sealed record GetDrivingEvaluationsQuery(EnrollmentId EnrollmentId) : IQuery<IReadOnlyCollection<DrivingEvaluationDto>>;
