using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Learning.Events;

namespace PedagoraPilot.Domain.Learning;
public sealed class DrivingEvaluation : AggregateRoot<DrivingEvaluationId>
{
    private readonly List<DrivingEvaluationCriterion> _criteria = [];
    private DrivingEvaluation()
    {
    }

    private DrivingEvaluation(DrivingEvaluationId id, Guid organizationId, EnrollmentId enrollmentId, CompetencyDefinitionId competencyDefinitionId, TrainingSessionId? trainingSessionId, DateTimeOffset evaluatedAtUtc, string? trainerAuthGateUserId, string trainerDisplayName, string subject, string? positive, string? difficulty, string? nextGoal, string? freeObservation) : base(id)
    {
        if (organizationId == Guid.Empty)
            throw new DomainException("DRIVING_ORGANIZATION_REQUIRED");
        if (enrollmentId.IsEmpty)
            throw new DomainException("DRIVING_ENROLLMENT_REQUIRED");
        if (competencyDefinitionId.IsEmpty)
            throw new DomainException("DRIVING_COMPETENCY_REQUIRED");
        OrganizationId = organizationId;
        EnrollmentId = enrollmentId;
        CompetencyDefinitionId = competencyDefinitionId;
        TrainingSessionId = trainingSessionId;
        EvaluatedAtUtc = evaluatedAtUtc;
        TrainerAuthGateUserId = Opt(trainerAuthGateUserId, 120);
        TrainerDisplayName = Req(trainerDisplayName, "DRIVING_TRAINER_REQUIRED", 200);
        Subject = Req(subject, "DRIVING_SUBJECT_REQUIRED", 500);
        Positive = Opt(positive, 2000);
        Difficulty = Opt(difficulty, 2000);
        NextGoal = Opt(nextGoal, 2000);
        FreeObservation = Opt(freeObservation, 4000);
        CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
        Version = 1;
    }

    public Guid OrganizationId { get; private set; }
    public EnrollmentId EnrollmentId { get; private set; }
    public CompetencyDefinitionId CompetencyDefinitionId { get; private set; }
    public TrainingSessionId? TrainingSessionId { get; private set; }
    public DateTimeOffset EvaluatedAtUtc { get; private set; }
    public string? TrainerAuthGateUserId { get; private set; }
    public string TrainerDisplayName { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string? Positive { get; private set; }
    public string? Difficulty { get; private set; }
    public string? NextGoal { get; private set; }
    public string? FreeObservation { get; private set; }
    public IReadOnlyCollection<DrivingEvaluationCriterion> Criteria => _criteria.AsReadOnly();
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static DrivingEvaluation Create(Guid organizationId, EnrollmentId enrollmentId, CompetencyDefinitionId competencyDefinitionId, TrainingSessionId? trainingSessionId, DateTimeOffset evaluatedAtUtc, string? trainerAuthGateUserId, string trainerDisplayName, string subject, string? positive, string? difficulty, string? nextGoal, string? freeObservation, IEnumerable<(string Code, string Label, CompetencyLevel Level)> criteria)
    {
        var e = new DrivingEvaluation(DrivingEvaluationId.New(), organizationId, enrollmentId, competencyDefinitionId, trainingSessionId, evaluatedAtUtc, trainerAuthGateUserId, trainerDisplayName, subject, positive, difficulty, nextGoal, freeObservation);
        foreach (var c in criteria)
            e._criteria.Add(new DrivingEvaluationCriterion(DrivingEvaluationCriterionId.New(), e.Id, c.Code, c.Label, c.Level));
        if (e._criteria.Count == 0)
            throw new DomainException("DRIVING_CRITERIA_REQUIRED");
        e.RaiseDomainEvent(new DrivingEvaluationRecordedDomainEvent(e.Id, e.EnrollmentId, e.OrganizationId));
        return e;
    }

    private static string Req(string v, string k, int m)
    {
        if (string.IsNullOrWhiteSpace(v))
            throw new DomainException(k);
        var s = v.Trim();
        if (s.Length > m)
            throw new DomainException(k);
        return s;
    }

    private static string? Opt(string? v, int m)
    {
        if (string.IsNullOrWhiteSpace(v))
            return null;
        var s = v.Trim();
        return s.Length <= m ? s : s[..m];
    }
}
