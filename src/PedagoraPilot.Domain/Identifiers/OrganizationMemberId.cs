using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Identifiers;

public readonly record struct OrganizationMemberId(Guid Value) : IIdentifier
{
    public static OrganizationMemberId New() => new(Guid.NewGuid());
    public static OrganizationMemberId Empty => new(Guid.Empty);
    public bool IsEmpty => Value == Guid.Empty;
    public override string ToString() => Value.ToString();
}
