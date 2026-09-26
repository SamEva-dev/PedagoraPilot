using Microsoft.EntityFrameworkCore;
using System.Text;
using DomainRelay.Abstractions;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Contracts.Reporting;

namespace PedagoraPilot.Application.Reporting;
internal static class ReportingScope
{
    public static void EnsureOrganization(ICurrentUser current, Guid requested)
    {
        TenantScope.Ensure(current, requested);
    }

    public static Guid? Organization(ICurrentUser current) => TenantScope.Organization(current);

    public static void EnsureOrganizationAggregate(ICurrentUser current)
    {
        if (current.HasContextualScopeRestrictions)
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
    }

    public static async Task EnsureCohortAsync(Guid cohortId, ICohortRepository cohorts, IProgramOfferingRepository offerings, ICurrentUser current, CancellationToken ct)
    {
        var cohort = await cohorts.GetByIdAsync(new PedagoraPilot.Domain.Identifiers.CohortId(cohortId), false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
        TenantScope.Ensure(current, cohort.OrganizationId);
        await ContextualScope.EnsureCanViewCohortAsync(current, cohort, offerings, ct);
    }
}

public sealed class GetOrganizationDashboardHandler(IReportingReadRepository reports, ICurrentUser current) : IRequestHandler<GetOrganizationDashboardQuery, OrganizationDashboardDto>
{
    public async Task<OrganizationDashboardDto> Handle(GetOrganizationDashboardQuery q, CancellationToken ct)
    {
        ReportingScope.EnsureOrganization(current, q.OrganizationId);
        ReportingScope.EnsureOrganizationAggregate(current);
        return await reports.GetOrganizationDashboardAsync(q.OrganizationId, ct) ?? throw new NotFoundApplicationException(ErrorKeys.OrganizationNotFound);
    }
}

public sealed class GetSiteDashboardHandler(IReportingReadRepository reports, ICurrentUser current) : IRequestHandler<GetSiteDashboardQuery, SiteDashboardDto>
{
    public async Task<SiteDashboardDto> Handle(GetSiteDashboardQuery q, CancellationToken ct)
    {
        if (current.HasContextualScopeRestrictions && !current.CanManageSite(q.SiteId))
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
        return await reports.GetSiteDashboardAsync(q.SiteId, ReportingScope.Organization(current), ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.SiteNotFound);
    }
}

public sealed class GetCohortDashboardHandler(IReportingReadRepository reports, ICohortRepository cohorts, IProgramOfferingRepository offerings, ICurrentUser current) : IRequestHandler<GetCohortDashboardQuery, CohortDashboardDto>
{
    public async Task<CohortDashboardDto> Handle(GetCohortDashboardQuery q, CancellationToken ct)
    {
        await ReportingScope.EnsureCohortAsync(q.CohortId, cohorts, offerings, current, ct);
        return await reports.GetCohortDashboardAsync(q.CohortId, ReportingScope.Organization(current), ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
    }
}

public sealed class GetCohortLearnerDashboardsHandler(IReportingReadRepository reports, ICohortRepository cohorts, IProgramOfferingRepository offerings, ICurrentUser current) : IRequestHandler<GetCohortLearnerDashboardsQuery, IReadOnlyCollection<CohortLearnerDashboardDto>>
{
    public async Task<IReadOnlyCollection<CohortLearnerDashboardDto>> Handle(GetCohortLearnerDashboardsQuery q, CancellationToken ct)
    {
        await ReportingScope.EnsureCohortAsync(q.CohortId, cohorts, offerings, current, ct);
        return await reports.GetCohortLearnerDashboardsAsync(q.CohortId, ReportingScope.Organization(current), ct);
    }
}

public sealed class GetCohortDrivingObservationsHandler(IReportingReadRepository reports, ICohortRepository cohorts, IProgramOfferingRepository offerings, ICurrentUser current) : IRequestHandler<GetCohortDrivingObservationsQuery, IReadOnlyCollection<CohortDrivingObservationDto>>
{
    public async Task<IReadOnlyCollection<CohortDrivingObservationDto>> Handle(GetCohortDrivingObservationsQuery q, CancellationToken ct)
    {
        await ReportingScope.EnsureCohortAsync(q.CohortId, cohorts, offerings, current, ct);
        return await reports.GetCohortDrivingObservationsAsync(q.CohortId, ReportingScope.Organization(current), Math.Clamp(q.Take, 1, 100), ct);
    }
}

public sealed class GetMyLearnerDashboardHandler(
    IReportingReadRepository reports,
    IEnrollmentRepository enrollments,
    ILearnerProfileRepository profiles,
    IPersonRepository people,
    ICohortRepository cohorts,
    IProgramOfferingRepository offerings,
    ICurrentUser current) : IRequestHandler<GetMyLearnerDashboardQuery, CohortLearnerDashboardDto>
{
    public async Task<CohortLearnerDashboardDto> Handle(GetMyLearnerDashboardQuery q, CancellationToken ct)
    {
        var profile = await PedagoraPilot.Application.Training.Learners.LearnerSelfAccess
            .ResolveProfileAsync(profiles, people, current, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.LearnerNotFound);

        var organizationId = ReportingScope.Organization(current);
        var enrollmentQuery = enrollments.Query(false)
            .Where(x => x.LearnerProfileId == profile.Id);

        if (organizationId.HasValue)
            enrollmentQuery = enrollmentQuery.Where(x => x.OrganizationId == organizationId.Value);
        if (q.CohortId.HasValue)
            enrollmentQuery = enrollmentQuery.Where(x => x.CohortId == new PedagoraPilot.Domain.Identifiers.CohortId(q.CohortId.Value));

        var enrollment = await enrollmentQuery
            .OrderByDescending(x => x.Status == PedagoraPilot.Domain.Training.EnrollmentStatus.Active)
            .ThenByDescending(x => x.EnrolledOn)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.LearnerNotFound);

        await ReportingScope.EnsureCohortAsync(enrollment.CohortId.Value, cohorts, offerings, current, ct);
        var rows = await reports.GetCohortLearnerDashboardsAsync(enrollment.CohortId.Value, organizationId, ct);
        return rows.SingleOrDefault(x => x.EnrollmentId == enrollment.Id.Value)
            ?? throw new NotFoundApplicationException(ErrorKeys.LearnerNotFound);
    }
}


public sealed class GetLearnerDetailHandler(IReportingReadRepository reports, ICohortRepository cohorts, IProgramOfferingRepository offerings, ICurrentUser current) : IRequestHandler<GetLearnerDetailQuery, LearnerDetailReportDto>
{
    public async Task<LearnerDetailReportDto> Handle(GetLearnerDetailQuery q, CancellationToken ct)
    {
        var result = await reports.GetLearnerDetailAsync(q.LearnerProfileId, ReportingScope.Organization(current), q.CohortId, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.LearnerNotFound);
        await ReportingScope.EnsureCohortAsync(result.CohortId, cohorts, offerings, current, ct);
        return result;
    }
}

public sealed class GetMyLearnerDetailHandler(
    IReportingReadRepository reports,
    ILearnerProfileRepository profiles,
    IPersonRepository people,
    ICohortRepository cohorts,
    IProgramOfferingRepository offerings,
    ICurrentUser current) : IRequestHandler<GetMyLearnerDetailQuery, LearnerDetailReportDto>
{
    public async Task<LearnerDetailReportDto> Handle(GetMyLearnerDetailQuery q, CancellationToken ct)
    {
        var profile = await PedagoraPilot.Application.Training.Learners.LearnerSelfAccess
            .ResolveProfileAsync(profiles, people, current, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.LearnerNotFound);

        var result = await reports.GetLearnerDetailAsync(profile.Id.Value, ReportingScope.Organization(current), q.CohortId, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.LearnerNotFound);
        await ReportingScope.EnsureCohortAsync(result.CohortId, cohorts, offerings, current, ct);
        return result;
    }
}

public sealed class GetCertificationSuccessHandler(IReportingReadRepository reports, ICurrentUser current) : IRequestHandler<GetCertificationSuccessQuery, IReadOnlyCollection<CertificationSuccessRecordDto>>
{
    public async Task<IReadOnlyCollection<CertificationSuccessRecordDto>> Handle(GetCertificationSuccessQuery q, CancellationToken ct)
    {
        ReportingScope.EnsureOrganization(current, q.OrganizationId);
        var rows = await reports.GetCertificationSuccessAsync(q.OrganizationId, ct);
        if (!current.HasContextualScopeRestrictions)
            return rows;
        return rows.Where(x => current.CanViewExam(x.SiteId, x.ProgramId, x.CohortId, x.ExamSessionId)).ToArray();
    }
}

public sealed class GetAttendanceTrendHandler(IReportingReadRepository reports, ICohortRepository cohorts, IProgramOfferingRepository offerings, ICurrentUser current) : IRequestHandler<GetAttendanceTrendQuery, IReadOnlyCollection<ReportingTrendPointDto>>
{
    public async Task<IReadOnlyCollection<ReportingTrendPointDto>> Handle(GetAttendanceTrendQuery q, CancellationToken ct)
    {
        if (q.To < q.From)
            throw new ValidationApplicationException(ErrorKeys.ReportingDateRangeInvalid);
        await ReportingScope.EnsureCohortAsync(q.CohortId, cohorts, offerings, current, ct);
        return await reports.GetAttendanceTrendAsync(q.CohortId, ReportingScope.Organization(current), q.From, q.To, ct);
    }
}

public sealed class GetAuditHandler(IReportingReadRepository reports, ICurrentUser current) : IRequestHandler<GetAuditQuery, PagedAuditDto>
{
    public Task<PagedAuditDto> Handle(GetAuditQuery q, CancellationToken ct)
    {
        if (q.OrganizationId.HasValue)
            TenantScope.Ensure(current, q.OrganizationId.Value);
        ReportingScope.EnsureOrganizationAggregate(current);
        var organizationId = TenantScope.Organization(current) ?? q.OrganizationId;
        return reports.GetAuditAsync(organizationId, q.Action, q.EntityType, q.UserId, q.From, q.To, Math.Max(1, q.Page), Math.Clamp(q.PageSize, 1, 200), ct);
    }
}

public sealed class ExportCohortReportHandler(IReportingReadRepository reports, ICohortRepository cohorts, IProgramOfferingRepository offerings, ICurrentUser current) : IRequestHandler<ExportCohortReportQuery, ReportExportDto>
{
    public async Task<ReportExportDto> Handle(ExportCohortReportQuery q, CancellationToken ct)
    {
        await ReportingScope.EnsureCohortAsync(q.CohortId, cohorts, offerings, current, ct);
        var rows = await reports.GetCohortExportRowsAsync(q.CohortId, ReportingScope.Organization(current), ct);
        if (rows.Count == 0)
            throw new NotFoundApplicationException(ErrorKeys.ReportingNoData);
        static string Csv(string? value) => "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";
        var sb = new StringBuilder();
        sb.AppendLine("EnrollmentId;LastName;FirstName;Email;Status;PlannedHours;CompletedHours;CatchupHours;ValidatedTopics;TotalTopics;AverageCompetencyProgress");
        foreach (var row in rows)
            sb.AppendLine($"{row.EnrollmentId};{Csv(row.LastName)};{Csv(row.FirstName)};{Csv(row.Email)};{Csv(row.Status)};{row.PlannedMinutes / 60m:0.##};{row.CompletedMinutes / 60m:0.##};{row.CatchupMinutes / 60m:0.##};{row.ValidatedTopics};{row.TotalTopics};{row.AverageCompetencyProgress:0.##}");
        return new($"cohort-{q.CohortId:N}-{DateTime.UtcNow:yyyyMMddHHmmss}.csv", "text/csv; charset=utf-8", Encoding.UTF8.GetBytes("\uFEFF" + sb.ToString()));
    }
}
