using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Certification.Events;

namespace PedagoraPilot.Domain.Certification;
public sealed class CertificationScheme : AggregateRoot<CertificationSchemeId>
{
    private readonly List<CertificationUnit> _units = [];
    private readonly List<CertificationStepDefinition> _steps = [];
    private CertificationScheme()
    {
    }

    private CertificationScheme(CertificationSchemeId id, Guid referentialVersionId, string code, string name) : base(id)
    {
        if (referentialVersionId == Guid.Empty)
            throw new DomainException("CERTIFICATION_REFERENTIAL_REQUIRED");
        ReferentialVersionId = referentialVersionId;
        Code = Required(code, "CERTIFICATION_SCHEME_CODE_REQUIRED", 80).ToUpperInvariant();
        Name = Required(name, "CERTIFICATION_SCHEME_NAME_REQUIRED", 240);
        Status = CertificationSchemeStatus.Draft;
        CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
        Version = 1;
    }

    public Guid ReferentialVersionId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public CertificationSchemeStatus Status { get; private set; }
    public DateOnly? EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }
    public IReadOnlyCollection<CertificationUnit> Units => _units.AsReadOnly();
    public IReadOnlyCollection<CertificationStepDefinition> Steps => _steps.AsReadOnly();

    public static CertificationScheme Create(Guid referentialVersionId, string code, string name)
    {
        var x = new CertificationScheme(CertificationSchemeId.New(), referentialVersionId, code, name);
        x.RaiseDomainEvent(new CertificationSchemeCreatedDomainEvent(x.Id, referentialVersionId));
        return x;
    }

    public CertificationUnit AddUnit(string code, string title, int sortOrder)
    {
        EnsureDraft();
        var normalized = Required(code, "CERTIFICATION_UNIT_CODE_REQUIRED", 80).ToUpperInvariant();
        if (_units.Any(x => x.Code == normalized))
            throw new DomainException("CERTIFICATION_UNIT_DUPLICATE");
        var unit = new CertificationUnit(CertificationUnitId.New(), Id, normalized, Required(title, "CERTIFICATION_UNIT_TITLE_REQUIRED", 300), sortOrder);
        _units.Add(unit);
        UpdatedAtUtc = DateTime.UtcNow;
        return unit;
    }

    public CertificationStepDefinition AddStep(CertificationUnitId? unitId, string code, string title, CertificationStepKind kind, int durationMinutes, int sortOrder)
    {
        EnsureDraft();
        if (durationMinutes <= 0)
            throw new DomainException("CERTIFICATION_STEP_DURATION_INVALID");
        if (unitId.HasValue && _units.All(x => x.Id != unitId.Value))
            throw new DomainException("CERTIFICATION_UNIT_NOT_FOUND");
        var normalized = Required(code, "CERTIFICATION_STEP_CODE_REQUIRED", 100).ToUpperInvariant();
        if (_steps.Any(x => x.Code == normalized))
            throw new DomainException("CERTIFICATION_STEP_DUPLICATE");
        var step = new CertificationStepDefinition(CertificationStepDefinitionId.New(), Id, unitId, normalized, Required(title, "CERTIFICATION_STEP_TITLE_REQUIRED", 300), kind, durationMinutes, sortOrder);
        _steps.Add(step);
        UpdatedAtUtc = DateTime.UtcNow;
        return step;
    }

    public void Publish(DateOnly effectiveFrom, DateOnly? effectiveTo)
    {
        EnsureDraft();
        if (_units.Count == 0 || _steps.Count == 0)
            throw new DomainException("CERTIFICATION_SCHEME_INCOMPLETE");
        if (effectiveTo.HasValue && effectiveTo < effectiveFrom)
            throw new DomainException("CERTIFICATION_EFFECTIVE_RANGE_INVALID");
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        Status = CertificationSchemeStatus.Published;
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new CertificationSchemePublishedDomainEvent(Id, ReferentialVersionId));
    }

    public void Archive()
    {
        if (Status == CertificationSchemeStatus.Archived)
            return;
        Status = CertificationSchemeStatus.Archived;
        UpdatedAtUtc = DateTime.UtcNow;
        RaiseDomainEvent(new CertificationSchemeArchivedDomainEvent(Id, ReferentialVersionId));
    }

    private void EnsureDraft()
    {
        if (Status != CertificationSchemeStatus.Draft)
            throw new DomainException("CERTIFICATION_SCHEME_LOCKED");
    }

    private static string Required(string? v, string key, int max)
    {
        if (string.IsNullOrWhiteSpace(v))
            throw new DomainException(key);
        var x = v.Trim();
        if (x.Length > max)
            throw new DomainException(key);
        return x;
    }
}

public sealed class CertificationUnit
{
    private CertificationUnit()
    {
    }

    internal CertificationUnit(CertificationUnitId id, CertificationSchemeId schemeId, string code, string title, int sortOrder)
    {
        Id = id;
        SchemeId = schemeId;
        Code = code;
        Title = title;
        SortOrder = sortOrder;
    }

    public CertificationUnitId Id { get; private set; }
    public CertificationSchemeId SchemeId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
}

public sealed class CertificationStepDefinition
{
    private CertificationStepDefinition()
    {
    }

    internal CertificationStepDefinition(CertificationStepDefinitionId id, CertificationSchemeId schemeId, CertificationUnitId? unitId, string code, string title, CertificationStepKind kind, int durationMinutes, int sortOrder)
    {
        Id = id;
        SchemeId = schemeId;
        UnitId = unitId;
        Code = code;
        Title = title;
        Kind = kind;
        DurationMinutes = durationMinutes;
        SortOrder = sortOrder;
    }

    public CertificationStepDefinitionId Id { get; private set; }
    public CertificationSchemeId SchemeId { get; private set; }
    public CertificationUnitId? UnitId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public CertificationStepKind Kind { get; private set; }
    public int DurationMinutes { get; private set; }
    public int SortOrder { get; private set; }
}
