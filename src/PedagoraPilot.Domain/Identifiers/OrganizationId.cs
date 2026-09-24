using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;
public readonly record struct OrganizationId(Guid Value) : IIdentifier
{
    public static OrganizationId New() => new(Guid.NewGuid());
    public static OrganizationId Empty => new(Guid.Empty);

    public override string ToString() => Value.ToString();
    public static implicit operator Guid(OrganizationId id) => id.Value;
    public static explicit operator OrganizationId(Guid value) => new(value);
}
