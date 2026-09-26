using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Contracts.Reporting;
using PedagoraPilot.Domain.Certification;
using PedagoraPilot.Domain.Documents;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Learning;
using PedagoraPilot.Domain.Training;
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

        var siteIds = await db.TrainingSites.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId)
            .Select(x => x.Id)
            .ToListAsync(ct);

        var cohorts = await db.Cohorts.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId)
            .Select(x => new { x.Id, x.SiteId, x.Status })
            .ToListAsync(ct);
        var cohortIds = cohorts.Select(x => x.Id).ToList();

        var learners = await db.Enrollments.AsNoTracking()
            .CountAsync(x => cohortIds.Contains(x.CohortId), ct);

        var activePrograms = await db.ProgramOfferings.AsNoTracking()
            .Where(x => siteIds.Contains(x.SiteId) && x.IsActive)
            .Select(x => x.ProgramId)
            .Distinct()
            .CountAsync(ct);

        return new(
            organizationId,
            siteIds.Count,
            activePrograms,
            cohorts.Count(x => x.Status == CohortStatus.Active),
            learners,
            await AttendanceRateAsync(organizationId, cohortIds, ct),
            await AverageProgressAsync(organizationId, cohortIds, ct),
            await CertificationSuccessAsync(organizationId, cohortIds, ct),
            0);
    }

    public async Task<SiteDashboardDto?> GetSiteDashboardAsync(Guid siteId, Guid? organizationScope, CancellationToken ct = default)
    {
        var site = await db.TrainingSites.AsNoTracking()
            .Where(x => x.Id == siteId && (!organizationScope.HasValue || x.OrganizationId == organizationScope.Value))
            .Select(x => new { x.Id, x.OrganizationId, x.Name })
            .SingleOrDefaultAsync(ct);
        if (site is null)
            return null;

        var cohorts = await db.Cohorts.AsNoTracking()
            .Where(x => x.SiteId == siteId)
            .Select(x => new { x.Id, x.Status })
            .ToListAsync(ct);
        var cohortIds = cohorts.Select(x => x.Id).ToList();
        var learners = await db.Enrollments.AsNoTracking()
            .CountAsync(x => cohortIds.Contains(x.CohortId), ct);

        return new(
            site.Id,
            site.Name,
            cohorts.Count(x => x.Status == CohortStatus.Active),
            learners,
            await AttendanceRateAsync(site.OrganizationId, cohortIds, ct),
            await AverageProgressAsync(site.OrganizationId, cohortIds, ct),
            await CertificationSuccessAsync(site.OrganizationId, cohortIds, ct));
    }

    public async Task<CohortDashboardDto?> GetCohortDashboardAsync(Guid cohortId, Guid? organizationScope, CancellationToken ct = default)
    {
        var typedCohortId = new CohortId(cohortId);

        var cohortQuery = db.Cohorts.AsNoTracking()
            .Where(x => x.Id == typedCohortId);

        if (organizationScope.HasValue)
        {
            var organizationId = organizationScope.Value;
            cohortQuery = cohortQuery.Where(x => x.OrganizationId == organizationId);
        }

        var cohort = await cohortQuery
            .Select(x => new { x.Id, x.OrganizationId, x.Code, x.Name })
            .SingleOrDefaultAsync(ct);
        if (cohort is null)
            return null;

        var cohortIds = new[] { cohort.Id };
        var enrollmentIds = await db.Enrollments.AsNoTracking()
            .Where(x => x.CohortId == cohort.Id)
            .Select(x => x.Id)
            .ToListAsync(ct);

        var sessions = await db.TrainingSessions.AsNoTracking()
            .Where(x => x.CohortId == cohort.Id)
            .Select(x => new { x.StartsAtUtc, x.EndsAtUtc, x.Status })
            .ToListAsync(ct);

        var plannedMinutes = (int)sessions.Sum(x => Math.Max(0, (x.EndsAtUtc - x.StartsAtUtc).TotalMinutes));
        var deliveredMinutes = (int)sessions.Where(x => x.Status == TrainingSessionStatus.Completed)
            .Sum(x => Math.Max(0, (x.EndsAtUtc - x.StartsAtUtc).TotalMinutes));

        var attendanceRows = await db.AttendanceSheets.AsNoTracking()
            .Where(x => x.CohortId == cohort.Id)
            .SelectMany(x => x.Entries.Select(e => new { e.ExpectedMinutes, e.PresentMinutes }))
            .ToListAsync(ct);
        var expected = attendanceRows.Sum(x => x.ExpectedMinutes);
        var present = attendanceRows.Sum(x => x.PresentMinutes);

        var workplaceCompleted = await db.WorkplacePeriods.AsNoTracking()
            .CountAsync(x => enrollmentIds.Contains(x.EnrollmentId) && x.Status == WorkplacePeriodStatus.Completed, ct);

        var candidates = await db.CertificationCandidates.AsNoTracking()
            .Where(x => enrollmentIds.Contains(x.EnrollmentId))
            .Select(x => new { x.Eligible, x.Decision })
            .ToListAsync(ct);

        return new(
            cohortId,
            cohort.Code,
            cohort.Name,
            enrollmentIds.Count,
            plannedMinutes,
            deliveredMinutes,
            present,
            expected == 0 ? 0m : Math.Round(present * 100m / expected, 2),
            await AverageProgressAsync(cohort.OrganizationId, cohortIds, ct),
            workplaceCompleted,
            candidates.Count(x => x.Eligible == true),
            candidates.Count(x => x.Decision == CertificationDecision.Obtained),
            0);
    }

    public async Task<IReadOnlyCollection<CohortLearnerDashboardDto>> GetCohortLearnerDashboardsAsync(
        Guid cohortId, Guid? organizationScope, CancellationToken ct = default)
    {
        var typedCohortId = new CohortId(cohortId);
        var cohortQuery = db.Cohorts.AsNoTracking().Where(x => x.Id == typedCohortId);
        if (organizationScope.HasValue)
        {
            var organizationId = organizationScope.Value;
            cohortQuery = cohortQuery.Where(x => x.OrganizationId == organizationId);
        }

        var cohort = await cohortQuery
            .Select(x => new { x.Id, x.OrganizationId, x.ReferentialVersionId })
            .SingleOrDefaultAsync(ct);
        if (cohort is null)
            return Array.Empty<CohortLearnerDashboardDto>();

        var enrollmentRows = await db.Enrollments.AsNoTracking()
            .Where(x => x.CohortId == cohort.Id)
            .Join(db.LearnerProfiles.AsNoTracking(), enrollment => enrollment.LearnerProfileId, profile => profile.Id,
                (enrollment, profile) => new { enrollment, profile })
            .Join(db.People.AsNoTracking(), x => x.profile.PersonId, person => person.Id,
                (x, person) => new
                {
                    x.enrollment.Id,
                    x.enrollment.LearnerProfileId,
                    x.enrollment.Status,
                    person.FirstName,
                    person.LastName
                })
            .ToListAsync(ct);

        if (enrollmentRows.Count == 0)
            return Array.Empty<CohortLearnerDashboardDto>();

        var enrollmentIds = enrollmentRows.Select(x => x.Id).ToArray();
        var totalHours = await db.ReferentialVersions.AsNoTracking()
            .Where(x => x.Id == cohort.ReferentialVersionId)
            .Select(x => x.TotalHours)
            .SingleOrDefaultAsync(ct);
        var totalTopics = await db.PedagogicalTopics.AsNoTracking()
            .CountAsync(x => x.ReferentialVersionId == cohort.ReferentialVersionId && x.Active, ct);

        var attendanceRows = await db.AttendanceSheets.AsNoTracking()
            .Where(x => x.CohortId == cohort.Id)
            .Join(
                db.TrainingSessions.AsNoTracking(),
                sheet => sheet.SessionId,
                session => session.Id,
                (sheet, session) => new { sheet, session.Type })
            .SelectMany(x => x.sheet.Entries.Select(e => new
            {
                e.EnrollmentId,
                e.Status,
                e.ExpectedMinutes,
                e.PresentMinutes,
                e.CatchupMinutes,
                x.Type
            }))
            .ToListAsync(ct);
        var attendance = attendanceRows
            .GroupBy(x => x.EnrollmentId)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    Expected = g.Sum(x => x.ExpectedMinutes),
                    Present = g.Sum(x => x.PresentMinutes),
                    Catchup = g.Sum(x => x.CatchupMinutes),
                    PresentCount = g.Count(x => x.Status == AttendanceStatus.Present),
                    LateCount = g.Count(x => x.Status == AttendanceStatus.Late),
                    AbsentCount = g.Count(x => x.Status == AttendanceStatus.Absent),
                    ExcusedCount = g.Count(x => x.Status == AttendanceStatus.Excused),
                    Classroom = g.Where(x => x.Type is not TrainingSessionType.Driving and not TrainingSessionType.Internship).Sum(x => x.PresentMinutes),
                    Driving = g.Where(x => x.Type == TrainingSessionType.Driving).Sum(x => x.PresentMinutes),
                    InternshipSession = g.Where(x => x.Type == TrainingSessionType.Internship).Sum(x => x.PresentMinutes)
                });

        var workplaceRows = await db.WorkplacePeriods.AsNoTracking()
            .Where(x => enrollmentIds.Contains(x.EnrollmentId))
            .Select(x => new { x.EnrollmentId, x.CompletedMinutes })
            .ToListAsync(ct);
        var workplaceMinutes = workplaceRows
            .GroupBy(x => x.EnrollmentId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.CompletedMinutes));

        var topicRows = await db.LearnerTopicProgressRows.AsNoTracking()
            .Where(x => enrollmentIds.Contains(x.EnrollmentId))
            .Select(x => new { x.EnrollmentId, x.Status })
            .ToListAsync(ct);
        var topics = topicRows
            .GroupBy(x => x.EnrollmentId)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    Prepared = g.Count(x => x.Status != TopicProgressStatus.NotStarted),
                    Presented = g.Count(x => x.Status is TopicProgressStatus.Presented or TopicProgressStatus.Validated or TopicProgressStatus.Rework),
                    Validated = g.Count(x => x.Status == TopicProgressStatus.Validated),
                    Rework = g.Count(x => x.Status == TopicProgressStatus.Rework)
                });

        var competencyRows = await db.LearnerCompetencyRecords.AsNoTracking()
            .Where(x => enrollmentIds.Contains(x.EnrollmentId))
            .Join(db.CompetencyDefinitions.AsNoTracking().Where(x => x.ReferentialVersionId == cohort.ReferentialVersionId),
                record => record.CompetencyDefinitionId,
                definition => definition.Id,
                (record, definition) => new
                {
                    record.EnrollmentId,
                    record.Score,
                    definition.Code,
                    definition.ParentId
                })
            .ToListAsync(ct);

        var competencyByEnrollment = competencyRows.ToLookup(x => x.EnrollmentId);

        return enrollmentRows
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .Select(row =>
            {
                attendance.TryGetValue(row.Id, out var hours);
                topics.TryGetValue(row.Id, out var sheetStats);
                var competencyRowsForLearner = competencyByEnrollment[row.Id];

                var scored = competencyRowsForLearner
                    .Where(x => x.Score.HasValue)
                    .Select(x => x.Score!.Value)
                    .ToArray();
                var progress = scored.Length == 0 ? 0m : Math.Round(scored.Average(), 2);

                var competencies = competencyRowsForLearner
                    .Where(x => x.ParentId is null && !string.IsNullOrWhiteSpace(x.Code))
                    .GroupBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key.ToUpperInvariant(),
                        g => g.Where(x => x.Score.HasValue).Select(x => x.Score!.Value).DefaultIfEmpty(0m).Average(),
                        StringComparer.OrdinalIgnoreCase);

                var workplace = workplaceMinutes.GetValueOrDefault(row.Id, 0);
                var internship = workplace > 0 ? workplace : hours?.InternshipSession ?? 0;
                var classroom = hours?.Classroom ?? 0;
                var driving = hours?.Driving ?? 0;
                var completed = classroom + driving + internship;
                var attendanceExpected = hours?.Expected ?? 0;
                var attendancePresent = hours?.Present ?? 0;
                var attendanceRate = attendanceExpected == 0
                    ? 0m
                    : Math.Round(attendancePresent * 100m / attendanceExpected, 2);

                return new CohortLearnerDashboardDto(
                    row.Id.Value,
                    row.LearnerProfileId.Value,
                    row.FirstName ?? string.Empty,
                    row.LastName ?? string.Empty,
                    row.Status.ToString().ToLowerInvariant(),
                    Math.Max(0, totalHours) * 60,
                    completed,
                    hours?.Catchup ?? 0,
                    sheetStats?.Prepared ?? 0,
                    sheetStats?.Presented ?? 0,
                    sheetStats?.Validated ?? 0,
                    sheetStats?.Rework ?? 0,
                    Math.Max(0, totalTopics),
                    progress,
                    competencies,
                    attendanceExpected,
                    attendancePresent,
                    attendanceRate,
                    hours?.PresentCount ?? 0,
                    hours?.LateCount ?? 0,
                    hours?.AbsentCount ?? 0,
                    hours?.ExcusedCount ?? 0,
                    classroom,
                    driving,
                    internship);
            })
            .ToArray();
    }

    public async Task<IReadOnlyCollection<CohortDrivingObservationDto>> GetCohortDrivingObservationsAsync(
        Guid cohortId, Guid? organizationScope, int take = 20, CancellationToken ct = default)
    {
        var typedCohortId = new CohortId(cohortId);
        var enrollmentQuery = db.Enrollments.AsNoTracking().Where(x => x.CohortId == typedCohortId);
        if (organizationScope.HasValue)
        {
            var organizationId = organizationScope.Value;
            enrollmentQuery = enrollmentQuery.Where(x => x.OrganizationId == organizationId);
        }

        var learners = await enrollmentQuery
            .Join(db.LearnerProfiles.AsNoTracking(), enrollment => enrollment.LearnerProfileId, profile => profile.Id,
                (enrollment, profile) => new { enrollment.Id, profile.PersonId })
            .Join(db.People.AsNoTracking(), x => x.PersonId, person => person.Id,
                (x, person) => new { x.Id, person.FirstName, person.LastName })
            .ToListAsync(ct);
        if (learners.Count == 0)
            return Array.Empty<CohortDrivingObservationDto>();

        var names = learners.ToDictionary(
            x => x.Id,
            x => $"{x.FirstName ?? string.Empty} {x.LastName ?? string.Empty}".Trim());
        var enrollmentIds = learners.Select(x => x.Id).ToArray();

        var rows = await db.DrivingEvaluations.AsNoTracking()
            .Where(x => enrollmentIds.Contains(x.EnrollmentId))
            .Join(db.CompetencyDefinitions.AsNoTracking(),
                evaluation => evaluation.CompetencyDefinitionId,
                definition => definition.Id,
                (evaluation, definition) => new
                {
                    evaluation.Id,
                    evaluation.EnrollmentId,
                    evaluation.EvaluatedAtUtc,
                    definition.Code,
                    evaluation.Subject,
                    evaluation.Positive,
                    evaluation.Difficulty,
                    evaluation.NextGoal
                })
            .OrderByDescending(x => x.EvaluatedAtUtc)
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(ct);

        return rows.Select(x => new CohortDrivingObservationDto(
            x.Id.Value,
            x.EnrollmentId.Value,
            names.GetValueOrDefault(x.EnrollmentId, string.Empty),
            x.EvaluatedAtUtc,
            x.Code ?? string.Empty,
            x.Subject ?? string.Empty,
            x.Positive ?? string.Empty,
            x.Difficulty ?? string.Empty,
            x.NextGoal ?? string.Empty)).ToArray();
    }

    public async Task<CohortLearnerDashboardDto?> GetMyLearnerDashboardAsync(
        Guid authGateUserId, Guid? organizationScope, Guid? cohortId = null, CancellationToken ct = default)
    {
        var enrollmentQuery = db.Enrollments.AsNoTracking()
            .Join(db.LearnerProfiles.AsNoTracking().Where(x => x.AuthGateUserId == authGateUserId),
                enrollment => enrollment.LearnerProfileId,
                profile => profile.Id,
                (enrollment, profile) => enrollment);
        if (organizationScope.HasValue)
        {
            var organizationId = organizationScope.Value;
            enrollmentQuery = enrollmentQuery.Where(x => x.OrganizationId == organizationId);
        }
        if (cohortId.HasValue)
        {
            var typedCohortId = new CohortId(cohortId.Value);
            enrollmentQuery = enrollmentQuery.Where(x => x.CohortId == typedCohortId);
        }

        var enrollment = await enrollmentQuery
            .OrderByDescending(x => x.Status == EnrollmentStatus.Active)
            .ThenByDescending(x => x.EnrolledOn)
            .Select(x => new { x.Id, x.CohortId })
            .FirstOrDefaultAsync(ct);
        if (enrollment is null)
            return null;

        var rows = await GetCohortLearnerDashboardsAsync(enrollment.CohortId.Value, organizationScope, ct);
        return rows.SingleOrDefault(x => x.EnrollmentId == enrollment.Id.Value);
    }

    public Task<LearnerDetailReportDto?> GetLearnerDetailAsync(
        Guid learnerProfileId, Guid? organizationScope, Guid? cohortId = null, CancellationToken ct = default) =>
        GetLearnerDetailCoreAsync(learnerProfileId, organizationScope, cohortId, ct);

    public async Task<LearnerDetailReportDto?> GetMyLearnerDetailAsync(
        Guid authGateUserId, Guid? organizationScope, Guid? cohortId = null, CancellationToken ct = default)
    {
        var learnerProfileId = await db.LearnerProfiles.AsNoTracking()
            .Where(x => x.AuthGateUserId == authGateUserId)
            .Select(x => (Guid?)x.Id.Value)
            .FirstOrDefaultAsync(ct);
        return learnerProfileId.HasValue
            ? await GetLearnerDetailCoreAsync(learnerProfileId.Value, organizationScope, cohortId, ct)
            : null;
    }

    private async Task<LearnerDetailReportDto?> GetLearnerDetailCoreAsync(
        Guid learnerProfileId, Guid? organizationScope, Guid? cohortId, CancellationToken ct)
    {
        var typedProfileId = new LearnerProfileId(learnerProfileId);
        var enrollmentQuery = db.Enrollments.AsNoTracking()
            .Where(x => x.LearnerProfileId == typedProfileId);
        if (organizationScope.HasValue)
        {
            var organizationId = organizationScope.Value;
            enrollmentQuery = enrollmentQuery.Where(x => x.OrganizationId == organizationId);
        }
        if (cohortId.HasValue)
        {
            var typedCohortId = new CohortId(cohortId.Value);
            enrollmentQuery = enrollmentQuery.Where(x => x.CohortId == typedCohortId);
        }

        var header = await enrollmentQuery
            .Join(db.LearnerProfiles.AsNoTracking(), e => e.LearnerProfileId, p => p.Id, (e, p) => new { e, p })
            .Join(db.People.AsNoTracking(), x => x.p.PersonId, p => p.Id, (x, person) => new { x.e, x.p, person })
            .Join(db.Cohorts.AsNoTracking(), x => x.e.CohortId, c => c.Id, (x, cohort) => new { x.e, x.p, x.person, cohort })
            .Join(db.ProgramOfferings.AsNoTracking(), x => x.cohort.ProgramOfferingId, o => o.Id, (x, offering) => new
            {
                EnrollmentId = x.e.Id,
                x.e.OrganizationId,
                x.e.LearnerProfileId,
                x.e.Status,
                x.e.EnrolledOn,
                x.person.FirstName,
                x.person.LastName,
                x.person.Email,
                CohortId = x.cohort.Id,
                x.cohort.SiteId,
                x.cohort.ReferentialVersionId,
                x.cohort.ExternalKey,
                CohortName = x.cohort.Name,
                CohortStartDate = x.cohort.StartDate,
                CohortEndDate = x.cohort.EndDate,
                offering.ProgramId
            })
            .OrderByDescending(x => x.Status == EnrollmentStatus.Active)
            .ThenByDescending(x => x.EnrolledOn)
            .FirstOrDefaultAsync(ct);
        if (header is null)
            return null;

        var summary = (await GetCohortLearnerDashboardsAsync(header.CohortId.Value, organizationScope, ct))
            .SingleOrDefault(x => x.EnrollmentId == header.EnrollmentId.Value);
        if (summary is null)
            return null;

        var attendanceRows = await db.AttendanceSheets.AsNoTracking()
            .Where(x => x.CohortId == header.CohortId)
            .Join(db.TrainingSessions.AsNoTracking(), sheet => sheet.SessionId, session => session.Id, (sheet, session) => new { sheet, session })
            .SelectMany(x => x.sheet.Entries
                .Where(e => e.EnrollmentId == header.EnrollmentId)
                .Select(e => new { x.session.StartsAtUtc, x.session.Title, e.Status, e.MissedMinutes }))
            .OrderByDescending(x => x.StartsAtUtc)
            .ToListAsync(ct);
        var attendance = attendanceRows.Select(x => new LearnerAttendanceDetailDto(
            x.StartsAtUtc, x.Title ?? string.Empty, x.Status.ToString().ToLowerInvariant(), x.MissedMinutes)).ToArray();

        var definitions = await db.CompetencyDefinitions.AsNoTracking()
            .Where(x => x.ReferentialVersionId == header.ReferentialVersionId && x.Active)
            .OrderBy(x => x.SortOrder)
            .Select(x => new { x.Id, x.ParentId, x.Code, x.Title, x.SortOrder })
            .ToListAsync(ct);
        var competencyRecords = await db.LearnerCompetencyRecords.AsNoTracking()
            .Where(x => x.EnrollmentId == header.EnrollmentId)
            .Select(x => new { x.CompetencyDefinitionId, x.Level, x.Score })
            .ToListAsync(ct);
        var competencyByDefinition = competencyRecords.ToDictionary(x => x.CompetencyDefinitionId);
        var skills = definitions.Where(x => x.ParentId is null).Select(root =>
        {
            competencyByDefinition.TryGetValue(root.Id, out var rootRecord);
            var children = definitions.Where(x => x.ParentId == root.Id).OrderBy(x => x.SortOrder).ToArray();
            var criterionDtos = children.Select(child =>
            {
                competencyByDefinition.TryGetValue(child.Id, out var record);
                return new LearnerSkillCriterionDetailDto(
                    child.Id.Value,
                    child.Code ?? string.Empty,
                    child.Title ?? string.Empty,
                    NormalizeCompetencyLevel(record?.Level),
                    record?.Score ?? 0m);
            }).ToArray();
            var childScores = criterionDtos.Where(x => x.Score > 0m).Select(x => x.Score).ToArray();
            var progress = rootRecord?.Score ?? (childScores.Length == 0 ? 0m : Math.Round(childScores.Average(), 2));
            return new LearnerSkillDetailDto(root.Id.Value, root.Code ?? string.Empty, root.Title ?? string.Empty, progress, criterionDtos);
        }).ToArray();

        var topicDefinitions = await db.PedagogicalTopics.AsNoTracking()
            .Where(x => x.ReferentialVersionId == header.ReferentialVersionId && x.Active)
            .OrderBy(x => x.Number ?? int.MaxValue)
            .ThenBy(x => x.Code)
            .Select(x => new { x.Id, x.Code, x.Number, x.Title, x.Category })
            .ToListAsync(ct);
        var topicProgressRows = await db.LearnerTopicProgressRows.AsNoTracking()
            .Where(x => x.EnrollmentId == header.EnrollmentId)
            .Select(x => new { x.TopicId, x.Status, x.PreparationDate, x.PresentationDate })
            .ToListAsync(ct);
        var progressByTopic = topicProgressRows.ToDictionary(x => x.TopicId);
        var topics = topicDefinitions.Select(topic =>
        {
            progressByTopic.TryGetValue(topic.Id, out var progress);
            return new LearnerTopicDetailDto(
                topic.Id.Value, topic.Code ?? string.Empty, topic.Number, topic.Title ?? string.Empty, topic.Category ?? string.Empty,
                progress is null ? "not_started" : NormalizeTopicStatus(progress.Status),
                progress?.PreparationDate, progress?.PresentationDate);
        }).ToArray();

        var drivingRows = await db.DrivingEvaluations.AsNoTracking()
            .Where(x => x.EnrollmentId == header.EnrollmentId)
            .Join(db.CompetencyDefinitions.AsNoTracking(), evaluation => evaluation.CompetencyDefinitionId, definition => definition.Id, (evaluation, definition) => new
            {
                evaluation.Id,
                evaluation.EvaluatedAtUtc,
                CompetencyCode = definition.Code,
                evaluation.Subject,
                evaluation.TrainerDisplayName,
                evaluation.Positive,
                evaluation.Difficulty,
                evaluation.NextGoal
            })
            .OrderByDescending(x => x.EvaluatedAtUtc)
            .ToListAsync(ct);
        var drivingIds = drivingRows.Select(x => x.Id).ToArray();
        var drivingCriteria = await db.DrivingEvaluationCriteria.AsNoTracking()
            .Where(x => drivingIds.Contains(x.DrivingEvaluationId))
            .Select(x => new { x.DrivingEvaluationId, x.Code, x.Label, x.Level })
            .ToListAsync(ct);
        var criteriaByDriving = drivingCriteria.ToLookup(x => x.DrivingEvaluationId);
        var driving = drivingRows.Select(x => new LearnerDrivingDetailDto(
            x.Id.Value,
            x.EvaluatedAtUtc,
            x.CompetencyCode ?? string.Empty,
            x.Subject ?? string.Empty,
            x.TrainerDisplayName ?? string.Empty,
            x.Positive ?? string.Empty,
            x.Difficulty ?? string.Empty,
            x.NextGoal ?? string.Empty,
            criteriaByDriving[x.Id].Select(c => new LearnerDrivingCriterionDetailDto(c.Code ?? string.Empty, c.Label ?? string.Empty, NormalizeCompetencyLevel(c.Level))).ToArray())).ToArray();

        var workplaceRows = await db.WorkplacePeriods.AsNoTracking()
            .Where(x => x.EnrollmentId == header.EnrollmentId)
            .OrderByDescending(x => x.StartDate)
            .Select(x => new
            {
                x.Id,
                x.Company,
                x.City,
                x.TutorName,
                x.StartDate,
                x.EndDate,
                x.PlannedMinutes,
                x.CompletedMinutes,
                x.Status,
                x.TutorObservation
            })
            .ToListAsync(ct);
        var workplaceIds = workplaceRows.Select(x => x.Id).ToArray();
        var workplaceActivities = await db.WorkplaceActivities.AsNoTracking()
            .Where(x => workplaceIds.Contains(x.PeriodId))
            .Join(db.WorkplaceActivityDefinitions.AsNoTracking(), activity => activity.DefinitionId, definition => definition.Id, (activity, definition) => new
            {
                activity.PeriodId,
                definition.Code,
                definition.Title,
                activity.Status
            })
            .ToListAsync(ct);
        var workplaceEvaluations = await db.WorkplaceEvaluations.AsNoTracking()
            .Where(x => workplaceIds.Contains(x.PeriodId))
            .Select(x => new
            {
                x.PeriodId,
                x.Kind,
                x.EvaluatorDisplayName,
                x.EvaluatedAtUtc,
                x.Summary,
                x.Strengths,
                x.ImprovementAreas,
                x.Validated
            })
            .ToListAsync(ct);
        var activitiesByPeriod = workplaceActivities.ToLookup(x => x.PeriodId);
        var evaluationsByPeriod = workplaceEvaluations.ToLookup(x => x.PeriodId);
        var workplace = workplaceRows.Select(x => new LearnerWorkplaceDetailDto(
            x.Id.Value,
            x.Company ?? string.Empty,
            x.City ?? string.Empty,
            x.TutorName ?? string.Empty,
            x.StartDate,
            x.EndDate,
            Math.Round(x.PlannedMinutes / 60m, 2),
            Math.Round(x.CompletedMinutes / 60m, 2),
            NormalizeWorkplaceStatus(x.Status),
            x.TutorObservation ?? string.Empty,
            activitiesByPeriod[x.Id].Select(a => new LearnerWorkplaceActivityDetailDto(a.Code ?? string.Empty, a.Title ?? string.Empty, NormalizeWorkplaceActivityStatus(a.Status))).ToArray(),
            evaluationsByPeriod[x.Id].OrderByDescending(e => e.EvaluatedAtUtc).Select(e => new LearnerWorkplaceEvaluationDetailDto(
                e.Kind.ToString().ToLowerInvariant(), e.EvaluatorDisplayName ?? string.Empty, e.EvaluatedAtUtc,
                e.Summary ?? string.Empty, e.Strengths ?? string.Empty, e.ImprovementAreas ?? string.Empty, e.Validated)).ToArray())).ToArray();

        var documentRows = await db.Documents.AsNoTracking()
            .Where(x => x.OrganizationId == header.OrganizationId && x.Status == DocumentStatus.Active && x.OwnerType == DocumentOwnerType.Enrollment && x.OwnerId == header.EnrollmentId.Value)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new { x.Id, x.Title, x.Category, x.CreatedAtUtc })
            .ToListAsync(ct);
        var documentIds = documentRows.Select(x => x.Id).ToArray();
        var versions = await db.DocumentVersions.AsNoTracking()
            .Where(x => documentIds.Contains(x.DocumentId))
            .OrderByDescending(x => x.VersionNumber)
            .Select(x => new { x.DocumentId, x.VersionNumber, x.SizeBytes, x.FileName })
            .ToListAsync(ct);
        var latestVersion = versions.GroupBy(x => x.DocumentId).ToDictionary(g => g.Key, g => g.First());
        var documents = documentRows.Select(x =>
        {
            latestVersion.TryGetValue(x.Id, out var version);
            return new LearnerDocumentDetailDto(x.Id.Value, x.Title ?? string.Empty, x.Category.ToString().ToLowerInvariant(), x.CreatedAtUtc, version?.SizeBytes ?? 0, version?.FileName ?? string.Empty);
        }).ToArray();

        var candidateRow = await db.CertificationCandidates.AsNoTracking()
            .Where(x => x.EnrollmentId == header.EnrollmentId)
            .Join(db.CertificationExamSessions.AsNoTracking(), candidate => candidate.ExamSessionId, session => session.Id, (candidate, session) => new { candidate, session })
            .OrderByDescending(x => x.session.StartsAtUtc)
            .Select(x => new
            {
                CandidateId = x.candidate.Id,
                x.candidate.Status,
                x.candidate.Eligible,
                x.candidate.Decision,
                x.candidate.EligibilitySnapshotJson,
                SessionId = x.session.Id,
                x.session.SchemeId,
                x.session.Title,
                x.session.StartsAtUtc,
                x.session.EndsAtUtc
            })
            .FirstOrDefaultAsync(ct);

        LearnerCertificationDetailDto? certification = null;
        if (candidateRow is not null)
        {
            var schemeUnits = await db.CertificationUnits.AsNoTracking()
                .Where(x => x.SchemeId == candidateRow.SchemeId)
                .Select(x => new { x.Id, x.Code })
                .ToListAsync(ct);
            var unitCodeById = schemeUnits.ToDictionary(x => x.Id, x => x.Code ?? string.Empty);
            var schemeSteps = await db.CertificationStepDefinitions.AsNoTracking()
                .Where(x => x.SchemeId == candidateRow.SchemeId)
                .OrderBy(x => x.SortOrder)
                .Select(x => new { x.Id, x.UnitId, x.Code, x.Title, x.DurationMinutes })
                .ToListAsync(ct);
            var assessmentRows = await db.CertificationAssessments.AsNoTracking()
                .Where(x => x.CandidateId == candidateRow.CandidateId)
                .Select(x => new { x.StepDefinitionId, x.Outcome })
                .ToListAsync(ct);
            var assessmentByStep = assessmentRows.ToDictionary(x => x.StepDefinitionId);
            certification = new LearnerCertificationDetailDto(
                candidateRow.CandidateId.Value,
                candidateRow.SessionId.Value,
                candidateRow.Title ?? string.Empty,
                candidateRow.StartsAtUtc,
                candidateRow.EndsAtUtc,
                candidateRow.Status.ToString().ToLowerInvariant(),
                candidateRow.Eligible,
                candidateRow.Decision.ToString().ToLowerInvariant(),
                ReadEligibilityBlockers(candidateRow.EligibilitySnapshotJson),
                schemeSteps.Select(step => new LearnerCertificationStepDetailDto(
                    step.Id.Value,
                    step.Code ?? string.Empty,
                    step.Title ?? string.Empty,
                    step.UnitId.HasValue && unitCodeById.TryGetValue(step.UnitId.Value, out var unitCode) ? unitCode : string.Empty,
                    step.DurationMinutes,
                    assessmentByStep.TryGetValue(step.Id, out var assessment) ? assessment.Outcome.ToString().ToLowerInvariant() : "pending")).ToArray());
        }

        var enrollmentIdText = header.EnrollmentId.Value.ToString();
        var profileIdText = header.LearnerProfileId.Value.ToString();
        var history = await db.AuditEntries.AsNoTracking()
            .Where(x => x.OrganizationId == header.OrganizationId)
            .Where(x => x.EntityId == enrollmentIdText || x.EntityId == profileIdText)
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(50)
            .Select(x => new LearnerHistoryDetailDto(x.Id.Value, x.OccurredAtUtc, x.Action, x.UserDisplayName ?? string.Empty))
            .ToListAsync(ct);

        return new LearnerDetailReportDto(
            header.EnrollmentId.Value,
            header.LearnerProfileId.Value,
            header.CohortId.Value,
            header.SiteId,
            header.ProgramId,
            header.ReferentialVersionId,
            header.ExternalKey ?? header.CohortId.Value.ToString(),
            header.CohortName ?? string.Empty,
            header.CohortStartDate,
            header.CohortEndDate,
            header.FirstName ?? string.Empty,
            header.LastName ?? string.Empty,
            header.Email ?? string.Empty,
            header.Status.ToString().ToLowerInvariant(),
            summary,
            attendance,
            skills,
            topics,
            driving,
            workplace,
            documents,
            certification,
            history);
    }

    public async Task<IReadOnlyCollection<CertificationSuccessRecordDto>> GetCertificationSuccessAsync(
        Guid organizationId, CancellationToken ct = default)
    {
        var cohorts = await db.Cohorts.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId)
            .Join(db.ProgramOfferings.AsNoTracking(), cohort => cohort.ProgramOfferingId, offering => offering.Id, (cohort, offering) => new
            {
                CohortId = cohort.Id,
                cohort.ExternalKey,
                cohort.Name,
                cohort.StartDate,
                cohort.EndDate,
                cohort.SiteId,
                offering.ProgramId
            })
            .ToListAsync(ct);
        if (cohorts.Count == 0)
            return Array.Empty<CertificationSuccessRecordDto>();

        var cohortIds = cohorts.Select(x => x.CohortId).ToArray();
        var sessions = await db.CertificationExamSessions.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && cohortIds.Contains(x.CohortId) && x.Status == CertificationExamSessionStatus.Published)
            .Select(x => new { x.Id, x.CohortId, x.SchemeId, x.Title, x.StartsAtUtc, x.Status })
            .ToListAsync(ct);
        if (sessions.Count == 0)
            return Array.Empty<CertificationSuccessRecordDto>();

        var sessionIds = sessions.Select(x => x.Id).ToArray();
        var candidates = await db.CertificationCandidates.AsNoTracking()
            .Where(x => sessionIds.Contains(x.ExamSessionId) && x.Decision != CertificationDecision.Pending)
            .Select(x => new { x.Id, x.ExamSessionId, x.EnrollmentId, x.Decision, x.DecisionAtUtc })
            .ToListAsync(ct);
        if (candidates.Count == 0)
            return Array.Empty<CertificationSuccessRecordDto>();

        var enrollmentIds = candidates.Select(x => x.EnrollmentId).Distinct().ToArray();
        var people = await db.Enrollments.AsNoTracking()
            .Where(x => enrollmentIds.Contains(x.Id))
            .Join(db.LearnerProfiles.AsNoTracking(), e => e.LearnerProfileId, p => p.Id, (e, p) => new { e.Id, p.PersonId })
            .Join(db.People.AsNoTracking(), x => x.PersonId, p => p.Id, (x, person) => new { EnrollmentId = x.Id, person.FirstName, person.LastName })
            .ToListAsync(ct);
        var peopleByEnrollment = people.ToDictionary(x => x.EnrollmentId);

        var schemeIds = sessions.Select(x => x.SchemeId).Distinct().ToArray();
        var units = await db.CertificationUnits.AsNoTracking()
            .Where(x => schemeIds.Contains(x.SchemeId))
            .OrderBy(x => x.SortOrder)
            .Select(x => new { x.Id, x.SchemeId, x.Code, x.SortOrder })
            .ToListAsync(ct);
        var steps = await db.CertificationStepDefinitions.AsNoTracking()
            .Where(x => schemeIds.Contains(x.SchemeId))
            .Select(x => new { x.Id, x.SchemeId, x.UnitId, x.SortOrder })
            .ToListAsync(ct);
        var candidateIds = candidates.Select(x => x.Id).ToArray();
        var assessments = await db.CertificationAssessments.AsNoTracking()
            .Where(x => candidateIds.Contains(x.CandidateId))
            .Select(x => new { x.CandidateId, x.StepDefinitionId, x.Outcome })
            .ToListAsync(ct);
        var assessmentsByCandidate = assessments.ToLookup(x => x.CandidateId);
        var sessionById = sessions.ToDictionary(x => x.Id);

        var records = new List<CertificationSuccessRecordDto>();
        foreach (var cohort in cohorts)
        {
            var cohortSessions = sessions.Where(x => x.CohortId == cohort.CohortId).OrderBy(x => x.StartsAtUtc).ToArray();
            if (cohortSessions.Length == 0)
                continue;
            var cohortSessionIds = cohortSessions.Select(x => x.Id).ToHashSet();
            var chosenCandidates = candidates
                .Where(x => cohortSessionIds.Contains(x.ExamSessionId))
                .GroupBy(x => x.EnrollmentId)
                .Select(g => g.OrderByDescending(x => x.DecisionAtUtc ?? sessionById[x.ExamSessionId].StartsAtUtc.UtcDateTime)
                    .ThenByDescending(x => sessionById[x.ExamSessionId].StartsAtUtc)
                    .First())
                .ToArray();
            if (chosenCandidates.Length == 0)
                continue;

            var latestSession = cohortSessions.OrderByDescending(x => x.StartsAtUtc).First();
            var candidateDtos = chosenCandidates.Select(candidate =>
            {
                peopleByEnrollment.TryGetValue(candidate.EnrollmentId, out var person);
                var session = sessionById[candidate.ExamSessionId];
                var schemeUnits = units.Where(x => x.SchemeId == session.SchemeId).OrderBy(x => x.SortOrder).ToArray();
                var schemeSteps = steps.Where(x => x.SchemeId == session.SchemeId).ToArray();
                var candidateAssessment = assessmentsByCandidate[candidate.Id].ToDictionary(x => x.StepDefinitionId);
                var unitResults = schemeUnits.Select(unit =>
                {
                    var unitStepIds = schemeSteps.Where(x => x.UnitId == unit.Id).OrderBy(x => x.SortOrder).Select(x => x.Id).ToArray();
                    var validated = unitStepIds.Length > 0 && unitStepIds.All(stepId => candidateAssessment.TryGetValue(stepId, out var assessment) && assessment.Outcome == CertificationAssessmentOutcome.Passed);
                    return new CertificationSuccessUnitResultDto(unit.Code ?? string.Empty, validated);
                }).ToArray();
                return new CertificationSuccessCandidateDto(
                    candidate.Id.Value,
                    candidate.EnrollmentId.Value,
                    person?.FirstName ?? string.Empty,
                    person?.LastName ?? string.Empty,
                    string.Empty,
                    NormalizeCertificationDecision(candidate.Decision),
                    unitResults);
            }).OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ToArray();

            var graduated = chosenCandidates.Count(x => x.Decision == CertificationDecision.Obtained);
            var partial = chosenCandidates.Count(x => x.Decision == CertificationDecision.Partial);
            var failed = chosenCandidates.Count(x => x.Decision == CertificationDecision.Failed);
            var absent = chosenCandidates.Count(x => x.Decision == CertificationDecision.Absent);
            var presented = graduated + partial + failed;
            records.Add(new CertificationSuccessRecordDto(
                cohort.CohortId.Value,
                cohort.ExternalKey ?? cohort.CohortId.Value.ToString(),
                organizationId,
                cohort.SiteId,
                cohort.ProgramId,
                cohort.Name ?? string.Empty,
                cohort.StartDate,
                cohort.EndDate,
                latestSession.Id.Value,
                latestSession.Title ?? string.Empty,
                latestSession.StartsAtUtc,
                presented,
                graduated,
                partial,
                failed,
                absent,
                presented == 0 ? 0m : Math.Round(graduated * 100m / presented, 1),
                candidateDtos));
        }
        return records.OrderByDescending(x => x.CohortEndDate).ThenBy(x => x.CohortName).ToArray();
    }

    public async Task<IReadOnlyCollection<ReportingTrendPointDto>> GetAttendanceTrendAsync(
        Guid cohortId, Guid? organizationScope, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var fromUtc = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        var toUtc = new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));

        var typedCohortId = new CohortId(cohortId);

        var sessionsQuery = db.TrainingSessions.AsNoTracking()
            .Where(x =>
                x.CohortId == typedCohortId &&
                x.StartsAtUtc >= fromUtc &&
                x.StartsAtUtc < toUtc);

        if (organizationScope.HasValue)
        {
            var organizationId = organizationScope.Value;
            sessionsQuery = sessionsQuery.Where(x => x.OrganizationId == organizationId);
        }

        var sessions = sessionsQuery
            .Select(x => new { x.Id, x.StartsAtUtc });

        var raw = await db.AttendanceSheets.AsNoTracking()
            .Where(x => x.CohortId == typedCohortId)
            .Join(sessions, sheet => sheet.SessionId, session => session.Id, (sheet, session) => new { sheet, session })
            .SelectMany(x => x.sheet.Entries.Select(e => new
            {
                x.session.StartsAtUtc,
                e.ExpectedMinutes,
                e.PresentMinutes
            }))
            .ToListAsync(ct);

        return raw
            .GroupBy(x => DateOnly.FromDateTime(x.StartsAtUtc.UtcDateTime))
            .OrderBy(x => x.Key)
            .Select(g =>
            {
                var expected = g.Sum(x => x.ExpectedMinutes);
                var present = g.Sum(x => x.PresentMinutes);
                return new ReportingTrendPointDto(g.Key,
                    expected == 0 ? 0m : Math.Round(present * 100m / expected, 2));
            })
            .ToArray();
    }

    public async Task<PagedAuditDto> GetAuditAsync(
        Guid? organizationId, string? action, string? entityType, Guid? userId,
        DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize, CancellationToken ct = default)
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
        var items = await query.OrderByDescending(x => x.OccurredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AuditEntryDto(
                x.Id.Value, x.OrganizationId, x.UserId, x.UserDisplayName,
                x.Action, x.EntityType, x.EntityId, x.Route, x.CorrelationId,
                x.TraceId, x.IpAddress, x.OccurredAtUtc))
            .ToListAsync(ct);

        return new(items, page, pageSize, total);
    }

    public async Task<IReadOnlyCollection<CohortExportRowDto>> GetCohortExportRowsAsync(
        Guid cohortId, Guid? organizationScope, CancellationToken ct = default)
    {
        var typedCohortId = new CohortId(cohortId);

        var enrollmentQuery = db.Enrollments.AsNoTracking()
            .Where(x => x.CohortId == typedCohortId);

        if (organizationScope.HasValue)
        {
            var organizationId = organizationScope.Value;
            enrollmentQuery = enrollmentQuery.Where(x => x.OrganizationId == organizationId);
        }

        var enrollments = await enrollmentQuery
            .Select(x => new { x.Id, x.LearnerProfileId, x.Status })
            .ToListAsync(ct);
        if (enrollments.Count == 0)
            return Array.Empty<CohortExportRowDto>();

        var profileIds = enrollments.Select(x => x.LearnerProfileId).Distinct().ToList();
        var profiles = await db.LearnerProfiles.AsNoTracking()
            .Where(x => profileIds.Contains(x.Id))
            .Select(x => new { x.Id, x.PersonId })
            .ToListAsync(ct);
        var personIds = profiles.Select(x => x.PersonId).Distinct().ToList();
        var people = await db.People.AsNoTracking()
            .Where(x => personIds.Contains(x.Id))
            .Select(x => new { x.Id, x.FirstName, x.LastName, x.Email })
            .ToListAsync(ct);

        var profileToPerson = profiles.ToDictionary(x => x.Id, x => x.PersonId);
        var personById = people.ToDictionary(x => x.Id);
        var summaries = (await GetCohortLearnerDashboardsAsync(cohortId, organizationScope, ct))
            .ToDictionary(x => x.EnrollmentId);
        return enrollments.Select(e =>
        {
            var personId = profileToPerson[e.LearnerProfileId];
            var p = personById[personId];
            summaries.TryGetValue(e.Id.Value, out var summary);
            return new CohortExportRowDto(
                e.Id.Value,
                p.LastName ?? string.Empty,
                p.FirstName ?? string.Empty,
                p.Email ?? string.Empty,
                e.Status.ToString(),
                summary?.PlannedMinutes ?? 0,
                summary?.CompletedMinutes ?? 0,
                summary?.CatchupMinutes ?? 0,
                summary?.ValidatedTopics ?? 0,
                summary?.TotalTopics ?? 0,
                summary?.AverageCompetencyProgress ?? 0m);
        }).ToArray();
    }

    private static string NormalizeCompetencyLevel(CompetencyLevel? level) => level switch
    {
        CompetencyLevel.Acquired => "acquired",
        CompetencyLevel.InProgress => "in_progress",
        CompetencyLevel.Rework => "rework",
        _ => "not_assessed"
    };

    private static string NormalizeTopicStatus(TopicProgressStatus status) => status switch
    {
        TopicProgressStatus.InProgress => "in_progress",
        TopicProgressStatus.Ready => "ready",
        TopicProgressStatus.Presented => "presented",
        TopicProgressStatus.Validated => "validated",
        TopicProgressStatus.Rework => "rework",
        _ => "not_started"
    };

    private static string NormalizeWorkplaceStatus(WorkplacePeriodStatus status) => status switch
    {
        WorkplacePeriodStatus.InProgress => "inProgress",
        WorkplacePeriodStatus.Completed => "completed",
        WorkplacePeriodStatus.Incomplete => "incomplete",
        WorkplacePeriodStatus.Cancelled => "cancelled",
        _ => "planned"
    };

    private static string NormalizeWorkplaceActivityStatus(WorkplaceActivityStatus status) => status switch
    {
        WorkplaceActivityStatus.Done => "done",
        WorkplaceActivityStatus.NotApplicable => "notApplicable",
        _ => "pending"
    };

    private static string NormalizeCertificationDecision(CertificationDecision decision) => decision switch
    {
        CertificationDecision.Obtained => "obtained",
        CertificationDecision.Partial => "partial",
        CertificationDecision.Failed => "failed",
        CertificationDecision.Absent => "absent",
        _ => "absent"
    };

    private static IReadOnlyCollection<string> ReadEligibilityBlockers(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Array.Empty<string>();
        try
        {
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("blockers", out var blockers) || blockers.ValueKind != JsonValueKind.Array)
                return Array.Empty<string>();
            return blockers.EnumerateArray()
                .Where(x => x.ValueKind == JsonValueKind.String)
                .Select(x => x.GetString() ?? string.Empty)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

    private async Task<decimal> AttendanceRateAsync(
        Guid organizationId,
        IReadOnlyCollection<PedagoraPilot.Domain.Identifiers.CohortId> cohortIds,
        CancellationToken ct)
    {
        if (cohortIds.Count == 0)
            return 0m;
        var rows = await db.AttendanceSheets.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && cohortIds.Contains(x.CohortId))
            .SelectMany(x => x.Entries.Select(e => new { e.ExpectedMinutes, e.PresentMinutes }))
            .ToListAsync(ct);
        var expected = rows.Sum(x => x.ExpectedMinutes);
        return expected == 0 ? 0m : Math.Round(rows.Sum(x => x.PresentMinutes) * 100m / expected, 2);
    }

    private async Task<decimal> AverageProgressAsync(
        Guid organizationId,
        IReadOnlyCollection<PedagoraPilot.Domain.Identifiers.CohortId> cohortIds,
        CancellationToken ct)
    {
        if (cohortIds.Count == 0)
            return 0m;
        var enrollmentIds = await db.Enrollments.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && cohortIds.Contains(x.CohortId))
            .Select(x => x.Id)
            .ToListAsync(ct);
        if (enrollmentIds.Count == 0)
            return 0m;

        var scores = await db.LearnerCompetencyRecords.AsNoTracking()
            .Where(x => enrollmentIds.Contains(x.EnrollmentId) && x.Score.HasValue)
            .Select(x => x.Score!.Value)
            .ToListAsync(ct);
        return scores.Count == 0 ? 0m : Math.Round(scores.Average(), 2);
    }

    private async Task<decimal> CertificationSuccessAsync(
        Guid organizationId,
        IReadOnlyCollection<PedagoraPilot.Domain.Identifiers.CohortId> cohortIds,
        CancellationToken ct)
    {
        if (cohortIds.Count == 0)
            return 0m;
        var enrollmentIds = await db.Enrollments.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && cohortIds.Contains(x.CohortId))
            .Select(x => x.Id)
            .ToListAsync(ct);
        if (enrollmentIds.Count == 0)
            return 0m;

        var decisions = await db.CertificationCandidates.AsNoTracking()
            .Where(x => enrollmentIds.Contains(x.EnrollmentId) && x.Decision != CertificationDecision.Pending)
            .Select(x => x.Decision)
            .ToListAsync(ct);
        return decisions.Count == 0
            ? 0m
            : Math.Round(decisions.Count(x => x == CertificationDecision.Obtained) * 100m / decisions.Count, 2);
    }
}
