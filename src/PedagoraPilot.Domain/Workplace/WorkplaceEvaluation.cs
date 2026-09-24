using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Domain.Workplace;
public sealed class WorkplaceEvaluation : Entity<WorkplaceEvaluationId>
{
    private WorkplaceEvaluation()
    {
    }

    internal WorkplaceEvaluation(WorkplaceEvaluationId id, WorkplacePeriodId periodId, WorkplaceEvaluationKind kind, string evaluatorDisplayName, DateTimeOffset evaluatedAtUtc, string summary, string? strengths, string? improvementAreas, bool? validated) : base(id)
    {
        if (string.IsNullOrWhiteSpace(evaluatorDisplayName))
            throw new DomainException("WORKPLACE_EVALUATOR_REQUIRED");
        if (string.IsNullOrWhiteSpace(summary))
            throw new DomainException("WORKPLACE_EVALUATION_SUMMARY_REQUIRED");
        PeriodId = periodId;
        Kind = kind;
        EvaluatorDisplayName = evaluatorDisplayName.Trim();
        EvaluatedAtUtc = evaluatedAtUtc.ToUniversalTime();
        Summary = summary.Trim();
        Strengths = string.IsNullOrWhiteSpace(strengths) ? null : strengths.Trim();
        ImprovementAreas = string.IsNullOrWhiteSpace(improvementAreas) ? null : improvementAreas.Trim();
        Validated = validated;
    }

    public WorkplacePeriodId PeriodId { get; private set; }
    public WorkplaceEvaluationKind Kind { get; private set; }
    public string EvaluatorDisplayName { get; private set; } = string.Empty;
    public DateTimeOffset EvaluatedAtUtc { get; private set; }
    public string Summary { get; private set; } = string.Empty;
    public string? Strengths { get; private set; }
    public string? ImprovementAreas { get; private set; }
    public bool? Validated { get; private set; }
}
