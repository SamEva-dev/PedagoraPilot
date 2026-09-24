using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;
public readonly record struct LearnerTopicProgressId(Guid Value) : IIdentifier
{
    public static LearnerTopicProgressId New() => new(Guid.NewGuid());
    public static LearnerTopicProgressId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
    public static implicit operator Guid(LearnerTopicProgressId id) => id.Value;
    public static explicit operator LearnerTopicProgressId(Guid value) => new(value);
}
