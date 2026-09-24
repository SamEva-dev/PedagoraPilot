using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;
public readonly record struct RemoteWorkActivityId(Guid Value) : IIdentifier
{
    public static RemoteWorkActivityId New() => new(Guid.NewGuid());
    public static RemoteWorkActivityId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
}
