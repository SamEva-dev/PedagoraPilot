using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;
public readonly record struct WorkplaceEvaluationId(Guid Value) : IIdentifier
{
    public static WorkplaceEvaluationId New() => new(Guid.NewGuid());
    public static WorkplaceEvaluationId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
}
