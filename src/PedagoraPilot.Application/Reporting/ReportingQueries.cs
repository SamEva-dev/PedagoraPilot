using DomainRelay.Abstractions;
using PedagoraPilot.Contracts.Reporting;

namespace PedagoraPilot.Application.Reporting;
public sealed record GetOrganizationDashboardQuery(Guid OrganizationId) : IRequest<OrganizationDashboardDto>;
public sealed record GetSiteDashboardQuery(Guid SiteId) : IRequest<SiteDashboardDto>;
public sealed record GetCohortDashboardQuery(Guid CohortId) : IRequest<CohortDashboardDto>;
public sealed record GetAttendanceTrendQuery(Guid CohortId, DateOnly From, DateOnly To) : IRequest<IReadOnlyCollection<ReportingTrendPointDto>>;
public sealed record GetAuditQuery(Guid? OrganizationId, string? Action, string? EntityType, Guid? UserId, DateTimeOffset? From, DateTimeOffset? To, int Page = 1, int PageSize = 50) : IRequest<PagedAuditDto>;
public sealed record ExportCohortReportQuery(Guid CohortId) : IRequest<ReportExportDto>;
