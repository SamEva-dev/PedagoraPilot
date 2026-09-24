using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;
public readonly record struct CertificationUnitId(Guid Value) : IIdentifier
{
    public static CertificationUnitId New() => new(Guid.NewGuid());
    public static CertificationUnitId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
}
