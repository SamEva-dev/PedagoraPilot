using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;

public readonly record struct LearnerTopicEvaluationCriterionId(Guid Value) : IIdentifier
{
    public static LearnerTopicEvaluationCriterionId New() => new(Guid.NewGuid());
    public static LearnerTopicEvaluationCriterionId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
    public static implicit operator Guid(LearnerTopicEvaluationCriterionId id) => id.Value;
    public static explicit operator LearnerTopicEvaluationCriterionId(Guid value) => new(value);
}
