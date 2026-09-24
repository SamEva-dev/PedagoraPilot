using System.Text.Json;
using DomainRelay.Abstractions;
using DomainRelay.Mapping.Abstractions;
using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Contracts.Certification;
using PedagoraPilot.Domain.Certification;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Application.Certification;
internal static class CertMap
{
    public static CertificationSchemeDto Scheme(CertificationScheme x) => new(x.Id.Value, x.ReferentialVersionId, x.Code, x.Name, x.Status.ToString(), x.EffectiveFrom, x.EffectiveTo, x.Units.OrderBy(u => u.SortOrder).Select(u => new CertificationUnitDto(u.Id.Value, u.Code, u.Title, u.SortOrder)).ToArray(), x.Steps.OrderBy(s => s.SortOrder).Select(s => new CertificationStepDto(s.Id.Value, s.UnitId?.Value, s.Code, s.Title, s.Kind.ToString(), s.DurationMinutes, s.SortOrder)).ToArray());
    public static CertificationExamSessionDto Session(CertificationExamSession x) => new(x.Id.Value, x.OrganizationId, x.SiteId, x.CohortId.Value, x.SchemeId.Value, x.Title, x.StartsAtUtc, x.EndsAtUtc, x.Venue, x.Status.ToString());
    public static CertificationCandidateDto Candidate(CertificationCandidate x) => new(x.Id.Value, x.ExamSessionId.Value, x.EnrollmentId.Value, x.Status.ToString(), x.Eligible, x.Decision.ToString(), x.DecisionComment, x.DecisionAtUtc, x.Assessments.OrderBy(a => a.RecordedAtUtc).Select(a => new CertificationAssessmentDto(a.Id.Value, a.StepDefinitionId.Value, a.JuryDisplayName, a.Outcome.ToString(), a.Score, a.Comment, a.RecordedAtUtc)).ToArray());
    public static JuryAssignmentDto Jury(JuryAssignment x) => new(x.Id.Value, x.ExamSessionId.Value, x.AuthGateUserId, x.DisplayName, x.Role, x.AssignedAtUtc);
}

public sealed class GetCertificationSchemesQueryHandler(ICertificationSchemeRepository repo) : IRequestHandler<GetCertificationSchemesQuery, IReadOnlyCollection<CertificationSchemeDto>>
{
    public async Task<IReadOnlyCollection<CertificationSchemeDto>> Handle(GetCertificationSchemesQuery r, CancellationToken ct)
    {
        var q = repo.Query(false).Include(x => x.Units).Include(x => x.Steps).AsQueryable();
        if (r.ReferentialVersionId.HasValue)
            q = q.Where(x => x.ReferentialVersionId == r.ReferentialVersionId);
        var rows = await q.OrderBy(x => x.Name).ToListAsync(ct);
        return rows.Select(CertMap.Scheme).ToArray();
    }
}

public sealed class GetCertificationExamSessionsQueryHandler(ICertificationExamSessionRepository repo, ICurrentUser current) : IRequestHandler<GetCertificationExamSessionsQuery, IReadOnlyCollection<CertificationExamSessionDto>>
{
    public async Task<IReadOnlyCollection<CertificationExamSessionDto>> Handle(GetCertificationExamSessionsQuery r, CancellationToken ct)
    {
        var q = repo.Query(false);
        if (current.OrganizationId.HasValue)
            q = q.Where(x => x.OrganizationId == current.OrganizationId.Value);
        if (r.CohortId.HasValue)
            q = q.Where(x => x.CohortId == r.CohortId.Value);
        return (await q.OrderByDescending(x => x.StartsAtUtc).ToListAsync(ct)).Select(CertMap.Session).ToArray();
    }
}

public sealed class GetCertificationCandidatesQueryHandler(ICertificationCandidateRepository repo, ICurrentUser current) : IRequestHandler<GetCertificationCandidatesQuery, IReadOnlyCollection<CertificationCandidateDto>>
{
    public async Task<IReadOnlyCollection<CertificationCandidateDto>> Handle(GetCertificationCandidatesQuery r, CancellationToken ct)
    {
        var q = repo.Query(false).Include(x => x.Assessments).Where(x => x.ExamSessionId == r.SessionId).AsQueryable();
        if (current.OrganizationId.HasValue)
            q = q.Where(x => x.OrganizationId == current.OrganizationId.Value);
        return (await q.ToListAsync(ct)).Select(CertMap.Candidate).ToArray();
    }
}

public sealed class CreateCertificationExamSessionCommandHandler(ICohortRepository cohorts, ICertificationSchemeRepository schemes, ICertificationExamSessionRepository sessions, ICurrentUser current) : IRequestHandler<CreateCertificationExamSessionCommand, CertificationExamSessionDto>
{
    public async Task<CertificationExamSessionDto> Handle(CreateCertificationExamSessionCommand r, CancellationToken ct)
    {
        var c = await cohorts.GetByIdAsync(r.CohortId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CohortNotFound);
        var s = await schemes.GetByIdAsync(r.SchemeId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CertificationSchemeNotFound);
        if (s.Status != CertificationSchemeStatus.Published)
            throw new ConflictApplicationException(ErrorKeys.CertificationSchemeNotPublished);
        if (s.ReferentialVersionId != c.ReferentialVersionId)
            throw new ConflictApplicationException(ErrorKeys.CertificationSchemeReferentialMismatch);
        if (current.OrganizationId.HasValue && c.OrganizationId != current.OrganizationId.Value)
            throw new ForbiddenApplicationException(ErrorKeys.CertificationForbidden);
        var x = CertificationExamSession.Create(c.OrganizationId, c.SiteId, c.Id, s.Id, r.Title, r.StartsAtUtc, r.EndsAtUtc, r.Venue);
        await sessions.AddAsync(x, ct);
        return CertMap.Session(x);
    }
}

