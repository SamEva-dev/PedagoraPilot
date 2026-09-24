using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;
public readonly record struct WorkplaceActivityId(Guid Value) : IIdentifier
{
    public static WorkplaceActivityId New() => new(Guid.NewGuid());
    public static WorkplaceActivityId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
}
