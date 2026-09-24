using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Domain.Workplace;
public sealed class WorkplaceDocumentRequirement : AggregateRoot<WorkplaceDocumentRequirementId>
{
    private WorkplaceDocumentRequirement()
    {
    }

    private WorkplaceDocumentRequirement(WorkplaceDocumentRequirementId id, Guid referentialVersionId, string periodTypeCode, string code, string title, string? labelKey, bool mandatory, int sortOrder) : base(id)
    {
        if (referentialVersionId == Guid.Empty)
            throw new DomainException("WORKPLACE_REFERENTIAL_REQUIRED");
        ReferentialVersionId = referentialVersionId;
        PeriodTypeCode = Required(periodTypeCode, "WORKPLACE_PERIOD_TYPE_REQUIRED").ToUpperInvariant();
        Code = Required(code, "WORKPLACE_DOCUMENT_CODE_REQUIRED").ToUpperInvariant();
        Title = Required(title, "WORKPLACE_DOCUMENT_TITLE_REQUIRED");
        LabelKey = string.IsNullOrWhiteSpace(labelKey) ? null : labelKey.Trim();
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

    public static WorkplaceDocumentRequirement Create(Guid r, string p, string c, string t, string? l, bool m, int s) => new(WorkplaceDocumentRequirementId.New(), r, p, c, t, l, m, s);
    private static string Required(string? v, string k)
    {
        if (string.IsNullOrWhiteSpace(v))
            throw new DomainException(k);
        return v.Trim();
    }
}
