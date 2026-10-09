using FluentValidation;
using PedagoraPilot.Application.Abstractions.Messaging;
using PedagoraPilot.Contracts.Training;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Application.Training.Cohorts;
public sealed record CreateCohortCommand(Guid ProgramOfferingId, Guid ReferentialVersionId, string Code, string Name, DateOnly StartDate, DateOnly EndDate, int Capacity, Guid? PedagogicalManagerAuthGateUserId, string? PedagogicalManagerDisplayName, string? ExternalKey, string? InitialStatus = null) : ICommand<CohortDto>;
public sealed record ReassignCohortManagerCommand(CohortId Id, Guid AuthGateUserId) : ICommand<CohortDto>;
public sealed record DeleteCohortCommand(CohortId Id) : ICommand<bool>;
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
        RuleFor(x => x.PedagogicalManagerAuthGateUserId).NotNull().NotEqual(Guid.Empty).WithMessage("COHORT_PEDAGOGICAL_MANAGER_REQUIRED");
        RuleFor(x => x.InitialStatus).Must(status => string.IsNullOrWhiteSpace(status) || status.Equals("planned", StringComparison.OrdinalIgnoreCase) || status.Equals("active", StringComparison.OrdinalIgnoreCase) || status.Equals("draft", StringComparison.OrdinalIgnoreCase))
            .WithMessage("COHORT_STATUS_INVALID");
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

public sealed class ReassignCohortManagerCommandValidator : AbstractValidator<ReassignCohortManagerCommand>
{
    public ReassignCohortManagerCommandValidator()
    {
        RuleFor(x => x.Id).Must(id => !id.IsEmpty);
        RuleFor(x => x.AuthGateUserId).NotEmpty().WithMessage("COHORT_PEDAGOGICAL_MANAGER_REQUIRED");
    }
}
