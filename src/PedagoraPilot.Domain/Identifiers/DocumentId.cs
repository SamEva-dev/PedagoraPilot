using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;
public readonly record struct DocumentId(Guid Value) : IIdentifier
{
    public static DocumentId New() => new(Guid.NewGuid());
    public static DocumentId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
}
