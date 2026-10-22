using System.Text.Json;
using DomainRelay.Abstractions;
using DomainRelay.Mapping.Abstractions;
using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Application.Training.Learners;
using PedagoraPilot.Contracts.Certification;
using PedagoraPilot.Domain.Certification;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Training;

namespace PedagoraPilot.Application.Certification;
internal static class CertMap
{
    public static CertificationSchemeDto Scheme(CertificationScheme x) => new(x.Id.Value, x.ReferentialVersionId, x.Code, x.Name, x.Status.ToString(), x.EffectiveFrom, x.EffectiveTo, x.Units.OrderBy(u => u.SortOrder).Select(u => new CertificationUnitDto(u.Id.Value, u.Code, u.Title, u.SortOrder)).ToArray(), x.Steps.OrderBy(s => s.SortOrder).Select(s => new CertificationStepDto(s.Id.Value, s.UnitId?.Value, s.Code, s.Title, s.Kind.ToString(), s.DurationMinutes, s.SortOrder)).ToArray());
    public static CertificationExamSessionDto Session(CertificationExamSession x) => new(x.Id.Value, x.OrganizationId, x.SiteId, x.CohortId.Value, x.SchemeId.Value, x.Title, x.StartsAtUtc, x.EndsAtUtc, x.Venue, x.Status.ToString());
    public static CertificationCandidateDto Candidate(CertificationCandidate x) => new(x.Id.Value, x.ExamSessionId.Value, x.EnrollmentId.Value, x.Status.ToString(), x.Eligible, x.Decision.ToString(), x.DecisionComment, x.DecisionAtUtc, x.Assessments.OrderBy(a => a.RecordedAtUtc).Select(a => new CertificationAssessmentDto(a.Id.Value, a.StepDefinitionId.Value, a.JuryDisplayName, a.Outcome.ToString(), a.Score, a.Comment, a.RecordedAtUtc)).ToArray());
    public static JuryAssignmentDto Jury(JuryAssignment x) => new(x.Id.Value, x.ExamSessionId.Value, x.AuthGateUserId, x.DisplayName, x.Role, x.AssignedAtUtc);
}

public sealed class GetCertificationSchemesQueryHandler(ICertificationSchemeRepository repo, ICurrentUser current,
    IReferentialVersionRepository versions, IReferentialRepository referentials,
    IProgramOfferingRepository offerings, ITrainingSiteRepository sites) : IRequestHandler<GetCertificationSchemesQuery, IReadOnlyCollection<CertificationSchemeDto>>
{
    public async Task<IReadOnlyCollection<CertificationSchemeDto>> Handle(GetCertificationSchemesQuery r, CancellationToken ct)
    {
        var q = repo.Query(false).Include(x => x.Units).Include(x => x.Steps).AsQueryable();
        var organizationId = TenantScope.Organization(current);
        if (organizationId.HasValue)
        {
            var candidates = await (from version in versions.Query(false)
                                    join referential in referentials.Query(false) on version.ReferentialId equals referential.Id
                                    join offering in offerings.Query(false) on referential.ProgramId equals offering.ProgramId
                                    join site in sites.Query(false) on offering.SiteId equals site.Id
                                    where offering.IsActive && site.OrganizationId == organizationId.Value
                                    select new { VersionId = version.Id, SiteId = site.Id, ProgramId = offering.ProgramId })
                .Distinct().ToListAsync(ct);
            var allowedVersionIds = candidates
                .Where(x => current.CanViewProgram(x.SiteId, x.ProgramId))
                .Select(x => x.VersionId).Distinct().ToArray();
            q = q.Where(x => allowedVersionIds.Contains(x.ReferentialVersionId));
        }
        if (r.ReferentialVersionId.HasValue)
            q = q.Where(x => x.ReferentialVersionId == r.ReferentialVersionId);
        var rows = await q.OrderBy(x => x.Name).ToListAsync(ct);
        return rows.Select(CertMap.Scheme).ToArray();
    }
}

