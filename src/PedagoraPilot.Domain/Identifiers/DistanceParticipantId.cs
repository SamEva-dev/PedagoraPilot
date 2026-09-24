using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;
public readonly record struct DistanceParticipantId(Guid Value) : IIdentifier
{
    public static DistanceParticipantId New() => new(Guid.NewGuid());
    public static DistanceParticipantId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
}
