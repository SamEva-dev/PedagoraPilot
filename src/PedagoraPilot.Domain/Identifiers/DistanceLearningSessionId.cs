using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;
public readonly record struct DistanceLearningSessionId(Guid Value) : IIdentifier
{
    public static DistanceLearningSessionId New() => new(Guid.NewGuid());
    public static DistanceLearningSessionId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
}
