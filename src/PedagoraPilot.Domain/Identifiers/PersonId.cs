using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;
public readonly record struct PersonId(Guid Value) : IIdentifier
{
    public static PersonId New() => new(Guid.NewGuid());
    public static PersonId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
}
