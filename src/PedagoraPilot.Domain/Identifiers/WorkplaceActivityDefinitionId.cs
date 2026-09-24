using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;
public readonly record struct WorkplaceActivityDefinitionId(Guid Value) : IIdentifier
{
    public static WorkplaceActivityDefinitionId New() => new(Guid.NewGuid());
    public static WorkplaceActivityDefinitionId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
}
