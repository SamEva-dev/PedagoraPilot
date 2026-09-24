using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;
public readonly record struct CertificationSchemeId(Guid Value) : IIdentifier
{
    public static CertificationSchemeId New() => new(Guid.NewGuid());
    public static CertificationSchemeId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
}
