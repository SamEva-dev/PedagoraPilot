using PedagoraPilot.Application.Abstractions.Messaging;
using PedagoraPilot.Contracts.Training;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Application.Training.Learners;
public sealed record GetCohortLearnersQuery(CohortId CohortId) : IQuery<IReadOnlyCollection<LearnerDto>>;
public sealed record GetLearnerQuery(LearnerProfileId LearnerProfileId) : IQuery<LearnerDto>;
public sealed record GetEnrollmentLearnerQuery(EnrollmentId EnrollmentId) : IQuery<LearnerDto>;
public sealed record GetSelfLearnerQuery() : IQuery<LearnerDto>;
