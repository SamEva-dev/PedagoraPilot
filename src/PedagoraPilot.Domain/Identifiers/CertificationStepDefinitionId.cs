using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;
public readonly record struct CertificationStepDefinitionId(Guid Value) : IIdentifier
{
    public static CertificationStepDefinitionId New() => new(Guid.NewGuid());
    public static CertificationStepDefinitionId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
}
