namespace PedagoraPilot.Contracts.Reporting;
public sealed record OrganizationDashboardDto(Guid OrganizationId, int Sites, int ActivePrograms, int ActiveCohorts, int Learners, decimal AttendanceRate, decimal AverageProgressRate, decimal CertificationSuccessRate, int OpenAlerts);
public sealed record SiteDashboardDto(Guid SiteId, string SiteName, int ActiveCohorts, int Learners, decimal AttendanceRate, decimal AverageProgressRate, decimal CertificationSuccessRate);
public sealed record CohortDashboardDto(Guid CohortId, string CohortCode, string CohortName, int Learners, int PlannedMinutes, int DeliveredMinutes, int PresentMinutes, decimal AttendanceRate, decimal AverageCompetencyProgress, int WorkplacePeriodsCompleted, int CertificationEligible, int CertificationObtained, int OpenAlerts);
public sealed record CohortDrivingObservationDto(Guid Id, Guid EnrollmentId, string LearnerDisplayName, DateTimeOffset EvaluatedAtUtc, string CompetencyCode, string Subject, string Positive, string Difficulty, string NextGoal);
public sealed record CohortLearnerDashboardDto(Guid EnrollmentId, Guid LearnerProfileId, string FirstName, string LastName, string EnrollmentStatus, int PlannedMinutes, int CompletedMinutes, int CatchupMinutes, int PreparedTopics, int PresentedTopics, int ValidatedTopics, int ReworkTopics, int TotalTopics, decimal AverageCompetencyProgress, IReadOnlyDictionary<string, decimal> Competencies, int AttendanceExpectedMinutes, int AttendancePresentMinutes, decimal AttendanceRate, int PresentCount, int LateCount, int AbsentCount, int ExcusedCount, int ClassroomMinutes, int DrivingMinutes, int InternshipMinutes);

public sealed record ReportingTrendPointDto(DateOnly Date, decimal Value);
public sealed record AuditEntryDto(Guid Id, Guid? OrganizationId, Guid? UserId, string? UserDisplayName, string Action, string EntityType, string? EntityId, string? Route, string? CorrelationId, string? TraceId, string? IpAddress, DateTimeOffset OccurredAtUtc);
public sealed record PagedAuditDto(IReadOnlyCollection<AuditEntryDto> Items, int Page, int PageSize, long Total);
public sealed record ReportExportDto(string FileName, string ContentType, byte[] Content);
public sealed record CohortExportRowDto(Guid EnrollmentId, string LastName, string FirstName, string Email, string Status, int PlannedMinutes, int CompletedMinutes, int CatchupMinutes, int ValidatedTopics, int TotalTopics, decimal AverageCompetencyProgress);

public sealed record LearnerAttendanceDetailDto(DateTimeOffset StartsAtUtc, string SessionTitle, string Status, int MissedMinutes);
public sealed record LearnerSkillCriterionDetailDto(Guid Id, string Code, string Title, string Level, decimal Score);
public sealed record LearnerSkillDetailDto(Guid Id, string Code, string Title, decimal Progress, IReadOnlyCollection<LearnerSkillCriterionDetailDto> Criteria);
public sealed record LearnerTopicDetailDto(Guid Id, string Code, int? Number, string Title, string Category, string Status, DateOnly? PreparationDate, DateOnly? PresentationDate);
public sealed record LearnerDrivingCriterionDetailDto(string Code, string Label, string Level);
public sealed record LearnerDrivingDetailDto(Guid Id, DateTimeOffset EvaluatedAtUtc, string CompetencyCode, string Subject, string TrainerDisplayName, string Positive, string Difficulty, string NextGoal, IReadOnlyCollection<LearnerDrivingCriterionDetailDto> Criteria);
public sealed record LearnerWorkplaceActivityDetailDto(string Code, string Title, string Status);
public sealed record LearnerWorkplaceEvaluationDetailDto(string Kind, string EvaluatorDisplayName, DateTimeOffset EvaluatedAtUtc, string Summary, string Strengths, string ImprovementAreas, bool? Validated);
public sealed record LearnerWorkplaceDetailDto(Guid Id, string Company, string City, string TutorName, DateOnly StartDate, DateOnly EndDate, decimal PlannedHours, decimal CompletedHours, string Status, string TutorObservation, IReadOnlyCollection<LearnerWorkplaceActivityDetailDto> Activities, IReadOnlyCollection<LearnerWorkplaceEvaluationDetailDto> Evaluations);
public sealed record LearnerDocumentDetailDto(Guid Id, string Title, string Category, DateTime CreatedAtUtc, long SizeBytes, string FileName);
public sealed record LearnerCertificationStepDetailDto(Guid Id, string Code, string Title, string UnitCode, int DurationMinutes, string Outcome);
public sealed record LearnerCertificationDetailDto(Guid CandidateId, Guid ExamSessionId, string SessionTitle, DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc, string Status, bool? Eligible, string Decision, IReadOnlyCollection<string> EligibilityBlockers, IReadOnlyCollection<LearnerCertificationStepDetailDto> Steps);
public sealed record LearnerHistoryDetailDto(Guid Id, DateTimeOffset OccurredAtUtc, string Action, string UserDisplayName);
public sealed record LearnerDetailReportDto(
    Guid EnrollmentId,
    Guid LearnerProfileId,
    Guid CohortId,
    Guid SiteId,
    Guid ProgramId,
    Guid ReferentialVersionId,
    string CohortKey,
    string CohortName,
    DateOnly CohortStartDate,
    DateOnly CohortEndDate,
    string FirstName,
    string LastName,
    string Email,
    string EnrollmentStatus,
    CohortLearnerDashboardDto Summary,
    IReadOnlyCollection<LearnerAttendanceDetailDto> Attendance,
    IReadOnlyCollection<LearnerSkillDetailDto> Skills,
    IReadOnlyCollection<LearnerTopicDetailDto> Topics,
    IReadOnlyCollection<LearnerDrivingDetailDto> Driving,
    IReadOnlyCollection<LearnerWorkplaceDetailDto> Workplace,
    IReadOnlyCollection<LearnerDocumentDetailDto> Documents,
    LearnerCertificationDetailDto? Certification,
    IReadOnlyCollection<LearnerHistoryDetailDto> History);

public sealed record CertificationSuccessUnitResultDto(string Code, bool Validated);
public sealed record CertificationSuccessCandidateDto(Guid Id, Guid EnrollmentId, string FirstName, string LastName, string CandidateNumber, string Result, IReadOnlyCollection<CertificationSuccessUnitResultDto> UnitResults);
public sealed record CertificationSuccessRecordDto(Guid CohortId, string CohortKey, Guid OrganizationId, Guid SiteId, Guid ProgramId, string CohortName, DateOnly CohortStartDate, DateOnly CohortEndDate, Guid ExamSessionId, string SessionTitle, DateTimeOffset SessionStartsAtUtc, int Presented, int Graduated, int Partial, int Failed, int Absent, decimal Rate, IReadOnlyCollection<CertificationSuccessCandidateDto> Candidates);
