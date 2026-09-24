using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;
public readonly record struct DrivingEvaluationId(Guid Value) : IIdentifier
{
    public static DrivingEvaluationId New() => new(Guid.NewGuid());
    public static DrivingEvaluationId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
    public static implicit operator Guid(DrivingEvaluationId id) => id.Value;
    public static explicit operator DrivingEvaluationId(Guid value) => new(value);
}
