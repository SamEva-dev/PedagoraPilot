using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;
public readonly record struct CohortId(Guid Value) : IIdentifier
{
    public static CohortId New() => new(Guid.NewGuid());
    public static CohortId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
}
