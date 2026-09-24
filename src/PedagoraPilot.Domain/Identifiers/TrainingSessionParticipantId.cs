using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;
public readonly record struct TrainingSessionParticipantId(Guid Value) : IIdentifier
{
    public static TrainingSessionParticipantId New() => new(Guid.NewGuid());
    public static TrainingSessionParticipantId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
}
