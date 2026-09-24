using FluentValidation;
using PedagoraPilot.Application.Abstractions.Messaging;
using PedagoraPilot.Contracts.Training;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Application.Training.Cohorts;
public sealed record CreateCohortCommand(Guid ProgramOfferingId, Guid ReferentialVersionId, string Code, string Name, DateOnly StartDate, DateOnly EndDate, int Capacity, string? ExternalKey) : ICommand<CohortDto>;
public sealed record UpdateCohortCommand(CohortId Id, string Name, DateOnly StartDate, DateOnly EndDate, int Capacity, string Status) : ICommand<CohortDto>;
public sealed class CreateCohortCommandValidator : AbstractValidator<CreateCohortCommand>
{
    public CreateCohortCommandValidator()
    {
        RuleFor(x => x.ProgramOfferingId).NotEmpty();
        RuleFor(x => x.ReferentialVersionId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Capacity).InclusiveBetween(1, 500);
        RuleFor(x => x).Must(x => x.EndDate >= x.StartDate).WithMessage("COHORT_DATE_RANGE_INVALID");
    }
}

public sealed class UpdateCohortCommandValidator : AbstractValidator<UpdateCohortCommand>
{
    public UpdateCohortCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Capacity).InclusiveBetween(1, 500);
        RuleFor(x => x).Must(x => x.EndDate >= x.StartDate).WithMessage("COHORT_DATE_RANGE_INVALID");
    }
}
