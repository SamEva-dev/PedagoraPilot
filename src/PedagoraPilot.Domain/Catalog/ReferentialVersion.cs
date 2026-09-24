using PedagoraPilot.Domain.Catalog.Events;
using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Catalog;
public sealed class ReferentialVersion : AggregateRoot
{
    private readonly List<string> _capabilities = [];
    private ReferentialVersion()
    {
    }

    private ReferentialVersion(Guid id, Guid referentialId, string versionLabel, string? certificationCode, DateOnly effectiveFrom, int totalHours, int sheetCount, int requiredDocumentCount, string? notesKey, string? externalKey) : base(id)
    {
        ReferentialId = referentialId;
        VersionLabel = versionLabel.Trim();
        CertificationCode = certificationCode?.Trim();
        EffectiveFrom = effectiveFrom;
        TotalHours = totalHours;
        SheetCount = sheetCount;
        RequiredDocumentCount = requiredDocumentCount;
        NotesKey = notesKey;
        ExternalKey = externalKey;
        Status = ReferentialVersionStatus.Draft;
        CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
        Version = 1;
    }

    public Guid ReferentialId { get; private set; }
    public string VersionLabel { get; private set; } = string.Empty;
    public string? CertificationCode { get; private set; }
    public ReferentialVersionStatus Status { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public int TotalHours { get; private set; }
    public int SheetCount { get; private set; }
    public int RequiredDocumentCount { get; private set; }
    public string? NotesKey { get; private set; }
    public string? ExternalKey { get; private set; }
    public DateTime? PublishedAtUtc { get; private set; }
    public IReadOnlyCollection<string> Capabilities => _capabilities.AsReadOnly();
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static ReferentialVersion CreateDraft(Guid referentialId, string versionLabel, string? certificationCode, DateOnly effectiveFrom, int totalHours, int sheetCount, int requiredDocumentCount, IEnumerable<string> capabilities, string? notesKey = null, string? externalKey = null)
    {
        if (totalHours <= 0)
            throw new DomainException("REFERENTIAL_TOTAL_HOURS_INVALID");
        var x = new ReferentialVersion(Guid.NewGuid(), referentialId, versionLabel, certificationCode, effectiveFrom, totalHours, sheetCount, requiredDocumentCount, notesKey, externalKey);
        x._capabilities.AddRange(capabilities.Distinct(StringComparer.OrdinalIgnoreCase));
        x.RaiseDomainEvent(new ReferentialVersionCreatedDomainEvent(x.Id, referentialId));
        return x;
    }

    public void Publish(DateTime utcNow)
    {
        if (Status != ReferentialVersionStatus.Draft)
            throw new DomainException("REFERENTIAL_VERSION_NOT_DRAFT");
        Status = ReferentialVersionStatus.Active;
        PublishedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
        RaiseDomainEvent(new ReferentialVersionPublishedDomainEvent(Id, ReferentialId));
    }

    public void Archive(DateOnly effectiveTo)
    {
        if (Status == ReferentialVersionStatus.Draft)
            throw new DomainException("REFERENTIAL_DRAFT_CANNOT_ARCHIVE");
        Status = ReferentialVersionStatus.Archived;
        EffectiveTo = effectiveTo;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
