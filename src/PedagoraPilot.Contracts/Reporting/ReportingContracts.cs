namespace PedagoraPilot.Contracts.Reporting;
public sealed record OrganizationDashboardDto(Guid OrganizationId, int Sites, int ActivePrograms, int ActiveCohorts, int Learners, decimal AttendanceRate, decimal AverageProgressRate, decimal CertificationSuccessRate, int OpenAlerts);
public sealed record SiteDashboardDto(Guid SiteId, string SiteName, int ActiveCohorts, int Learners, decimal AttendanceRate, decimal AverageProgressRate, decimal CertificationSuccessRate);
public sealed record CohortDashboardDto(Guid CohortId, string CohortCode, string CohortName, int Learners, int PlannedMinutes, int DeliveredMinutes, int PresentMinutes, decimal AttendanceRate, decimal AverageCompetencyProgress, int WorkplacePeriodsCompleted, int CertificationEligible, int CertificationObtained, int OpenAlerts);
public sealed record ReportingTrendPointDto(DateOnly Date, decimal Value);
public sealed record AuditEntryDto(Guid Id, Guid? OrganizationId, Guid? UserId, string? UserDisplayName, string Action, string EntityType, string? EntityId, string? Route, string? CorrelationId, string? TraceId, string? IpAddress, DateTimeOffset OccurredAtUtc);
public sealed record PagedAuditDto(IReadOnlyCollection<AuditEntryDto> Items, int Page, int PageSize, long Total);
public sealed record ReportExportDto(string FileName, string ContentType, byte[] Content);
public sealed record CohortExportRowDto(Guid EnrollmentId, string LastName, string FirstName, string Email, string Status);
