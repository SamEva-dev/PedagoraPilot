using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Domain.Learning;
public sealed class DrivingEvaluationCriterion : Entity<DrivingEvaluationCriterionId>
{
    private DrivingEvaluationCriterion()
    {
    }

    internal DrivingEvaluationCriterion(DrivingEvaluationCriterionId id, DrivingEvaluationId drivingEvaluationId, string code, string label, CompetencyLevel level) : base(id)
    {
        DrivingEvaluationId = drivingEvaluationId;
        Code = string.IsNullOrWhiteSpace(code) ? throw new DomainException("DRIVING_CRITERION_CODE_REQUIRED") : code.Trim();
        Label = string.IsNullOrWhiteSpace(label) ? throw new DomainException("DRIVING_CRITERION_LABEL_REQUIRED") : label.Trim();
        Level = level;
    }

    public DrivingEvaluationId DrivingEvaluationId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Label { get; private set; } = string.Empty;
    public CompetencyLevel Level { get; private set; }
}
