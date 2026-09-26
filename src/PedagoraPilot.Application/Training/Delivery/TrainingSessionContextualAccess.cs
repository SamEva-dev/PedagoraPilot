using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Domain.Training.Delivery;

namespace PedagoraPilot.Application.Training.Delivery;

internal static class TrainingSessionContextualAccess
{
    public static async Task EnsureAsync(
        TrainingSession session,
        ICohortRepository cohorts,
        IProgramOfferingRepository offerings,
        ICurrentUser current,
        bool manage,
        CancellationToken ct)
    {
        var cohort = await cohorts.GetByIdAsync(session.CohortId, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
        if (manage)
            await ContextualScope.EnsureCanManageCohortAsync(current, cohort, offerings, ct);
        else
            await ContextualScope.EnsureCanViewCohortAsync(current, cohort, offerings, ct);
    }

    public static async Task<List<TrainingSession>> FilterAsync(
        IEnumerable<TrainingSession> source,
        ICohortRepository cohorts,
        IProgramOfferingRepository offerings,
        ICurrentUser current,
        CancellationToken ct)
    {
        var items = source.ToList();
        if (!current.HasContextualScopeRestrictions || items.Count == 0)
            return items;

        var cohortIds = items.Select(x => x.CohortId).Distinct().ToArray();
        var cohortRows = await cohorts.Query(false)
            .Where(x => cohortIds.Contains(x.Id))
            .ToListAsync(ct);
        var cohortById = cohortRows.ToDictionary(x => x.Id);
        var offeringIds = cohortRows.Select(x => x.ProgramOfferingId).Distinct().ToArray();
        var programByOffering = await offerings.Query(false)
            .Where(x => offeringIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.ProgramId, ct);

        return items.Where(session =>
            cohortById.TryGetValue(session.CohortId, out var cohort)
            && programByOffering.TryGetValue(cohort.ProgramOfferingId, out var programId)
            && current.CanViewCohort(cohort.SiteId, programId, cohort.Id.Value)).ToList();
    }
}
