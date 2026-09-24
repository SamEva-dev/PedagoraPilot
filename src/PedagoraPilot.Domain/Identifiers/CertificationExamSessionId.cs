using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;
public readonly record struct CertificationExamSessionId(Guid Value) : IIdentifier
{
    public static CertificationExamSessionId New() => new(Guid.NewGuid());
    public static CertificationExamSessionId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
}
