using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Contracts.Reporting;
using PedagoraPilot.Domain.Certification;
using PedagoraPilot.Domain.Training.Delivery;
using PedagoraPilot.Domain.Workplace;

namespace PedagoraPilot.Infrastructure.Persistence.Repositories;
public sealed class ReportingReadRepository(PedagoraPilotDbContext db) : IReportingReadRepository
{
    public async Task<OrganizationDashboardDto?> GetOrganizationDashboardAsync(Guid organizationId, CancellationToken ct = default)
    {
        var exists = await db.Organizations.AsNoTracking().AnyAsync(x => x.Id == organizationId, ct);
        if (!exists)
            return null;
        var siteIds = await db.TrainingSites.AsNoTracking().Where(x => x.OrganizationId == organizationId).Select(x => x.Id).ToListAsync(ct);
        var cohorts = await db.Cohorts.AsNoTracking().Where(x => x.OrganizationId == organizationId).Select(x => new { x.Id, x.SiteId }).ToListAsync(ct);
        var cohortIds = cohorts.Select(x => x.Id).ToList();
        var learners = await db.Enrollments.AsNoTracking().CountAsync(x => cohortIds.Contains(x.CohortId), ct);
        var activePrograms = await db.ProgramOfferings.AsNoTracking().Where(x => siteIds.Contains(x.SiteId) && x.IsActive).Select(x => x.ProgramId).Distinct().CountAsync(ct);
        return new(organizationId, siteIds.Count, activePrograms, cohorts.Count, learners, await AttendanceRateAsync(organizationId, cohortIds, ct), await AverageProgressAsync(organizationId, cohortIds, ct), await CertificationSuccessAsync(organizationId, cohortIds, ct), 0);
    }

    public async Task<SiteDashboardDto?> GetSiteDashboardAsync(Guid siteId, Guid? organizationScope, CancellationToken ct = default)
    {
        var site = await db.TrainingSites.AsNoTracking().Where(x => x.Id == siteId && (!organizationScope.HasValue || x.OrganizationId == organizationScope.Value)).Select(x => new { x.Id, x.OrganizationId, x.Name }).SingleOrDefaultAsync(ct);
        if (site is null)
            return null;
        var cohortIds = await db.Cohorts.AsNoTracking().Where(x => x.SiteId == siteId).Select(x => x.Id).ToListAsync(ct);
        var learners = await db.Enrollments.AsNoTracking().CountAsync(x => cohortIds.Contains(x.CohortId), ct);
        return new(site.Id, site.Name, cohortIds.Count, learners, await AttendanceRateAsync(site.OrganizationId, cohortIds, ct), await AverageProgressAsync(site.OrganizationId, cohortIds, ct), await CertificationSuccessAsync(site.OrganizationId, cohortIds, ct));
    }

