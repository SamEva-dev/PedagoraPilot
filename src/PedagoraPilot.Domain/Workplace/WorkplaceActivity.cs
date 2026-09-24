using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Domain.Workplace;
public sealed class WorkplaceActivity : Entity<WorkplaceActivityId>
{
    private WorkplaceActivity()
    {
    }

    internal WorkplaceActivity(WorkplaceActivityId id, WorkplacePeriodId periodId, WorkplaceActivityDefinitionId definitionId, string code, string title, string? labelKey, bool mandatory) : base(id)
    {
        PeriodId = periodId;
        DefinitionId = definitionId;
        Code = code;
        Title = title;
        LabelKey = labelKey;
        Mandatory = mandatory;
        Status = WorkplaceActivityStatus.Pending;
    }

    public WorkplacePeriodId PeriodId { get; private set; }
    public WorkplaceActivityDefinitionId DefinitionId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string? LabelKey { get; private set; }
    public bool Mandatory { get; private set; }
    public WorkplaceActivityStatus Status { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public string? Comment { get; private set; }

    internal void SetStatus(WorkplaceActivityStatus status, string? comment)
    {
        Status = status;
        CompletedAtUtc = status == WorkplaceActivityStatus.Done ? DateTimeOffset.UtcNow : null;
        Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
    }
}