public sealed class GetCertificationExamSessionsQueryHandler(ICertificationExamSessionRepository repo, ICohortRepository cohorts, IProgramOfferingRepository offerings, IJuryAssignmentRepository juries, ICurrentUser current) : IRequestHandler<GetCertificationExamSessionsQuery, IReadOnlyCollection<CertificationExamSessionDto>>
{
    public async Task<IReadOnlyCollection<CertificationExamSessionDto>> Handle(GetCertificationExamSessionsQuery r, CancellationToken ct)
    {
        var q = repo.Query(false);
        var organizationId = TenantScope.Organization(current);
        if (organizationId.HasValue)
            q = q.Where(x => x.OrganizationId == organizationId.Value);
        if (r.CohortId.HasValue)
            q = q.Where(x => x.CohortId == r.CohortId.Value);
        var rows = await q.OrderByDescending(x => x.StartsAtUtc).ToListAsync(ct);
        rows = await CertificationContextualAccess.FilterSessionsAsync(rows, cohorts, offerings, current, ct);
        if (CertificationActorAccess.IsJury(current) && !current.HasPermission(PedagoraPilot.Security.Contracts.PedagoraPilotPermissionCodes.Certification.Manage))
        {
            if (!current.UserId.HasValue) return Array.Empty<CertificationExamSessionDto>();
            var assignedIds = await juries.Query(false)
                .Where(x => x.OrganizationId == current.OrganizationId && x.AuthGateUserId == current.UserId.Value)
                .Select(x => x.ExamSessionId)
                .ToArrayAsync(ct);
            rows = rows.Where(x => assignedIds.Contains(x.Id)).ToList();
        }
        return rows.Select(CertMap.Session).ToArray();
    }
}

public sealed class GetCertificationCandidatesQueryHandler(
    ICertificationCandidateRepository repo,
    ICertificationExamSessionRepository sessions,
    ICohortRepository cohorts,
    IProgramOfferingRepository offerings,
    IEnrollmentRepository enrollments,
    ILearnerProfileRepository learnerProfiles,
    IPersonRepository people,
    IJuryAssignmentRepository juries,
    ICurrentUser current) : IRequestHandler<GetCertificationCandidatesQuery, IReadOnlyCollection<CertificationCandidateDto>>
{
    public async Task<IReadOnlyCollection<CertificationCandidateDto>> Handle(GetCertificationCandidatesQuery r, CancellationToken ct)
    {
        var session = await sessions.GetByIdAsync(r.SessionId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CertificationSessionNotFound);
        TenantScope.Ensure(current, session.OrganizationId);
        await CertificationContextualAccess.EnsureSessionAsync(session, cohorts, offerings, current, manage: false, ct);
        await CertificationActorAccess.EnsureJuryAssignmentIfRequiredAsync(session, juries, current, ct);
        var q = repo.Query(false).Include(x => x.Assessments).Where(x => x.ExamSessionId == r.SessionId).AsQueryable();
        var organizationId = TenantScope.Organization(current);
        if (organizationId.HasValue)
            q = q.Where(x => x.OrganizationId == organizationId.Value);

        var rows = await q.ToListAsync(ct);

        if (CertificationActorAccess.IsStudent(current))
        {
            var self = await LearnerSelfAccess.ResolveProfileAsync(learnerProfiles, people, current, ct)
                ?? throw new ForbiddenApplicationException(ErrorKeys.Forbidden);
            var selfEnrollmentIds = await enrollments.Query(false)
                .Where(x => x.LearnerProfileId == self.Id)
                .Select(x => x.Id)
                .ToArrayAsync(ct);
            rows = rows.Where(x => selfEnrollmentIds.Contains(x.EnrollmentId)).ToList();
        }

        if (rows.Count == 0)
            return Array.Empty<CertificationCandidateDto>();

        var enrollmentIds = rows.Select(x => x.EnrollmentId).Distinct().ToArray();
        var identities = await (from enrollment in enrollments.Query(false)
                                join profile in learnerProfiles.Query(false) on enrollment.LearnerProfileId equals profile.Id
                                join person in people.Query(false) on profile.PersonId equals person.Id
                                where enrollmentIds.Contains(enrollment.Id)
                                select new
                                {
                                    enrollment.Id,
                                    person.FirstName,
                                    person.LastName
                                })
            .ToDictionaryAsync(x => x.Id, ct);

        return rows.Select(candidate =>
        {
            identities.TryGetValue(candidate.EnrollmentId, out var identity);
            return CertMap.Candidate(candidate) with
            {
                FirstName = identity?.FirstName ?? string.Empty,
                LastName = identity?.LastName ?? string.Empty,
                CandidateNumber = string.Empty
            };
        }).ToArray();
    }
}

