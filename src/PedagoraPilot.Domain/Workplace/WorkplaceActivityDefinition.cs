using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Domain.Workplace;
public sealed class WorkplaceActivityDefinition : AggregateRoot<WorkplaceActivityDefinitionId>
{
    private WorkplaceActivityDefinition()
    {
    }

    private WorkplaceActivityDefinition(WorkplaceActivityDefinitionId id, Guid referentialVersionId, string periodTypeCode, string code, string title, string? labelKey, bool mandatory, int sortOrder) : base(id)
    {
        if (referentialVersionId == Guid.Empty)
            throw new DomainException("WORKPLACE_REFERENTIAL_REQUIRED");
        ReferentialVersionId = referentialVersionId;
        PeriodTypeCode = Normalize(periodTypeCode, "WORKPLACE_PERIOD_TYPE_REQUIRED", 64).ToUpperInvariant();
        Code = Normalize(code, "WORKPLACE_ACTIVITY_CODE_REQUIRED", 100).ToUpperInvariant();
        Title = Normalize(title, "WORKPLACE_ACTIVITY_TITLE_REQUIRED", 300);
        LabelKey = Optional(labelKey, 200);
        Mandatory = mandatory;
        SortOrder = sortOrder;
        Active = true;
        CreatedAtUtc = UpdatedAtUtc = DateTime.UtcNow;
        Version = 1;
    }

    public Guid ReferentialVersionId { get; private set; }
    public string PeriodTypeCode { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string? LabelKey { get; private set; }
    public bool Mandatory { get; private set; }
    public int SortOrder { get; private set; }
    public bool Active { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public long Version { get; private set; }

    public static WorkplaceActivityDefinition Create(Guid referentialVersionId, string periodTypeCode, string code, string title, string? labelKey, bool mandatory, int sortOrder) => new(WorkplaceActivityDefinitionId.New(), referentialVersionId, periodTypeCode, code, title, labelKey, mandatory, sortOrder);
    private static string Normalize(string? v, string key, int max)
    {
        if (string.IsNullOrWhiteSpace(v))
            throw new DomainException(key);
        var x = v.Trim();
        if (x.Length > max)
            throw new DomainException(key);
        return x;
    }

    private static string? Optional(string? v, int max)
    {
        if (string.IsNullOrWhiteSpace(v))
            return null;
        var x = v.Trim();
        return x.Length <= max ? x : x[..max];
    }
}
