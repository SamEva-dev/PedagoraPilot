using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Learning;

namespace PedagoraPilot.Application.Abstractions.Persistence;
public interface ICompetencyDefinitionRepository : IRepository<CompetencyDefinition, CompetencyDefinitionId>
{
    Task<CompetencyDefinition?> FindByCodeAsync(Guid referentialVersionId, string code, bool isTracking = false, CancellationToken ct = default);
}

public interface ILearnerCompetencyRecordRepository : IRepository<LearnerCompetencyRecord, LearnerCompetencyRecordId>
{
    Task<LearnerCompetencyRecord?> FindAsync(EnrollmentId enrollmentId, CompetencyDefinitionId competencyDefinitionId, bool isTracking = false, CancellationToken ct = default);
}

public interface IPedagogicalTopicRepository : IRepository<PedagogicalTopic, PedagogicalTopicId>
{
}

public interface ILearnerTopicProgressRepository : IRepository<LearnerTopicProgress, LearnerTopicProgressId>
{
    Task<LearnerTopicProgress?> FindAsync(EnrollmentId enrollmentId, PedagogicalTopicId topicId, bool isTracking = false, CancellationToken ct = default);
}

public interface IDrivingEvaluationRepository : IRepository<DrivingEvaluation, DrivingEvaluationId>
{
}
