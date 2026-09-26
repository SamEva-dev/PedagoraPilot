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
    Task<IReadOnlyCollection<CohortLearnerDashboardDto>> GetCohortLearnerDashboardsAsync(Guid cohortId, Guid? organizationScope, CancellationToken ct = default);
    Task<IReadOnlyCollection<CohortDrivingObservationDto>> GetCohortDrivingObservationsAsync(Guid cohortId, Guid? organizationScope, int take = 20, CancellationToken ct = default);
    Task<CohortLearnerDashboardDto?> GetMyLearnerDashboardAsync(Guid authGateUserId, Guid? organizationScope, Guid? cohortId = null, CancellationToken ct = default);
    Task<LearnerDetailReportDto?> GetLearnerDetailAsync(Guid learnerProfileId, Guid? organizationScope, Guid? cohortId = null, CancellationToken ct = default);
    Task<LearnerDetailReportDto?> GetMyLearnerDetailAsync(Guid authGateUserId, Guid? organizationScope, Guid? cohortId = null, CancellationToken ct = default);
    Task<IReadOnlyCollection<CertificationSuccessRecordDto>> GetCertificationSuccessAsync(Guid organizationId, CancellationToken ct = default);
    Task<IReadOnlyCollection<ReportingTrendPointDto>> GetAttendanceTrendAsync(Guid cohortId, Guid? organizationScope, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<PagedAuditDto> GetAuditAsync(Guid? organizationId, string? action, string? entityType, Guid? userId, DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyCollection<CohortExportRowDto>> GetCohortExportRowsAsync(Guid cohortId, Guid? organizationScope, CancellationToken ct = default);
}
