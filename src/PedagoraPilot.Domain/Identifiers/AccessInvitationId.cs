using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;

public readonly record struct AccessInvitationId(Guid Value) : IIdentifier
{
    public static AccessInvitationId New() => new(Guid.NewGuid());
    public static AccessInvitationId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;
    public override string ToString() => Value.ToString("D");
}
