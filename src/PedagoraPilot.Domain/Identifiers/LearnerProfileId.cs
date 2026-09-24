using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;
public readonly record struct LearnerProfileId(Guid Value) : IIdentifier
{
    public static LearnerProfileId New() => new(Guid.NewGuid());
    public static LearnerProfileId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
}
