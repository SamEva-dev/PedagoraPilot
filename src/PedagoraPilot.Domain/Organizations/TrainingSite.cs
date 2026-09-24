using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Organizations.Events;
using PedagoraPilot.Domain.Organizations.ValueObjects;

namespace PedagoraPilot.Domain.Organizations;
public sealed class TrainingSite : AggregateRoot
{
    private TrainingSite()
    {
    }

    private TrainingSite(Guid id, Guid organizationId, SiteCode code, string name, string city, string? externalKey) : base(id)
    {
        if (organizationId == Guid.Empty)
            throw new DomainException("SITE_ORGANIZATION_REQUIRED");
        OrganizationId = organizationId;
        Code = code;
        Name = Normalize(name, 160, "SITE_NAME_REQUIRED");
        City = Normalize(city, 120, "SITE_CITY_REQUIRED");
        ExternalKey = NormalizeOptional(externalKey, 80);
        Status = TrainingSiteStatus.Active;
        CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new TrainingSiteCreatedDomainEvent(Id, OrganizationId, Code.Value, Name));
    }

    public Guid OrganizationId { get; private set; }
    public SiteCode Code { get; private set; } = null!;
    public string Name { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string? ExternalKey { get; private set; }
    public TrainingSiteStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public uint Version { get; private set; }
    public bool IsActive => Status == TrainingSiteStatus.Active;

    public static TrainingSite Create(Guid organizationId, string code, string name, string city, string? externalKey = null) => new(Guid.NewGuid(), organizationId, SiteCode.Create(code), name, city, externalKey);
    public void Update(string name, string city)
    {
        Name = Normalize(name, 160, "SITE_NAME_REQUIRED");
        City = Normalize(city, 120, "SITE_CITY_REQUIRED");
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new TrainingSiteUpdatedDomainEvent(Id, OrganizationId, Code.Value, Name));
    }

    public void Deactivate()
    {
        Status = TrainingSiteStatus.Inactive;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Activate()
    {
        Status = TrainingSiteStatus.Active;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string Normalize(string value, int max, string key)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(key);
        var s = value.Trim();
        if (s.Length > max)
            throw new DomainException("VALIDATION_MAX_LENGTH");
        return s;
    }

    private static string? NormalizeOptional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var s = value.Trim();
        if (s.Length > max)
            throw new DomainException("VALIDATION_MAX_LENGTH");
        return s;
    }
}
