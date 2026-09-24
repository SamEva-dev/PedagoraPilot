using PedagoraPilot.Domain.Catalog.Events;
using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Catalog;
public sealed class ProgramOffering : AggregateRoot
{
    private ProgramOffering()
    {
    }

    private ProgramOffering(Guid id, Guid siteId, Guid programId, string? externalKey) : base(id)
    {
        SiteId = siteId;
        ProgramId = programId;
        ExternalKey = externalKey;
        IsActive = true;
        CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
        Version = 1;
    }

    public Guid SiteId { get; private set; }
    public Guid ProgramId { get; private set; }
    public bool IsActive { get; private set; }
    public string? ExternalKey { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static ProgramOffering Create(Guid siteId, Guid programId, string? externalKey = null)
    {
        var x = new ProgramOffering(Guid.NewGuid(), siteId, programId, externalKey);
        x.RaiseDomainEvent(new ProgramOfferingChangedDomainEvent(x.Id, siteId, programId, true));
        return x;
    }

    public void SetActive(bool active)
    {
        if (IsActive == active)
            return;
        IsActive = active;
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new ProgramOfferingChangedDomainEvent(Id, SiteId, ProgramId, active));
    }
}
