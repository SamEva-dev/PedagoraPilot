using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Domain.DistanceLearning;
public sealed class AsyncModuleStep : Entity<AsyncModuleStepId>
{
    private AsyncModuleStep()
    {
    }

    internal AsyncModuleStep(AsyncModuleStepId id, AsyncLearningModuleId moduleId, string code, string label, int sortOrder) : base(id)
    {
        ModuleId = moduleId;
        Code = string.IsNullOrWhiteSpace(code) ? throw new DomainException("DISTANCE_STEP_CODE_REQUIRED") : code.Trim();
        Label = string.IsNullOrWhiteSpace(label) ? throw new DomainException("DISTANCE_STEP_LABEL_REQUIRED") : label.Trim();
        SortOrder = sortOrder;
    }

    public AsyncLearningModuleId ModuleId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Label { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
}