    public async Task<CohortDashboardDto?> GetCohortDashboardAsync(Guid cohortId, Guid? organizationScope, CancellationToken ct = default)
    {
        var cohort = await db.Cohorts.AsNoTracking().Where(x => x.Id.Value == cohortId && (!organizationScope.HasValue || x.OrganizationId == organizationScope.Value)).Select(x => new { x.Id, x.OrganizationId, x.Code, x.Name }).SingleOrDefaultAsync(ct);
        if (cohort is null)
            return null;
        var cohortIds = new[]
        {
            cohort.Id
        };
        var enrollmentIds = await db.Enrollments.AsNoTracking().Where(x => x.CohortId == cohort.Id).Select(x => x.Id).ToListAsync(ct);
        var sessions = await db.TrainingSessions.AsNoTracking().Where(x => x.CohortId == cohort.Id).Select(x => new { x.StartsAtUtc, x.EndsAtUtc, x.Status }).ToListAsync(ct);
        var plannedMinutes = (int)sessions.Sum(x => Math.Max(0, (x.EndsAtUtc - x.StartsAtUtc).TotalMinutes));
        var deliveredMinutes = (int)sessions.Where(x => x.Status == TrainingSessionStatus.Completed).Sum(x => Math.Max(0, (x.EndsAtUtc - x.StartsAtUtc).TotalMinutes));
        var attendanceRows = await db.AttendanceSheets.AsNoTracking().Where(x => x.CohortId == cohort.Id).SelectMany(x => x.Entries.Select(e => new { e.ExpectedMinutes, e.PresentMinutes })).ToListAsync(ct);
        var expected = attendanceRows.Sum(x => x.ExpectedMinutes);
        var present = attendanceRows.Sum(x => x.PresentMinutes);
        var workplaceCompleted = await db.WorkplacePeriods.AsNoTracking().CountAsync(x => enrollmentIds.Contains(x.EnrollmentId) && x.Status == WorkplacePeriodStatus.Completed, ct);
        var candidates = await db.CertificationCandidates.AsNoTracking().Where(x => enrollmentIds.Contains(x.EnrollmentId)).Select(x => new { x.Eligible, x.Decision }).ToListAsync(ct);
        return new(cohortId, cohort.Code, cohort.Name, enrollmentIds.Count, plannedMinutes, deliveredMinutes, present, expected == 0 ? 0m : Math.Round(present * 100m / expected, 2), await AverageProgressAsync(cohort.OrganizationId, cohortIds, ct), workplaceCompleted, candidates.Count(x => x.Eligible == true), candidates.Count(x => x.Decision == CertificationDecision.Obtained), 0);
    }

    public async Task<IReadOnlyCollection<ReportingTrendPointDto>> GetAttendanceTrendAsync(Guid cohortId, Guid? organizationScope, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var fromUtc = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        var toUtc = new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        var sessions = db.TrainingSessions.AsNoTracking().Where(x => x.CohortId.Value == cohortId && (!organizationScope.HasValue || x.OrganizationId == organizationScope.Value) && x.StartsAtUtc >= fromUtc && x.StartsAtUtc < toUtc).Select(x => new { x.Id, x.StartsAtUtc });
        var raw = await db.AttendanceSheets.AsNoTracking().Where(x => x.CohortId.Value == cohortId).Join(sessions, sheet => sheet.SessionId, session => session.Id, (sheet, session) => new { sheet, session }).SelectMany(x => x.sheet.Entries.Select(e => new { x.session.StartsAtUtc, e.ExpectedMinutes, e.PresentMinutes })).ToListAsync(ct);
        return raw.GroupBy(x => DateOnly.FromDateTime(x.StartsAtUtc.UtcDateTime)).OrderBy(x => x.Key).Select(g =>
        {
            var expected = g.Sum(x => x.ExpectedMinutes);
            var present = g.Sum(x => x.PresentMinutes);
            return new ReportingTrendPointDto(g.Key, expected == 0 ? 0m : Math.Round(present * 100m / expected, 2));
        }).ToArray();
    }

