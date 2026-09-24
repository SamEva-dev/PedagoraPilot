using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;
public readonly record struct CertificationCandidateId(Guid Value) : IIdentifier
{
    public static CertificationCandidateId New() => new(Guid.NewGuid());
    public static CertificationCandidateId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
}
