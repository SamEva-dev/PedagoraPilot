using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Organizations.Events;
using PedagoraPilot.Domain.Organizations.ValueObjects;

namespace PedagoraPilot.Domain.Organizations;
public sealed class Organization : AggregateRoot, IAuditableEntity
{
    private Organization()
    {
    }

    private Organization(Guid id, Guid ownerUserId, OrganizationCode code, string legalName, string countryCode, string ownerEmail, string? ownerPhone) : base(id)
    {
        OwnerUserId = ownerUserId;
        Code = code;
        LegalName = legalName;
        CountryCode = countryCode;
        OwnerEmail = ownerEmail;
        OwnerPhone = ownerPhone;
        Status = OrganizationStatus.Active;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
        RaiseDomainEvent(new OrganizationProvisionedDomainEvent(Id, OwnerUserId, Code.Value, LegalName));
    }

    public Guid OwnerUserId { get; private set; }
    public OrganizationCode Code { get; private set; } = null!;
    public string LegalName { get; private set; } = string.Empty;
    public string CountryCode { get; private set; } = "FR";
    public string OwnerEmail { get; private set; } = string.Empty;
    public string? OwnerPhone { get; private set; }
    public OrganizationStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public uint Version { get; private set; }

    DateTimeOffset IAuditableEntity.CreatedAtUtc => new(CreatedAtUtc, TimeSpan.Zero);
    public Guid? CreatedByUserId { get; private set; }
    public DateTimeOffset? LastModifiedAtUtc { get; private set; }
    public Guid? LastModifiedByUserId { get; private set; }

    public void SetCreatedAudit(DateTimeOffset createdAtUtc, Guid? createdByUserId)
    {
        CreatedAtUtc = createdAtUtc.UtcDateTime;
        CreatedByUserId = createdByUserId;
    }

    public void SetModifiedAudit(DateTimeOffset modifiedAtUtc, Guid? modifiedByUserId)
    {
        LastModifiedAtUtc = modifiedAtUtc.ToUniversalTime();
        LastModifiedByUserId = modifiedByUserId;
        UpdatedAtUtc = modifiedAtUtc.UtcDateTime;
    }

    public bool IsLoginAllowed => Status is OrganizationStatus.Draft or OrganizationStatus.Active or OrganizationStatus.Restricted;

    public static Organization Provision(Guid ownerUserId, string code, string legalName, string? countryCode, string ownerEmail, string? ownerPhone)
    {
        if (ownerUserId == Guid.Empty)
            throw new DomainException("ORGANIZATION_OWNER_REQUIRED");
        ArgumentException.ThrowIfNullOrWhiteSpace(legalName);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerEmail);
        return new Organization(Guid.NewGuid(), ownerUserId, OrganizationCode.Create(code), legalName.Trim(), NormalizeCountry(countryCode), ownerEmail.Trim().ToLowerInvariant(), string.IsNullOrWhiteSpace(ownerPhone) ? null : ownerPhone.Trim());
    }

    private static string NormalizeCountry(string? value)
    {
        var country = string.IsNullOrWhiteSpace(value) ? "FR" : value.Trim().ToUpperInvariant();
        if (country.Length != 2)
            throw new DomainException("ORGANIZATION_COUNTRY_CODE_INVALID");
        return country;
    }
}
