using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Learning;

namespace PedagoraPilot.Infrastructure.Persistence.Repositories;
public sealed class CompetencyDefinitionRepository(PedagoraPilotDbContext db) : Repository<CompetencyDefinition, CompetencyDefinitionId>(db), ICompetencyDefinitionRepository
{
    public Task<CompetencyDefinition?> FindByCodeAsync(Guid referentialVersionId, string code, bool isTracking = false, CancellationToken ct = default) => Query(isTracking).SingleOrDefaultAsync(x => x.ReferentialVersionId == referentialVersionId && x.Code == code.ToUpper(), ct);
}

public sealed class LearnerCompetencyRecordRepository(PedagoraPilotDbContext db) : Repository<LearnerCompetencyRecord, LearnerCompetencyRecordId>(db), ILearnerCompetencyRecordRepository
{
    public Task<LearnerCompetencyRecord?> FindAsync(EnrollmentId enrollmentId, CompetencyDefinitionId competencyDefinitionId, bool isTracking = false, CancellationToken ct = default) => Query(isTracking).SingleOrDefaultAsync(x => x.EnrollmentId == enrollmentId && x.CompetencyDefinitionId == competencyDefinitionId, ct);
}

public sealed class PedagogicalTopicRepository(PedagoraPilotDbContext db) : Repository<PedagogicalTopic, PedagogicalTopicId>(db), IPedagogicalTopicRepository
{
}

public sealed class LearnerTopicProgressRepository(PedagoraPilotDbContext db) : Repository<LearnerTopicProgress, LearnerTopicProgressId>(db), ILearnerTopicProgressRepository
{
    public Task<LearnerTopicProgress?> FindAsync(EnrollmentId enrollmentId, PedagogicalTopicId topicId, bool isTracking = false, CancellationToken ct = default) =>
        Query(isTracking).Include(x => x.EvaluationCriteria).SingleOrDefaultAsync(x => x.EnrollmentId == enrollmentId && x.TopicId == topicId, ct);
}

public sealed class DrivingEvaluationRepository(PedagoraPilotDbContext db) : Repository<DrivingEvaluation, DrivingEvaluationId>(db), IDrivingEvaluationRepository
{
}
