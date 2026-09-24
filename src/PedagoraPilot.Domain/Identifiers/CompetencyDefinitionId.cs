using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;
public readonly record struct CompetencyDefinitionId(Guid Value) : IIdentifier
{
    public static CompetencyDefinitionId New() => new(Guid.NewGuid());
    public static CompetencyDefinitionId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
    public static implicit operator Guid(CompetencyDefinitionId id) => id.Value;
    public static explicit operator CompetencyDefinitionId(Guid value) => new(value);
}
