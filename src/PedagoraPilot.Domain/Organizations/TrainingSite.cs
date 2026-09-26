using DomainRelay.Abstractions;
using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Organizations.Events;
using PedagoraPilot.Domain.Organizations.ValueObjects;

namespace PedagoraPilot.Domain.Organizations;
public sealed class TrainingSite : AggregateRoot
{
    private TrainingSite() { }

    private TrainingSite(Guid id, Guid organizationId, SiteCode code, string name, string city,
        string? externalKey, string? address, string? postalCode, string? phone, string? email,
        string? manager, TrainingSiteStatus status) : base(id)
    {
        if (organizationId == Guid.Empty)
            throw new DomainException("SITE_ORGANIZATION_REQUIRED");
        OrganizationId = organizationId;
        Code = code;
        Name = Normalize(name, 160, "SITE_NAME_REQUIRED");
        City = Normalize(city, 120, "SITE_CITY_REQUIRED");
        Address = NormalizeOptionalText(address, 240);
        PostalCode = NormalizeOptionalText(postalCode, 32);
        Phone = NormalizeOptionalText(phone, 40);
        Email = NormalizeOptionalText(email, 240);
        Manager = NormalizeOptionalText(manager, 160);
        ExternalKey = NormalizeOptional(externalKey, 80);
        Status = status == TrainingSiteStatus.Archived ? TrainingSiteStatus.Inactive : status;
        CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new TrainingSiteCreatedDomainEvent(Id, OrganizationId, Code.Value, Name));
    }

    public Guid OrganizationId { get; private set; }
    public SiteCode Code { get; private set; } = null!;
    public string Name { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string PostalCode { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Manager { get; private set; } = string.Empty;
    public string? ExternalKey { get; private set; }
    public TrainingSiteStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public uint Version { get; private set; }
    public bool IsActive => Status is TrainingSiteStatus.Active or TrainingSiteStatus.Attention;

    public static TrainingSite Create(Guid organizationId, string code, string name, string city,
        string? externalKey = null, string? address = null, string? postalCode = null,
        string? phone = null, string? email = null, string? manager = null,
        TrainingSiteStatus status = TrainingSiteStatus.Active)
        => new(Guid.NewGuid(), organizationId, SiteCode.Create(code), name, city, externalKey,
            address, postalCode, phone, email, manager, status);

    public void Update(string name, string city)
        => Update(Code.Value, name, city, Address, PostalCode, Phone, Email, Manager, Status);

    public void Update(string code, string name, string city, string? address, string? postalCode,
        string? phone, string? email, string? manager, TrainingSiteStatus status)
    {
        Code = SiteCode.Create(code);
        Name = Normalize(name, 160, "SITE_NAME_REQUIRED");
        City = Normalize(city, 120, "SITE_CITY_REQUIRED");
        Address = NormalizeOptionalText(address, 240);
        PostalCode = NormalizeOptionalText(postalCode, 32);
        Phone = NormalizeOptionalText(phone, 40);
        Email = NormalizeOptionalText(email, 240);
        Manager = NormalizeOptionalText(manager, 160);
        Status = status == TrainingSiteStatus.Archived ? TrainingSiteStatus.Inactive : status;
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new TrainingSiteUpdatedDomainEvent(Id, OrganizationId, Code.Value, Name));
    }

    public void Deactivate() { Status = TrainingSiteStatus.Inactive; UpdatedAtUtc = DateTime.UtcNow; }
    public void Activate() { Status = TrainingSiteStatus.Active; UpdatedAtUtc = DateTime.UtcNow; }

    private static string Normalize(string value, int max, string key)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new DomainException(key);
        var s = value.Trim();
        if (s.Length > max) throw new DomainException("VALIDATION_MAX_LENGTH");
        return s;
    }

    private static string NormalizeOptionalText(string? value, int max)
        => NormalizeOptional(value, max) ?? string.Empty;

    private static string? NormalizeOptional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var s = value.Trim();
        if (s.Length > max) throw new DomainException("VALIDATION_MAX_LENGTH");
        return s;
    }
}
