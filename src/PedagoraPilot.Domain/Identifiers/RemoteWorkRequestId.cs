using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;
public readonly record struct RemoteWorkRequestId(Guid Value) : IIdentifier
{
    public static RemoteWorkRequestId New() => new(Guid.NewGuid());
    public static RemoteWorkRequestId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;

    public override string ToString() => Value.ToString();
}
