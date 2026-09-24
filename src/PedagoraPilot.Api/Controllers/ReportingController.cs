using DomainRelay.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PedagoraPilot.Api.Authorization;
using PedagoraPilot.Application.Reporting;
using PedagoraPilot.Contracts.Reporting;
using PedagoraPilot.Security.Contracts;

namespace PedagoraPilot.Api.Controllers;
[ApiController]
[Authorize]
[Route("api/v1/reporting")]
public sealed class ReportingController(IMediator mediator) : ControllerBase
{
    [HttpGet("organizations/{organizationId:guid}/dashboard")]
    [HasPermission(PedagoraPilotPermissionCodes.Statistics.View)]
    public async Task<ActionResult<OrganizationDashboardDto>> OrganizationDashboard(Guid organizationId, CancellationToken ct) => Ok(await mediator.Send(new GetOrganizationDashboardQuery(organizationId), ct));
    [HttpGet("sites/{siteId:guid}/dashboard")]
    [HasPermission(PedagoraPilotPermissionCodes.Statistics.View)]
    public async Task<ActionResult<SiteDashboardDto>> SiteDashboard(Guid siteId, CancellationToken ct) => Ok(await mediator.Send(new GetSiteDashboardQuery(siteId), ct));
    [HttpGet("cohorts/{cohortId:guid}/dashboard")]
    [HasPermission(PedagoraPilotPermissionCodes.Statistics.View)]
    public async Task<ActionResult<CohortDashboardDto>> CohortDashboard(Guid cohortId, CancellationToken ct) => Ok(await mediator.Send(new GetCohortDashboardQuery(cohortId), ct));
    [HttpGet("cohorts/{cohortId:guid}/attendance-trend")]
    [HasPermission(PedagoraPilotPermissionCodes.Statistics.View)]
    public async Task<ActionResult<IReadOnlyCollection<ReportingTrendPointDto>>> AttendanceTrend(Guid cohortId, [FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct) => Ok(await mediator.Send(new GetAttendanceTrendQuery(cohortId, from, to), ct));
    [HttpGet("audit")]
    [HasPermission(PedagoraPilotPermissionCodes.Audit.View)]
    public async Task<ActionResult<PagedAuditDto>> Audit([FromQuery] Guid? organizationId, [FromQuery] string? action, [FromQuery] string? entityType, [FromQuery] Guid? userId, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default) => Ok(await mediator.Send(new GetAuditQuery(organizationId, action, entityType, userId, from, to, page, pageSize), ct));
    [HttpGet("cohorts/{cohortId:guid}/export")]
    [HasPermission(PedagoraPilotPermissionCodes.Reports.Export)]
    public async Task<IActionResult> ExportCohort(Guid cohortId, CancellationToken ct)
    {
        var report = await mediator.Send(new ExportCohortReportQuery(cohortId), ct);
        return File(report.Content, report.ContentType, report.FileName);
    }
}
