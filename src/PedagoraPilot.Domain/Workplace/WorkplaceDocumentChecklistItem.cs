using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Domain.Workplace;
public sealed class WorkplaceDocumentChecklistItem : Entity<WorkplaceDocumentChecklistItemId>
{
    private WorkplaceDocumentChecklistItem()
    {
    }

    internal WorkplaceDocumentChecklistItem(WorkplaceDocumentChecklistItemId id, WorkplacePeriodId periodId, WorkplaceDocumentRequirementId requirementId, string code, string title, string? labelKey, bool mandatory, WorkplaceDocumentStatus status) : base(id)
    {
        PeriodId = periodId;
        RequirementId = requirementId;
        Code = code;
        Title = title;
        LabelKey = labelKey;
        Mandatory = mandatory;
        Status = status;
    }

    public WorkplacePeriodId PeriodId { get; private set; }
    public WorkplaceDocumentRequirementId RequirementId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string? LabelKey { get; private set; }
    public bool Mandatory { get; private set; }
    public WorkplaceDocumentStatus Status { get; private set; }
    public Guid? DocumentId { get; private set; }

    internal void SetStatus(WorkplaceDocumentStatus status, Guid? documentId)
    {
        Status = status;
        DocumentId = documentId;
    }
}
