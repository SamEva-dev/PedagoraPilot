using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;
public readonly record struct PedagogicalTopicId(Guid Value) : IIdentifier
{
    public static PedagogicalTopicId New() => new(Guid.NewGuid());
    public static PedagogicalTopicId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
    public static implicit operator Guid(PedagogicalTopicId id) => id.Value;
    public static explicit operator PedagogicalTopicId(Guid value) => new(value);
}