public sealed class PlanCertificationExamSessionCommandHandler(ICertificationExamSessionRepository sessions) : IRequestHandler<PlanCertificationExamSessionCommand, CertificationExamSessionDto>
{
    public async Task<CertificationExamSessionDto> Handle(PlanCertificationExamSessionCommand r, CancellationToken ct)
    {
        var x = await sessions.GetByIdAsync(r.SessionId, true, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CertificationSessionNotFound);
        x.Plan();
        return CertMap.Session(x);
    }
}

public sealed class RegisterCohortCandidatesCommandHandler(ICertificationExamSessionRepository sessions, IEnrollmentRepository enrollments, ICertificationCandidateRepository candidates) : IRequestHandler<RegisterCohortCandidatesCommand, int>
{
    public async Task<int> Handle(RegisterCohortCandidatesCommand r, CancellationToken ct)
    {
        var s = await sessions.GetByIdAsync(r.SessionId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CertificationSessionNotFound);
        var rows = await enrollments.Query(false).Where(x => x.CohortId == s.CohortId).ToListAsync(ct);
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

public sealed class EvaluateCertificationEligibilityCommandHandler(ICertificationCandidateRepository candidates) : IRequestHandler<EvaluateCertificationEligibilityCommand, CertificationCandidateDto>
{
    public async Task<CertificationCandidateDto> Handle(EvaluateCertificationEligibilityCommand r, CancellationToken ct)
    {
        var x = await candidates.Query(true).Include(a => a.Assessments).SingleOrDefaultAsync(a => a.Id == r.CandidateId, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CertificationCandidateNotFound);
        x.SetEligibility(r.Eligible, JsonSerializer.Serialize(new { eligible = r.Eligible, blockers = r.Blockers ?? [] }));
        return CertMap.Candidate(x);
    }
}

public sealed class RecordCertificationAssessmentCommandHandler(ICertificationCandidateRepository candidates, ICertificationSchemeRepository schemes, ICertificationExamSessionRepository sessions) : IRequestHandler<RecordCertificationAssessmentCommand, CertificationCandidateDto>
{
    public async Task<CertificationCandidateDto> Handle(RecordCertificationAssessmentCommand r, CancellationToken ct)
    {
        var x = await candidates.Query(true).Include(a => a.Assessments).SingleOrDefaultAsync(a => a.Id == r.CandidateId, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CertificationCandidateNotFound);
        var session = await sessions.GetByIdAsync(x.ExamSessionId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CertificationSessionNotFound);
        var scheme = await schemes.Query(false).Include(a => a.Steps).SingleOrDefaultAsync(a => a.Id == session.SchemeId, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CertificationSchemeNotFound);
        if (scheme.Steps.All(a => a.Id != r.StepDefinitionId))
            throw new ValidationApplicationException(ErrorKeys.CertificationStepNotFound);
        if (!Enum.TryParse<CertificationAssessmentOutcome>(r.Outcome, true, out var outcome))
            throw new ValidationApplicationException(ErrorKeys.CertificationAssessmentOutcomeInvalid);
        x.RecordAssessment(r.StepDefinitionId, r.JuryDisplayName, outcome, r.Score, r.Comment);
        return CertMap.Candidate(x);
    }
}

public sealed class RecordCertificationDecisionCommandHandler(ICertificationCandidateRepository candidates) : IRequestHandler<RecordCertificationDecisionCommand, CertificationCandidateDto>
{
    public async Task<CertificationCandidateDto> Handle(RecordCertificationDecisionCommand r, CancellationToken ct)
    {
        var x = await candidates.Query(true).Include(a => a.Assessments).SingleOrDefaultAsync(a => a.Id == r.CandidateId, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CertificationCandidateNotFound);
        if (!Enum.TryParse<CertificationDecision>(r.Decision, true, out var d))
            throw new ValidationApplicationException(ErrorKeys.CertificationDecisionInvalid);
        x.Decide(d, r.Comment);
        return CertMap.Candidate(x);
    }
}

public sealed class PublishCertificationResultsCommandHandler(ICertificationExamSessionRepository sessions, ICertificationCandidateRepository candidates) : IRequestHandler<PublishCertificationResultsCommand, CertificationExamSessionDto>
{
    public async Task<CertificationExamSessionDto> Handle(PublishCertificationResultsCommand r, CancellationToken ct)
    {
        var s = await sessions.GetByIdAsync(r.SessionId, true, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CertificationSessionNotFound);
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

public sealed class AssignJuryCommandHandler(ICertificationExamSessionRepository sessions, IJuryAssignmentRepository juries) : IRequestHandler<AssignJuryCommand, JuryAssignmentDto>
{
    public async Task<JuryAssignmentDto> Handle(AssignJuryCommand r, CancellationToken ct)
    {
        var s = await sessions.GetByIdAsync(r.SessionId, false, ct) ?? throw new NotFoundApplicationException(ErrorKeys.CertificationSessionNotFound);
        var j = JuryAssignment.Create(s.OrganizationId, s.Id, r.AuthGateUserId, r.DisplayName, r.Role);
        await juries.AddAsync(j, ct);
        return CertMap.Jury(j);
    }
}
