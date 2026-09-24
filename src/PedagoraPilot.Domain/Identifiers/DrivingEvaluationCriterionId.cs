using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;
public readonly record struct DrivingEvaluationCriterionId(Guid Value) : IIdentifier
{
    public static DrivingEvaluationCriterionId New() => new(Guid.NewGuid());
    public static DrivingEvaluationCriterionId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
    public static implicit operator Guid(DrivingEvaluationCriterionId id) => id.Value;
    public static explicit operator DrivingEvaluationCriterionId(Guid value) => new(value);
}
