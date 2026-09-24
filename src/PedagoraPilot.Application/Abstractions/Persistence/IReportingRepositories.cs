using PedagoraPilot.Contracts.Reporting;
using PedagoraPilot.Domain.Audit;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Application.Abstractions.Persistence;
public interface IAuditEntryRepository : IRepository<AuditEntry, AuditEntryId>
{
    IQueryable<AuditEntry> QueryByOrganization(Guid organizationId, bool isTracking = false);
}

/// <summary>
/// Read-only reporting repository. Its infrastructure implementation MUST use no-tracking queries.
/// </summary>
public interface IReportingReadRepository
{
    Task<OrganizationDashboardDto?> GetOrganizationDashboardAsync(Guid organizationId, CancellationToken ct = default);
    Task<SiteDashboardDto?> GetSiteDashboardAsync(Guid siteId, Guid? organizationScope, CancellationToken ct = default);
    Task<CohortDashboardDto?> GetCohortDashboardAsync(Guid cohortId, Guid? organizationScope, CancellationToken ct = default);
    Task<IReadOnlyCollection<ReportingTrendPointDto>> GetAttendanceTrendAsync(Guid cohortId, Guid? organizationScope, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<PagedAuditDto> GetAuditAsync(Guid? organizationId, string? action, string? entityType, Guid? userId, DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyCollection<CohortExportRowDto>> GetCohortExportRowsAsync(Guid cohortId, Guid? organizationScope, CancellationToken ct = default);
}
