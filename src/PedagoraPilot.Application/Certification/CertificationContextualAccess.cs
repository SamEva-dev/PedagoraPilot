using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Domain.Certification;

namespace PedagoraPilot.Application.Certification;

internal static class CertificationContextualAccess
{
    public static async Task EnsureSessionAsync(
        CertificationExamSession session,
        ICohortRepository cohorts,
        IProgramOfferingRepository offerings,
        ICurrentUser current,
        bool manage,
        CancellationToken ct)
    {
        var cohort = await cohorts.GetByIdAsync(session.CohortId, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
        var offering = await offerings.GetByIdAsync(cohort.ProgramOfferingId, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.ProgramOfferingNotFound);
        if (manage)
            ContextualScope.EnsureCanManageExam(current, cohort.SiteId, offering.ProgramId, cohort.Id.Value, session.Id.Value);
        else
            ContextualScope.EnsureCanViewExam(current, cohort.SiteId, offering.ProgramId, cohort.Id.Value, session.Id.Value);
    }

    public static async Task<List<CertificationExamSession>> FilterSessionsAsync(
        IEnumerable<CertificationExamSession> source,
        ICohortRepository cohorts,
        IProgramOfferingRepository offerings,
        ICurrentUser current,
        CancellationToken ct)
    {
        var items = source.ToList();
        if (!current.HasContextualScopeRestrictions || items.Count == 0)
            return items;
        var cohortIds = items.Select(x => x.CohortId).Distinct().ToArray();
        var cohortRows = await cohorts.Query(false).Where(x => cohortIds.Contains(x.Id)).ToListAsync(ct);
        var cohortById = cohortRows.ToDictionary(x => x.Id);
        var offeringIds = cohortRows.Select(x => x.ProgramOfferingId).Distinct().ToArray();
        var programByOffering = await offerings.Query(false)
            .Where(x => offeringIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.ProgramId, ct);
        return items.Where(session =>
            cohortById.TryGetValue(session.CohortId, out var cohort)
            && programByOffering.TryGetValue(cohort.ProgramOfferingId, out var programId)
            && current.CanViewExam(cohort.SiteId, programId, cohort.Id.Value, session.Id.Value)).ToList();
    }
}