    public async Task<PagedAuditDto> GetAuditAsync(Guid? organizationId, string? action, string? entityType, Guid? userId, DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.AuditEntries.AsNoTracking().AsQueryable();
        if (organizationId.HasValue)
            query = query.Where(x => x.OrganizationId == organizationId);
        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(x => x.Action == action);
        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(x => x.EntityType == entityType);
        if (userId.HasValue)
            query = query.Where(x => x.UserId == userId);
        if (from.HasValue)
            query = query.Where(x => x.OccurredAtUtc >= from);
        if (to.HasValue)
            query = query.Where(x => x.OccurredAtUtc <= to);
        var total = await query.LongCountAsync(ct);
        var items = await query.OrderByDescending(x => x.OccurredAtUtc).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new AuditEntryDto(x.Id.Value, x.OrganizationId, x.UserId, x.UserDisplayName, x.Action, x.EntityType, x.EntityId, x.Route, x.CorrelationId, x.TraceId, x.IpAddress, x.OccurredAtUtc)).ToListAsync(ct);
        return new(items, page, pageSize, total);
    }

    public async Task<IReadOnlyCollection<CohortExportRowDto>> GetCohortExportRowsAsync(Guid cohortId, Guid? organizationScope, CancellationToken ct = default)
    {
        var enrollments = await db.Enrollments.AsNoTracking().Where(x => x.CohortId.Value == cohortId && (!organizationScope.HasValue || x.OrganizationId == organizationScope.Value)).Select(x => new { x.Id, x.LearnerProfileId, x.Status }).ToListAsync(ct);
        if (enrollments.Count == 0)
            return Array.Empty<CohortExportRowDto>();
        var profileIds = enrollments.Select(x => x.LearnerProfileId).Distinct().ToList();
        var profiles = await db.LearnerProfiles.AsNoTracking().Where(x => profileIds.Contains(x.Id)).Select(x => new { x.Id, x.PersonId }).ToListAsync(ct);
        var personIds = profiles.Select(x => x.PersonId).Distinct().ToList();
        var people = await db.People.AsNoTracking().Where(x => personIds.Contains(x.Id)).Select(x => new { x.Id, x.FirstName, x.LastName, x.Email }).ToListAsync(ct);
        var profileToPerson = profiles.ToDictionary(x => x.Id, x => x.PersonId);
        var personById = people.ToDictionary(x => x.Id);
        return enrollments.Select(e =>
        {
            var personId = profileToPerson[e.LearnerProfileId];
            var p = personById[personId];
            return new CohortExportRowDto(e.Id.Value, p.LastName, p.FirstName, p.Email, e.Status.ToString());
        }).ToArray();
    }

    private async Task<decimal> AttendanceRateAsync(Guid organizationId, IReadOnlyCollection<PedagoraPilot.Domain.Identifiers.CohortId> cohortIds, CancellationToken ct)
    {
        if (cohortIds.Count == 0)
            return 0m;
        var rows = await db.AttendanceSheets.AsNoTracking().Where(x => x.OrganizationId == organizationId && cohortIds.Contains(x.CohortId)).SelectMany(x => x.Entries.Select(e => new { e.ExpectedMinutes, e.PresentMinutes })).ToListAsync(ct);
        var expected = rows.Sum(x => x.ExpectedMinutes);
        return expected == 0 ? 0m : Math.Round(rows.Sum(x => x.PresentMinutes) * 100m / expected, 2);
    }

    private async Task<decimal> AverageProgressAsync(Guid organizationId, IReadOnlyCollection<PedagoraPilot.Domain.Identifiers.CohortId> cohortIds, CancellationToken ct)
    {
        if (cohortIds.Count == 0)
            return 0m;
        var enrollmentIds = await db.Enrollments.AsNoTracking().Where(x => x.OrganizationId == organizationId && cohortIds.Contains(x.CohortId)).Select(x => x.Id).ToListAsync(ct);
        if (enrollmentIds.Count == 0)
            return 0m;
        var scores = await db.LearnerCompetencyRecords.AsNoTracking().Where(x => enrollmentIds.Contains(x.EnrollmentId) && x.Score.HasValue).Select(x => x.Score!.Value).ToListAsync(ct);
        return scores.Count == 0 ? 0m : Math.Round(scores.Average(), 2);
    }

    private async Task<decimal> CertificationSuccessAsync(Guid organizationId, IReadOnlyCollection<PedagoraPilot.Domain.Identifiers.CohortId> cohortIds, CancellationToken ct)
    {
        if (cohortIds.Count == 0)
            return 0m;
        var enrollmentIds = await db.Enrollments.AsNoTracking().Where(x => x.OrganizationId == organizationId && cohortIds.Contains(x.CohortId)).Select(x => x.Id).ToListAsync(ct);
        if (enrollmentIds.Count == 0)
            return 0m;
        var decisions = await db.CertificationCandidates.AsNoTracking().Where(x => enrollmentIds.Contains(x.EnrollmentId) && x.Decision != CertificationDecision.Pending).Select(x => x.Decision).ToListAsync(ct);
        return decisions.Count == 0 ? 0m : Math.Round(decisions.Count(x => x == CertificationDecision.Obtained) * 100m / decisions.Count, 2);
    }
}
