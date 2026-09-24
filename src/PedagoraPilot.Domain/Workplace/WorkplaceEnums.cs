namespace PedagoraPilot.Domain.Workplace;
public enum WorkplacePeriodStatus
{
    Planned = 0,
    InProgress = 1,
    Completed = 2,
    Incomplete = 3,
    Cancelled = 4
}

public enum WorkplaceActivityStatus
{
    Pending = 0,
    Done = 1,
    NotApplicable = 2
}

public enum WorkplaceDocumentStatus
{
    Missing = 0,
    Available = 1,
    Validated = 2
}

public enum WorkplaceEvaluationKind
{
    Tutor = 0,
    Trainer = 1,
    SelfAssessment = 2,
    Final = 3
}
