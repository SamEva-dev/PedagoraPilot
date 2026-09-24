using DomainRelay.Abstractions;
using FluentValidation;
using PedagoraPilot.Contracts.Workplace;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Application.Workplace;
public sealed record CreateWorkplacePeriodCommand(EnrollmentId EnrollmentId, string PeriodTypeCode, string Company, string City, string TutorName, string? TutorEmail, string? TutorPhone, DateOnly StartDate, DateOnly EndDate, decimal PlannedHours, bool AgreementReceived, string? Notes) : IRequest<WorkplacePeriodDto>;
public sealed record UpdateWorkplacePeriodCommand(WorkplacePeriodId Id, string Company, string City, string TutorName, string? TutorEmail, string? TutorPhone, DateOnly StartDate, DateOnly EndDate, decimal PlannedHours, bool TrainerVisible, string? Notes) : IRequest<WorkplacePeriodDto>;
public sealed record UpdateWorkplaceHoursCommand(WorkplacePeriodId Id, decimal CompletedHours, string? TutorObservation) : IRequest<WorkplacePeriodDto>;
public sealed record UpdateWorkplaceActivityCommand(WorkplacePeriodId PeriodId, WorkplaceActivityId ActivityId, string Status, string? Comment) : IRequest<WorkplacePeriodDto>;
public sealed record UpdateWorkplaceDocumentCommand(WorkplacePeriodId PeriodId, WorkplaceDocumentChecklistItemId ItemId, string Status, Guid? DocumentId) : IRequest<WorkplacePeriodDto>;
public sealed record RecordWorkplaceEvaluationCommand(WorkplacePeriodId PeriodId, string Kind, string EvaluatorDisplayName, DateTimeOffset? EvaluatedAtUtc, string Summary, string? Strengths, string? ImprovementAreas, bool? Validated) : IRequest<WorkplacePeriodDto>;
public sealed class CreateWorkplacePeriodCommandValidator : AbstractValidator<CreateWorkplacePeriodCommand>
{
    public CreateWorkplacePeriodCommandValidator()
    {
        RuleFor(x => x.EnrollmentId).Must(x => !x.IsEmpty);
        RuleFor(x => x.PeriodTypeCode).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Company).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(120);
        RuleFor(x => x.TutorName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TutorEmail).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.TutorEmail));
        RuleFor(x => x.PlannedHours).GreaterThan(0).LessThanOrEqualTo(2000);
    }
}

public sealed class UpdateWorkplacePeriodCommandValidator : AbstractValidator<UpdateWorkplacePeriodCommand>
{
    public UpdateWorkplacePeriodCommandValidator()
    {
        RuleFor(x => x.Company).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(120);
        RuleFor(x => x.TutorName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PlannedHours).GreaterThan(0).LessThanOrEqualTo(2000);
    }
}

public sealed class UpdateWorkplaceHoursCommandValidator : AbstractValidator<UpdateWorkplaceHoursCommand>
{
    public UpdateWorkplaceHoursCommandValidator() => RuleFor(x => x.CompletedHours).GreaterThanOrEqualTo(0).LessThanOrEqualTo(2000);
}

public sealed class RecordWorkplaceEvaluationCommandValidator : AbstractValidator<RecordWorkplaceEvaluationCommand>
{
    public RecordWorkplaceEvaluationCommandValidator()
    {
        RuleFor(x => x.EvaluatorDisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Summary).NotEmpty().MaximumLength(4000);
    }
}
