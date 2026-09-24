using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Learning.Events;

namespace PedagoraPilot.Domain.Learning;
public sealed class LearnerCompetencyRecord : AggregateRoot<LearnerCompetencyRecordId>
{
    private LearnerCompetencyRecord()
    {
    }

    private LearnerCompetencyRecord(LearnerCompetencyRecordId id, Guid organizationId, EnrollmentId enrollmentId, CompetencyDefinitionId competencyDefinitionId) : base(id)
    {
        if (organizationId == Guid.Empty)
            throw new DomainException("COMPETENCY_ORGANIZATION_REQUIRED");
        if (enrollmentId.IsEmpty)
            throw new DomainException("COMPETENCY_ENROLLMENT_REQUIRED");
        if (competencyDefinitionId.IsEmpty)
            throw new DomainException("COMPETENCY_DEFINITION_REQUIRED");
        OrganizationId = organizationId;
        EnrollmentId = enrollmentId;
        CompetencyDefinitionId = competencyDefinitionId;
        Level = CompetencyLevel.NotAssessed;
        CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
        Version = 1;
    }

    public Guid OrganizationId { get; private set; }
    public EnrollmentId EnrollmentId { get; private set; }
    public CompetencyDefinitionId CompetencyDefinitionId { get; private set; }
    public CompetencyLevel Level { get; private set; }
    public decimal? Score { get; private set; }
    public string? Comment { get; private set; }
    public Guid? EvaluatorAuthGateUserId { get; private set; }
    public string? EvaluatorDisplayName { get; private set; }
    public DateTimeOffset? EvaluatedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static LearnerCompetencyRecord Create(Guid organizationId, EnrollmentId enrollmentId, CompetencyDefinitionId competencyDefinitionId) => new(LearnerCompetencyRecordId.New(), organizationId, enrollmentId, competencyDefinitionId);
    public void Evaluate(CompetencyLevel level, decimal? score, string? comment, Guid? evaluatorAuthGateUserId, string? evaluatorDisplayName, DateTimeOffset evaluatedAtUtc)
    {
        if (score is < 0 or > 100)
            throw new DomainException("COMPETENCY_SCORE_INVALID");
        Level = level;
        Score = score;
        Comment = NormalizeOptional(comment, 2000);
        EvaluatorAuthGateUserId = evaluatorAuthGateUserId;
        EvaluatorDisplayName = NormalizeOptional(evaluatorDisplayName, 200);
        EvaluatedAtUtc = evaluatedAtUtc;
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new LearnerCompetencyEvaluatedDomainEvent(Id, EnrollmentId, CompetencyDefinitionId, OrganizationId));
    }

    private static string? NormalizeOptional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var v = value.Trim();
        return v.Length <= max ? v : v[..max];
    }
}
