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
}

public sealed class GetOrganizationDashboardHandler(IReportingReadRepository reports, ICurrentUser current) : IRequestHandler<GetOrganizationDashboardQuery, OrganizationDashboardDto>
{
    public async Task<OrganizationDashboardDto> Handle(GetOrganizationDashboardQuery q, CancellationToken ct)
    {
        ReportingScope.EnsureOrganization(current, q.OrganizationId);
        return await reports.GetOrganizationDashboardAsync(q.OrganizationId, ct) ?? throw new NotFoundApplicationException(ErrorKeys.OrganizationNotFound);
    }
}

public sealed class GetSiteDashboardHandler(IReportingReadRepository reports, ICurrentUser current) : IRequestHandler<GetSiteDashboardQuery, SiteDashboardDto>
{
    public async Task<SiteDashboardDto> Handle(GetSiteDashboardQuery q, CancellationToken ct) => await reports.GetSiteDashboardAsync(q.SiteId, ReportingScope.Organization(current), ct) ?? throw new NotFoundApplicationException(ErrorKeys.SiteNotFound);
}

public sealed class GetCohortDashboardHandler(IReportingReadRepository reports, ICurrentUser current) : IRequestHandler<GetCohortDashboardQuery, CohortDashboardDto>
{
    public async Task<CohortDashboardDto> Handle(GetCohortDashboardQuery q, CancellationToken ct) => await reports.GetCohortDashboardAsync(q.CohortId, ReportingScope.Organization(current), ct) ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
}

public sealed class GetAttendanceTrendHandler(IReportingReadRepository reports, ICurrentUser current) : IRequestHandler<GetAttendanceTrendQuery, IReadOnlyCollection<ReportingTrendPointDto>>
{
    public Task<IReadOnlyCollection<ReportingTrendPointDto>> Handle(GetAttendanceTrendQuery q, CancellationToken ct)
    {
        if (q.To < q.From)
            throw new ValidationApplicationException(ErrorKeys.ReportingDateRangeInvalid);
        return reports.GetAttendanceTrendAsync(q.CohortId, ReportingScope.Organization(current), q.From, q.To, ct);
    }
}

public sealed class GetAuditHandler(IReportingReadRepository reports, ICurrentUser current) : IRequestHandler<GetAuditQuery, PagedAuditDto>
{
    public Task<PagedAuditDto> Handle(GetAuditQuery q, CancellationToken ct)
    {
        if (q.OrganizationId.HasValue)
            TenantScope.Ensure(current, q.OrganizationId.Value);
        var organizationId = TenantScope.Organization(current) ?? q.OrganizationId;
        return reports.GetAuditAsync(organizationId, q.Action, q.EntityType, q.UserId, q.From, q.To, Math.Max(1, q.Page), Math.Clamp(q.PageSize, 1, 200), ct);
    }
}

public sealed class ExportCohortReportHandler(IReportingReadRepository reports, ICurrentUser current) : IRequestHandler<ExportCohortReportQuery, ReportExportDto>
{
    public async Task<ReportExportDto> Handle(ExportCohortReportQuery q, CancellationToken ct)
    {
        var rows = await reports.GetCohortExportRowsAsync(q.CohortId, ReportingScope.Organization(current), ct);
        if (rows.Count == 0)
            throw new NotFoundApplicationException(ErrorKeys.ReportingNoData);
        static string Csv(string? value) => "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";
        var sb = new StringBuilder();
        sb.AppendLine("EnrollmentId;LastName;FirstName;Email;Status");
        foreach (var row in rows)
            sb.AppendLine($"{row.EnrollmentId};{Csv(row.LastName)};{Csv(row.FirstName)};{Csv(row.Email)};{Csv(row.Status)}");
        return new($"cohort-{q.CohortId:N}-{DateTime.UtcNow:yyyyMMddHHmmss}.csv", "text/csv; charset=utf-8", Encoding.UTF8.GetBytes(sb.ToString()));
    }
}
