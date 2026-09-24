using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;
public readonly record struct DocumentVersionId(Guid Value) : IIdentifier
{
    public static DocumentVersionId New() => new(Guid.NewGuid());
    public static DocumentVersionId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
}
