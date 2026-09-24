using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;
public readonly record struct WorkplaceDocumentRequirementId(Guid Value) : IIdentifier
{
    public static WorkplaceDocumentRequirementId New() => new(Guid.NewGuid());
    public static WorkplaceDocumentRequirementId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
}
