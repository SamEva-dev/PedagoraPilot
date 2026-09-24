using DomainRelay.Mapping.Abstractions.Configuration;
using DomainRelay.Mapping.Abstractions.Profiles;
using PedagoraPilot.Domain.Workplace;

namespace PedagoraPilot.Application.Mapping;
public sealed class WorkplaceMappingProfile : MappingProfile
{
    public override void Configure(IMappingConfiguration configuration)
    {
        configuration.CreateMap<WorkplacePeriod, WorkplacePeriodReadModel>();
        configuration.CreateMap<WorkplaceActivity, WorkplaceActivityReadModel>();
        configuration.CreateMap<WorkplaceDocumentChecklistItem, WorkplaceDocumentReadModel>();
        configuration.CreateMap<WorkplaceEvaluation, WorkplaceEvaluationReadModel>();
    }
}

public sealed class WorkplacePeriodReadModel
{
    public PedagoraPilot.Domain.Identifiers.WorkplacePeriodId Id { get; set; }
    public PedagoraPilot.Domain.Identifiers.EnrollmentId EnrollmentId { get; set; }
    public PedagoraPilot.Domain.Identifiers.CohortId CohortId { get; set; }
    public Guid ReferentialVersionId { get; set; }
    public string PeriodTypeCode { get; set; } = "";
    public string Company { get; set; } = "";
    public string City { get; set; } = "";
    public string TutorName { get; set; } = "";
    public string? TutorEmail { get; set; }
    public string? TutorPhone { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int PlannedMinutes { get; set; }
    public int CompletedMinutes { get; set; }
    public WorkplacePeriodStatus Status { get; set; }
    public bool AgreementReceived { get; set; }
    public bool TrainerVisible { get; set; }
    public string? Notes { get; set; }
    public string? TutorObservation { get; set; }
}

public sealed class WorkplaceActivityReadModel
{
    public PedagoraPilot.Domain.Identifiers.WorkplaceActivityId Id { get; set; }
    public PedagoraPilot.Domain.Identifiers.WorkplaceActivityDefinitionId DefinitionId { get; set; }
    public string Code { get; set; } = "";
    public string Title { get; set; } = "";
    public string? LabelKey { get; set; }
    public bool Mandatory { get; set; }
    public WorkplaceActivityStatus Status { get; set; }
    public string? Comment { get; set; }
}

public sealed class WorkplaceDocumentReadModel
{
    public PedagoraPilot.Domain.Identifiers.WorkplaceDocumentChecklistItemId Id { get; set; }
    public PedagoraPilot.Domain.Identifiers.WorkplaceDocumentRequirementId RequirementId { get; set; }
    public string Code { get; set; } = "";
    public string Title { get; set; } = "";
    public string? LabelKey { get; set; }
    public bool Mandatory { get; set; }
    public WorkplaceDocumentStatus Status { get; set; }
    public Guid? DocumentId { get; set; }
}

public sealed class WorkplaceEvaluationReadModel
{
    public PedagoraPilot.Domain.Identifiers.WorkplaceEvaluationId Id { get; set; }
    public WorkplaceEvaluationKind Kind { get; set; }
    public string EvaluatorDisplayName { get; set; } = "";
    public DateTimeOffset EvaluatedAtUtc { get; set; }
    public string Summary { get; set; } = "";
    public string? Strengths { get; set; }
    public string? ImprovementAreas { get; set; }
    public bool? Validated { get; set; }
}
