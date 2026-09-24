using DomainRelay.Abstractions;
using PedagoraPilot.Contracts.DistanceLearning;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Application.DistanceLearning;
public sealed record GetDistanceLearningSessionsQuery(CohortId? CohortId) : IRequest<IReadOnlyCollection<DistanceLearningSessionDto>>;
public sealed record GetAsyncLearningModulesQuery(CohortId? CohortId) : IRequest<IReadOnlyCollection<AsyncLearningModuleDto>>;
