using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;
public readonly record struct AuditEntryId(Guid Value) : IIdentifier
{
    public static AuditEntryId New() => new(Guid.NewGuid());
    public static implicit operator Guid(AuditEntryId id) => id.Value;
}
