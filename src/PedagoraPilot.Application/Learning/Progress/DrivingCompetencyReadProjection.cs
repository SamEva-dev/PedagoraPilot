using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Contracts.Learning;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Learning;

namespace PedagoraPilot.Application.Learning.Progress;

// Read-only compatibility projection for driving evaluations recorded before
// competency records were synchronized. Never mutates state during GET.
internal static class DrivingCompetencyReadProjection
{
    internal static async Task<Dictionary<(EnrollmentId, CompetencyDefinitionId), LearnerCompetencyDto>> BuildAsync(
        IDrivingEvaluationRepository evaluations,
        Guid organizationId,
        IReadOnlyCollection<EnrollmentId> enrollmentIds,
        IReadOnlyCollection<CompetencyDefinition> definitions,
        CancellationToken ct)
    {
        var result = new Dictionary<(EnrollmentId, CompetencyDefinitionId), LearnerCompetencyDto>();
        if (enrollmentIds.Count == 0 || definitions.Count == 0) return result;
        var byCode = definitions.GroupBy(d => d.Code, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() == 1)
            .ToDictionary(g => g.Key, g => g.Single(), StringComparer.OrdinalIgnoreCase);
        var ids = enrollmentIds.ToArray();
        var rows = await evaluations.Query(false)
            .Where(e => e.OrganizationId == organizationId && ids.Contains(e.EnrollmentId))
            .Include(e => e.Criteria)
            .OrderByDescending(e => e.EvaluatedAtUtc)
            .ToListAsync(ct);
        foreach (var evaluation in rows)
        {
            foreach (var criterion in evaluation.Criteria)
            {
                if (!byCode.TryGetValue(criterion.Code, out var definition)) continue;
                var key = (evaluation.EnrollmentId, definition.Id);
                result.TryAdd(key, new LearnerCompetencyDto(
                    Guid.Empty, evaluation.EnrollmentId.Value, definition.Id.Value,
                    definition.Code, definition.Title,
                    LearningProgressDtoFactory.LevelCode(criterion.Level),
                    null, null, evaluation.TrainerDisplayName, evaluation.EvaluatedAtUtc));
            }
        }
        return result;
    }
}
