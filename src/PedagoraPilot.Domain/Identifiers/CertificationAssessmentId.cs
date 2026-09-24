using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;
public readonly record struct CertificationAssessmentId(Guid Value) : IIdentifier
{
    public static CertificationAssessmentId New() => new(Guid.NewGuid());
    public static CertificationAssessmentId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
}
