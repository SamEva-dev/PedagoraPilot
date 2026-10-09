using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Certification.Events;

namespace PedagoraPilot.Domain.Certification;
public sealed class CertificationCandidate : AggregateRoot<CertificationCandidateId>
{
    private readonly List<CertificationAssessment> _assessments = [];
    private CertificationCandidate()
    {
    }

    private CertificationCandidate(CertificationCandidateId id, Guid organizationId, CertificationExamSessionId examSessionId, EnrollmentId enrollmentId) : base(id)
    {
        if (organizationId == Guid.Empty)
            throw new DomainException("CERTIFICATION_ORGANIZATION_REQUIRED");
        if (examSessionId.IsEmpty)
            throw new DomainException("CERTIFICATION_SESSION_REQUIRED");
        if (enrollmentId.IsEmpty)
            throw new DomainException("CERTIFICATION_ENROLLMENT_REQUIRED");
        OrganizationId = organizationId;
        ExamSessionId = examSessionId;
        EnrollmentId = enrollmentId;
        Status = CertificationCandidateStatus.Registered;
        Decision = CertificationDecision.Pending;
        CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
        Version = 1;
    }

    public Guid OrganizationId { get; private set; }
    public CertificationExamSessionId ExamSessionId { get; private set; }
    public EnrollmentId EnrollmentId { get; private set; }
    public CertificationCandidateStatus Status { get; private set; }
    public bool? Eligible { get; private set; }
    public string? EligibilitySnapshotJson { get; private set; }
    public CertificationDecision Decision { get; private set; }
    public string? DecisionComment { get; private set; }
    public DateTime? DecisionAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }
    public IReadOnlyCollection<CertificationAssessment> Assessments => _assessments.AsReadOnly();

    public static CertificationCandidate Register(Guid organizationId, CertificationExamSessionId examSessionId, EnrollmentId enrollmentId)
    {
        var x = new CertificationCandidate(CertificationCandidateId.New(), organizationId, examSessionId, enrollmentId);
        x.RaiseDomainEvent(new CertificationCandidateRegisteredDomainEvent(x.Id, organizationId, examSessionId, enrollmentId));
        return x;
    }

    public void SetEligibility(bool eligible, string snapshotJson)
    {
        if (Status is CertificationCandidateStatus.InProgress or CertificationCandidateStatus.AssessmentsValidated or CertificationCandidateStatus.Completed or CertificationCandidateStatus.Published)
            throw new DomainException("CERTIFICATION_CANDIDATE_LOCKED");
        Eligible = eligible;
        EligibilitySnapshotJson = string.IsNullOrWhiteSpace(snapshotJson) ? "{}" : snapshotJson;
        Status = eligible ? CertificationCandidateStatus.Eligible : CertificationCandidateStatus.Ineligible;
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new CertificationEligibilityEvaluatedDomainEvent(Id, OrganizationId, eligible));
    }

    public CertificationAssessment RecordAssessment(CertificationStepDefinitionId stepId, string juryDisplayName, CertificationAssessmentOutcome outcome, decimal? score, string? comment)
    {
        if (Eligible != true)
            throw new DomainException("CERTIFICATION_CANDIDATE_NOT_ELIGIBLE");
        // A jury decision freezes the assessment record; correction is a separate audited workflow.
        if (Status is CertificationCandidateStatus.AssessmentsValidated or CertificationCandidateStatus.Completed or CertificationCandidateStatus.Published)
            throw new DomainException("CERTIFICATION_CANDIDATE_LOCKED");
        var existing = _assessments.SingleOrDefault(x => x.StepDefinitionId == stepId);
        if (existing is not null)
        {
            existing.Update(juryDisplayName, outcome, score, comment);
            Status = CertificationCandidateStatus.InProgress;
            UpdatedAtUtc = DateTime.UtcNow;
            RaiseDomainEvent(new CertificationAssessmentRecordedDomainEvent(Id, OrganizationId, existing.Id, stepId));
            return existing;
        }
        var a = new CertificationAssessment(CertificationAssessmentId.New(), Id, stepId, juryDisplayName, outcome, score, comment);
        _assessments.Add(a);
        Status = CertificationCandidateStatus.InProgress;
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new CertificationAssessmentRecordedDomainEvent(Id, OrganizationId, a.Id, stepId));
        return a;
    }

    /// <summary>
    /// Writes the entire jury grid to one aggregate. Transactional persistence is provided
    /// by UnitOfWorkBehavior for the calling command. Validation precedes mutation.
    /// Pending is a draft result and cannot satisfy final validation.
    /// </summary>
    public void RecordAssessmentBatch(
        IReadOnlyCollection<CertificationAssessmentInput> entries,
        IReadOnlyCollection<CertificationStepDefinitionId> schemeStepIds,
        bool finalize)
    {
        if (Eligible != true)
            throw new DomainException("CERTIFICATION_CANDIDATE_NOT_ELIGIBLE");
        if (Status is CertificationCandidateStatus.AssessmentsValidated or CertificationCandidateStatus.Completed or CertificationCandidateStatus.Published)
            throw new DomainException("CERTIFICATION_CANDIDATE_LOCKED");
        if (entries is null || entries.Count == 0)
            throw new DomainException("CERTIFICATION_ASSESSMENTS_REQUIRED");
        if (schemeStepIds is null || schemeStepIds.Count == 0)
            throw new DomainException("CERTIFICATION_SCHEME_INCOMPLETE");
        var allowed = schemeStepIds.ToHashSet();
        var seen = new HashSet<CertificationStepDefinitionId>();
        foreach (var entry in entries)
        {
            if (entry.StepDefinitionId.IsEmpty || !allowed.Contains(entry.StepDefinitionId))
                throw new DomainException("CERTIFICATION_STEP_NOT_FOUND");
            if (!seen.Add(entry.StepDefinitionId))
                throw new DomainException("CERTIFICATION_ASSESSMENT_DUPLICATE_STEP");
            if (!Enum.IsDefined(entry.Outcome))
                throw new DomainException("CERTIFICATION_ASSESSMENT_OUTCOME_INVALID");
            if (string.IsNullOrWhiteSpace(entry.JuryDisplayName))
                throw new DomainException("CERTIFICATION_JURY_REQUIRED");
            if (entry.Score.HasValue && (entry.Score.Value < 0 || entry.Score.Value > 100))
                throw new DomainException("CERTIFICATION_SCORE_INVALID");
            if (entry.Comment is { Length: > 4000 })
                throw new DomainException("CERTIFICATION_COMMENT_TOO_LONG");
        }
        if (finalize && schemeStepIds.Any(step =>
            entries.FirstOrDefault(a => a.StepDefinitionId == step) is { } input
                ? input.Outcome == CertificationAssessmentOutcome.Pending
                : !_assessments.Any(a => a.StepDefinitionId == step && a.Outcome != CertificationAssessmentOutcome.Pending)))
            throw new DomainException("CERTIFICATION_ASSESSMENTS_INCOMPLETE");

        foreach (var entry in entries)
            RecordAssessment(entry.StepDefinitionId, entry.JuryDisplayName, entry.Outcome, entry.Score, entry.Comment);

        if (finalize)
        {
            Status = CertificationCandidateStatus.AssessmentsValidated;
            UpdatedAtUtc = DateTime.UtcNow;
        }
    }

    public void Decide(CertificationDecision decision, string? comment, IReadOnlyCollection<CertificationStepDefinitionId> requiredStepIds)
    {
        if (!Enum.IsDefined(decision) || decision == CertificationDecision.Pending)
            throw new DomainException("CERTIFICATION_DECISION_INVALID");
        if (Status is CertificationCandidateStatus.Completed or CertificationCandidateStatus.Published)
            throw new DomainException("CERTIFICATION_CANDIDATE_LOCKED");
        if (Eligible != true)
            throw new DomainException("CERTIFICATION_CANDIDATE_NOT_ELIGIBLE");
        if (requiredStepIds is null || requiredStepIds.Count == 0)
            throw new DomainException("CERTIFICATION_SCHEME_INCOMPLETE");
        if (decision != CertificationDecision.Absent && requiredStepIds.Any(id =>
                !_assessments.Any(a => a.StepDefinitionId == id && a.Outcome != CertificationAssessmentOutcome.Pending)))
            throw new DomainException("CERTIFICATION_ASSESSMENTS_INCOMPLETE");
        Decision = decision;
        DecisionComment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        DecisionAtUtc = DateTime.UtcNow;
        Status = CertificationCandidateStatus.Completed;
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new CertificationDecisionRecordedDomainEvent(Id, OrganizationId, decision));
    }

    public void Publish()
    {
        if (Status != CertificationCandidateStatus.Completed)
            throw new DomainException("CERTIFICATION_CANDIDATE_NOT_COMPLETED");
        Status = CertificationCandidateStatus.Published;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public sealed class CertificationAssessment
{
    private CertificationAssessment()
    {
    }

    internal CertificationAssessment(CertificationAssessmentId id, CertificationCandidateId candidateId, CertificationStepDefinitionId stepDefinitionId, string juryDisplayName, CertificationAssessmentOutcome outcome, decimal? score, string? comment)
    {
        if (string.IsNullOrWhiteSpace(juryDisplayName))
            throw new DomainException("CERTIFICATION_JURY_REQUIRED");
        if (score.HasValue && (score < 0 || score > 100))
            throw new DomainException("CERTIFICATION_SCORE_INVALID");
        Id = id;
        CandidateId = candidateId;
        StepDefinitionId = stepDefinitionId;
        JuryDisplayName = juryDisplayName.Trim();
        Outcome = outcome;
        Score = score;
        Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        RecordedAtUtc = DateTimeOffset.UtcNow;
    }

    public CertificationAssessmentId Id { get; private set; }
    public CertificationCandidateId CandidateId { get; private set; }
    public CertificationStepDefinitionId StepDefinitionId { get; private set; }
    public string JuryDisplayName { get; private set; } = string.Empty;
    public CertificationAssessmentOutcome Outcome { get; private set; }
    public decimal? Score { get; private set; }
    public string? Comment { get; private set; }
    public DateTimeOffset RecordedAtUtc { get; private set; }

    internal void Update(string juryDisplayName, CertificationAssessmentOutcome outcome, decimal? score, string? comment)
    {
        if (string.IsNullOrWhiteSpace(juryDisplayName))
            throw new DomainException("CERTIFICATION_JURY_REQUIRED");
        if (score.HasValue && (score < 0 || score > 100))
            throw new DomainException("CERTIFICATION_SCORE_INVALID");
        JuryDisplayName = juryDisplayName.Trim();
        Outcome = outcome;
        Score = score;
        Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        RecordedAtUtc = DateTimeOffset.UtcNow;
    }
}