public sealed class CreateCertificationExamSessionCommandHandler(ICohortRepository cohorts, IProgramOfferingRepository offerings, ICertificationSchemeRepository schemes, ICertificationExamSessionRepository sessions, ICurrentUser current) : IRequestHandler<CreateCertificationExamSessionCommand, CertificationExamSessionDto>
{
    public async Task<CertificationExamSessionDto> Handle(CreateCertificationExamSessionCommand r, CancellationToken ct)
    {
        var c = await cohorts.GetByIdAsync(r.CohortId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
        var s = await schemes.GetByIdAsync(r.SchemeId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CertificationSchemeNotFound);
        if (s.Status != CertificationSchemeStatus.Published)
            throw new ConflictApplicationException(ErrorKeys.CertificationSchemeNotPublished);
        if (s.ReferentialVersionId != c.ReferentialVersionId)
            throw new ConflictApplicationException(ErrorKeys.CertificationSchemeReferentialMismatch);
        TenantScope.Ensure(current, c.OrganizationId);
        await ContextualScope.EnsureCanManageCohortAsync(current, c, offerings, ct);
        var x = CertificationExamSession.Create(c.OrganizationId, c.SiteId, c.Id, s.Id, r.Title, r.StartsAtUtc, r.EndsAtUtc, r.Venue);
        await sessions.AddAsync(x, ct);
        return CertMap.Session(x);
    }
}

public sealed class PlanCertificationExamSessionCommandHandler(ICertificationExamSessionRepository sessions, ICohortRepository cohorts, IProgramOfferingRepository offerings, ICurrentUser current) : IRequestHandler<PlanCertificationExamSessionCommand, CertificationExamSessionDto>
{
    public async Task<CertificationExamSessionDto> Handle(PlanCertificationExamSessionCommand r, CancellationToken ct)
    {
        var x = await sessions.GetByIdAsync(r.SessionId, true, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CertificationSessionNotFound);
        TenantScope.Ensure(current, x.OrganizationId);
        await CertificationContextualAccess.EnsureSessionAsync(x, cohorts, offerings, current, manage: true, ct);
        x.Plan();
        return CertMap.Session(x);
    }
}

// Explicit lifecycle transition. Publishing must never implicitly start an exam session.
public sealed class StartCertificationExamSessionCommandHandler(
    ICertificationExamSessionRepository sessions, ICohortRepository cohorts,
    IProgramOfferingRepository offerings, ICurrentUser current)
    : IRequestHandler<StartCertificationExamSessionCommand, CertificationExamSessionDto>
{
    public async Task<CertificationExamSessionDto> Handle(StartCertificationExamSessionCommand r, CancellationToken ct)
    {
        var session = await sessions.GetByIdAsync(r.SessionId, true, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.CertificationSessionNotFound);
        TenantScope.Ensure(current, session.OrganizationId);
        await CertificationContextualAccess.EnsureSessionAsync(session, cohorts, offerings, current, manage: true, ct);
        session.Start();
        return CertMap.Session(session);
    }
}

public sealed class RegisterCohortCandidatesCommandHandler(ICertificationExamSessionRepository sessions, ICohortRepository cohorts, IProgramOfferingRepository offerings, IEnrollmentRepository enrollments, ICertificationCandidateRepository candidates, ICurrentUser current) : IRequestHandler<RegisterCohortCandidatesCommand, int>
{
    public async Task<int> Handle(RegisterCohortCandidatesCommand r, CancellationToken ct)
    {
        var s = await sessions.GetByIdAsync(r.SessionId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CertificationSessionNotFound);
        TenantScope.Ensure(current, s.OrganizationId);
        await CertificationContextualAccess.EnsureSessionAsync(s, cohorts, offerings, current, manage: true, ct);
        if (s.Status is not (CertificationExamSessionStatus.Draft or CertificationExamSessionStatus.Planned))
            throw new ConflictApplicationException(ErrorKeys.CertificationSessionNotWritable);
        // A candidature does not resurrect a cancelled/withdrawn/pending enrollment.
        // A completed enrollment remains eligible for the final exam after training ends.
        var rows = await enrollments.Query(false)
            .Where(x => x.CohortId == s.CohortId && x.OrganizationId == s.OrganizationId
                && (x.Status == EnrollmentStatus.Active || x.Status == EnrollmentStatus.Completed))
            .ToListAsync(ct);
        var count = 0;
        foreach (var e in rows)
        {
            if (await candidates.ExistsAsync(s.Id, e.Id, ct))
                continue;
            await candidates.AddAsync(CertificationCandidate.Register(s.OrganizationId, s.Id, e.Id), ct);
            count++;
        }

        return count;
    }
}

public sealed class EvaluateCertificationEligibilityCommandHandler(ICertificationCandidateRepository candidates, ICertificationExamSessionRepository sessions, ICohortRepository cohorts, IProgramOfferingRepository offerings, ICurrentUser current) : IRequestHandler<EvaluateCertificationEligibilityCommand, CertificationCandidateDto>
{
    public async Task<CertificationCandidateDto> Handle(EvaluateCertificationEligibilityCommand r, CancellationToken ct)
    {
        var x = await candidates.Query(true).Include(a => a.Assessments).SingleOrDefaultAsync(a => a.Id == r.CandidateId, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CertificationCandidateNotFound);
        TenantScope.Ensure(current, x.OrganizationId);
        var examSession = await sessions.GetByIdAsync(x.ExamSessionId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CertificationSessionNotFound);
        await CertificationContextualAccess.EnsureSessionAsync(examSession, cohorts, offerings, current, manage: true, ct);
        if (examSession.Status is CertificationExamSessionStatus.Cancelled or CertificationExamSessionStatus.Completed or CertificationExamSessionStatus.Published)
            throw new ConflictApplicationException(ErrorKeys.CertificationSessionNotWritable);
        x.SetEligibility(r.Eligible, JsonSerializer.Serialize(new { eligible = r.Eligible, blockers = r.Blockers ?? [] }));
        return CertMap.Candidate(x);
    }
}

public sealed class RecordCertificationAssessmentCommandHandler(ICertificationCandidateRepository candidates, ICertificationSchemeRepository schemes, ICertificationExamSessionRepository sessions, ICohortRepository cohorts, IProgramOfferingRepository offerings, IJuryAssignmentRepository juries, ICurrentUser current) : IRequestHandler<RecordCertificationAssessmentCommand, CertificationCandidateDto>
{
    public async Task<CertificationCandidateDto> Handle(RecordCertificationAssessmentCommand r, CancellationToken ct)
    {
        var x = await candidates.Query(true).Include(a => a.Assessments).SingleOrDefaultAsync(a => a.Id == r.CandidateId, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CertificationCandidateNotFound);
        TenantScope.Ensure(current, x.OrganizationId);
        var session = await sessions.GetByIdAsync(x.ExamSessionId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CertificationSessionNotFound);
        await CertificationContextualAccess.EnsureSessionAsync(session, cohorts, offerings, current, manage: true, ct);
        await CertificationActorAccess.EnsureJuryAssignmentIfRequiredAsync(session, juries, current, ct);
        // Recording marks/assessments is only legitimate while the exam is running.
        if (session.Status != CertificationExamSessionStatus.InProgress)
            throw new ConflictApplicationException(ErrorKeys.CertificationSessionNotWritable);
        var scheme = await schemes.Query(false).Include(a => a.Steps).SingleOrDefaultAsync(a => a.Id == session.SchemeId, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CertificationSchemeNotFound);
        if (scheme.Steps.All(a => a.Id != r.StepDefinitionId))
            throw new ValidationApplicationException(ErrorKeys.CertificationStepNotFound);
        if (!Enum.TryParse<CertificationAssessmentOutcome>(r.Outcome, true, out var outcome) || !Enum.IsDefined(outcome))
            throw new ValidationApplicationException(ErrorKeys.CertificationAssessmentOutcomeInvalid);
        x.RecordAssessment(r.StepDefinitionId, r.JuryDisplayName, outcome, r.Score, r.Comment);
        return CertMap.Candidate(x);
    }
}


/// <summary>One command, one candidate aggregate, one UnitOfWork transaction.</summary>
public sealed class RecordCertificationAssessmentBatchCommandHandler(
    ICertificationCandidateRepository candidates,
    ICertificationSchemeRepository schemes,
    ICertificationExamSessionRepository sessions,
    ICohortRepository cohorts,
    IProgramOfferingRepository offerings,
    IJuryAssignmentRepository juries,
    ICurrentUser current) : IRequestHandler<RecordCertificationAssessmentBatchCommand, CertificationCandidateDto>
{
    public async Task<CertificationCandidateDto> Handle(RecordCertificationAssessmentBatchCommand r, CancellationToken ct)
    {
        var candidate = await candidates.Query(true).Include(x => x.Assessments)
            .SingleOrDefaultAsync(x => x.Id == r.CandidateId, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.CertificationCandidateNotFound);
        TenantScope.Ensure(current, candidate.OrganizationId);
        var session = await sessions.GetByIdAsync(candidate.ExamSessionId, false, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.CertificationSessionNotFound);
        await CertificationContextualAccess.EnsureSessionAsync(session, cohorts, offerings, current, manage: true, ct);
        await CertificationActorAccess.EnsureJuryAssignmentIfRequiredAsync(session, juries, current, ct);
        if (session.Status != CertificationExamSessionStatus.InProgress)
            throw new ConflictApplicationException(ErrorKeys.CertificationSessionNotWritable);
        var scheme = await schemes.Query(false).Include(x => x.Steps)
            .SingleOrDefaultAsync(x => x.Id == session.SchemeId, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.CertificationSchemeNotFound);
        if (r.Assessments is null || r.Assessments.Count == 0 || r.Assessments.Count > scheme.Steps.Count)
            throw new ValidationApplicationException(ErrorKeys.CertificationAssessmentsRequired);
        var items = new List<CertificationAssessmentInput>(r.Assessments.Count);
        foreach (var assessment in r.Assessments)
        {
            if (assessment is null)
                throw new ValidationApplicationException(ErrorKeys.CertificationAssessmentsRequired);
            if (!Enum.TryParse<CertificationAssessmentOutcome>(assessment.Outcome, true, out var outcome)
                || !Enum.IsDefined(outcome))
                throw new ValidationApplicationException(ErrorKeys.CertificationAssessmentOutcomeInvalid);
            items.Add(new CertificationAssessmentInput(new CertificationStepDefinitionId(assessment.StepDefinitionId),
                assessment.JuryDisplayName, outcome, assessment.Score, assessment.Comment));
        }
        candidate.RecordAssessmentBatch(items, scheme.Steps.Select(step => step.Id).ToArray(), r.Finalize);
        // ITransactionalRequest -> UnitOfWorkBehavior commits the aggregate and its events atomically.
        return CertMap.Candidate(candidate);
    }
}

public sealed class RecordCertificationDecisionCommandHandler(ICertificationCandidateRepository candidates, ICertificationExamSessionRepository sessions, ICertificationSchemeRepository schemes, ICohortRepository cohorts, IProgramOfferingRepository offerings, ICurrentUser current) : IRequestHandler<RecordCertificationDecisionCommand, CertificationCandidateDto>
{
    public async Task<CertificationCandidateDto> Handle(RecordCertificationDecisionCommand r, CancellationToken ct)
    {
        var x = await candidates.Query(true).Include(a => a.Assessments).SingleOrDefaultAsync(a => a.Id == r.CandidateId, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CertificationCandidateNotFound);
        TenantScope.Ensure(current, x.OrganizationId);
        var examSession = await sessions.GetByIdAsync(x.ExamSessionId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CertificationSessionNotFound);
        await CertificationContextualAccess.EnsureSessionAsync(examSession, cohorts, offerings, current, manage: true, ct);
        // A jury decision must not be recorded before the session has started.
        if (examSession.Status != CertificationExamSessionStatus.InProgress)
            throw new ConflictApplicationException(ErrorKeys.CertificationSessionNotWritable);
        if (!Enum.TryParse<CertificationDecision>(r.Decision, true, out var d) || !Enum.IsDefined(d))
            throw new ValidationApplicationException(ErrorKeys.CertificationDecisionInvalid);
        // Decisions must be based on the steps of the session's *own* published scheme.
        // An absent candidate can be recorded without sitting the assessments.
        var scheme = await schemes.Query(false).Include(a => a.Steps)
            .SingleOrDefaultAsync(a => a.Id == examSession.SchemeId, ct)
            ?? throw new NotFoundApplicationException(ErrorKeys.CertificationSchemeNotFound);
        x.Decide(d, r.Comment, scheme.Steps.Select(step => step.Id).ToArray());
        return CertMap.Candidate(x);
    }
}

public sealed class PublishCertificationResultsCommandHandler(ICertificationExamSessionRepository sessions, ICohortRepository cohorts, IProgramOfferingRepository offerings, ICertificationCandidateRepository candidates, ICurrentUser current) : IRequestHandler<PublishCertificationResultsCommand, CertificationExamSessionDto>
{
    public async Task<CertificationExamSessionDto> Handle(PublishCertificationResultsCommand r, CancellationToken ct)
    {
        var s = await sessions.GetByIdAsync(r.SessionId, true, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CertificationSessionNotFound);
        TenantScope.Ensure(current, s.OrganizationId);
        await CertificationContextualAccess.EnsureSessionAsync(s, cohorts, offerings, current, manage: true, ct);
        var rows = await candidates.Query(true).Where(x => x.ExamSessionId == s.Id).ToListAsync(ct);
        if (rows.Count == 0 || rows.Any(x => x.Status != CertificationCandidateStatus.Completed))
            throw new ConflictApplicationException(ErrorKeys.CertificationCandidatesNotCompleted);
        foreach (var c in rows)
            c.Publish();
        if (s.Status == CertificationExamSessionStatus.InProgress)
            s.Complete();
        s.Publish();
        return CertMap.Session(s);
    }
}

public sealed class AssignJuryCommandHandler(ICertificationExamSessionRepository sessions, ICohortRepository cohorts, IProgramOfferingRepository offerings, IJuryAssignmentRepository juries, IOrganizationMemberRepository members, ICurrentUser current) : IRequestHandler<AssignJuryCommand, JuryAssignmentDto>
{
    public async Task<JuryAssignmentDto> Handle(AssignJuryCommand r, CancellationToken ct)
    {
        var s = await sessions.GetByIdAsync(r.SessionId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CertificationSessionNotFound);
        TenantScope.Ensure(current, s.OrganizationId);
        await CertificationContextualAccess.EnsureSessionAsync(s, cohorts, offerings, current, manage: true, ct);
        if (s.Status is CertificationExamSessionStatus.Cancelled or CertificationExamSessionStatus.Completed or CertificationExamSessionStatus.Published)
            throw new ConflictApplicationException(ErrorKeys.CertificationSessionNotWritable);
        // The API must not trust a user id or display name supplied by the browser.
        // Resolve the identity within the session tenant before assigning an examiner.
        var member = await members.GetByAuthGateUserAsync(s.OrganizationId, r.AuthGateUserId, false, ct);
        if (member is null || !member.IsActive)
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);

        var juryRoles = await members.GetRoleCodesAsync(s.OrganizationId, r.AuthGateUserId, ct);
        if (!juryRoles.Any(role => string.Equals(role, "jury", StringComparison.OrdinalIgnoreCase)))
            throw new ForbiddenApplicationException(ErrorKeys.Forbidden);

        var alreadyAssigned = await juries.Query(false).AnyAsync(x =>
            x.OrganizationId == s.OrganizationId && x.ExamSessionId == s.Id
            && x.AuthGateUserId == member.AuthGateUserId, ct);
        if (alreadyAssigned)
            throw new ConflictApplicationException("CERTIFICATION_JURY_ALREADY_ASSIGNED");

        var displayName = member.DisplayName;
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ConflictApplicationException("CERTIFICATION_JURY_DISPLAY_NAME_REQUIRED");
        var j = JuryAssignment.Create(s.OrganizationId, s.Id, member.AuthGateUserId, displayName, r.Role);
        await juries.AddAsync(j, ct);
        return CertMap.Jury(j);
    }
}
