using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;
public readonly record struct EnrollmentId(Guid Value) : IIdentifier
{
    public static EnrollmentId New() => new(Guid.NewGuid());
    public static EnrollmentId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
}
