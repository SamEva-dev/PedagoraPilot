using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Domain.Learning;
public sealed class CompetencyDefinition : AggregateRoot<CompetencyDefinitionId>
{
    private CompetencyDefinition()
    {
    }

    private CompetencyDefinition(CompetencyDefinitionId id, Guid referentialVersionId, CompetencyDefinitionId? parentId, string code, string title, CompetencyKind kind, int sortOrder, bool active, string? externalKey) : base(id)
    {
        if (referentialVersionId == Guid.Empty)
            throw new DomainException("COMPETENCY_REFERENTIAL_VERSION_REQUIRED");
        ReferentialVersionId = referentialVersionId;
        ParentId = parentId;
        Code = NormalizeRequired(code, "COMPETENCY_CODE_REQUIRED", 80).ToUpperInvariant();
        Title = NormalizeRequired(title, "COMPETENCY_TITLE_REQUIRED", 300);
        Kind = kind;
        SortOrder = sortOrder;
        Active = active;
        ExternalKey = NormalizeOptional(externalKey, 120);
        CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
        Version = 1;
    }

    public Guid ReferentialVersionId { get; private set; }
    public CompetencyDefinitionId? ParentId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public CompetencyKind Kind { get; private set; }
    public int SortOrder { get; private set; }
    public bool Active { get; private set; }
    public string? ExternalKey { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static CompetencyDefinition Create(Guid referentialVersionId, CompetencyDefinitionId? parentId, string code, string title, CompetencyKind kind, int sortOrder, bool active = true, string? externalKey = null) => new(CompetencyDefinitionId.New(), referentialVersionId, parentId, code, title, kind, sortOrder, active, externalKey);
    private static string NormalizeRequired(string value, string key, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(key);
        var v = value.Trim();
        if (v.Length > max)
            throw new DomainException(key);
        return v;
    }

    private static string? NormalizeOptional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var v = value.Trim();
        return v.Length <= max ? v : v[..max];
    }
}
