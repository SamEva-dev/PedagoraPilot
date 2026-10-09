using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Access.Events;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Domain.Access;

/// <summary>
/// PedagoraPilot business projection of an AuthGate identity inside one organization.
/// AuthGate owns authentication; this aggregate owns the product-side member lifecycle.
/// Role-specific tables reference this aggregate so one person may hold several roles
/// without duplicating identity data.
/// </summary>
public sealed class OrganizationMember : AggregateRoot<OrganizationMemberId>
{
    private OrganizationMember() { }

    private OrganizationMember(
        OrganizationMemberId id,
        Guid organizationId,
        Guid authGateUserId,
        string email,
        string firstName,
        string lastName) : base(id)
    {
        OrganizationId = organizationId;
        AuthGateUserId = authGateUserId;
        Email = NormalizeEmail(email);
        FirstName = Normalize(firstName, 120);
        LastName = Normalize(lastName, 120);
        IsActive = true;
        CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
        Version = 1;
    }

    public Guid OrganizationId { get; private set; }
    public Guid AuthGateUserId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public string DisplayName => string.Join(' ', new[] { FirstName, LastName }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();

    public static OrganizationMember Create(Guid organizationId, Guid authGateUserId, string email, string firstName, string lastName)
    {
        if (organizationId == Guid.Empty) throw new DomainException("MEMBER_ORGANIZATION_REQUIRED");
        if (authGateUserId == Guid.Empty) throw new DomainException("MEMBER_AUTHGATE_USER_REQUIRED");
        if (string.IsNullOrWhiteSpace(email)) throw new DomainException("MEMBER_EMAIL_REQUIRED");
        return new OrganizationMember(OrganizationMemberId.New(), organizationId, authGateUserId, email, firstName, lastName);
    }

    public void SynchronizeIdentity(string email, string firstName, string lastName, bool active)
    {
        Email = NormalizeEmail(email);
        FirstName = Normalize(firstName, 120);
        LastName = Normalize(lastName, 120);
        IsActive = active;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RegisterRoleChange(string roleCode)
    {
        if (string.IsNullOrWhiteSpace(roleCode)) throw new DomainException("MEMBER_ROLE_REQUIRED");
        UpdatedAtUtc = DateTime.UtcNow;
        Version++;
        RaiseDomainEvent(new AccessMemberRoleChangedDomainEvent(OrganizationId, AuthGateUserId, roleCode.Trim()));
    }

    public void RegisterStatusChange(bool active)
    {
        UpdatedAtUtc = DateTime.UtcNow;
        Version++;
        RaiseDomainEvent(new AccessMemberStatusChangedDomainEvent(OrganizationId, AuthGateUserId, active));
    }

    public void RegisterRevoked()
    {
        UpdatedAtUtc = DateTime.UtcNow;
        Version++;
        RaiseDomainEvent(new AccessMemberRevokedDomainEvent(OrganizationId, AuthGateUserId));
    }

    private static string Normalize(string? value, int max)
    {
        var normalized = (value ?? string.Empty).Trim();
        return normalized.Length <= max ? normalized : normalized[..max];
    }
    private static string NormalizeEmail(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length > 254) throw new DomainException("MEMBER_EMAIL_INVALID");
        return normalized;
    }
}
