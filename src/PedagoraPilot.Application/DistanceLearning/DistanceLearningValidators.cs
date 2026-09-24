using FluentValidation;

namespace PedagoraPilot.Application.DistanceLearning;
public sealed class CreateDistanceLearningSessionCommandValidator : AbstractValidator<CreateDistanceLearningSessionCommand>
{
    public CreateDistanceLearningSessionCommandValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty();
        RuleFor(x => x.ProgramId).NotEmpty();
        RuleFor(x => x.CohortId.Value).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(240);
        RuleFor(x => x.TrainerDisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TrainerEmail).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.TrainerEmail));
        RuleFor(x => x.JoinUrl).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.EndsAtUtc).GreaterThan(x => x.StartsAtUtc);
    }
}

public sealed class CreateAsyncLearningModuleCommandValidator : AbstractValidator<CreateAsyncLearningModuleCommand>
{
    public CreateAsyncLearningModuleCommandValidator()
    {
        RuleFor(x => x.SiteId).NotEmpty();
        RuleFor(x => x.ProgramId).NotEmpty();
        RuleFor(x => x.CohortId.Value).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(240);
        RuleFor(x => x.EstimatedMinutes).GreaterThan(0);
        RuleFor(x => x.ExpectedStudents).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateAsyncModuleProgressCommandValidator : AbstractValidator<UpdateAsyncModuleProgressCommand>
{
    public UpdateAsyncModuleProgressCommandValidator()
    {
        RuleFor(x => x.ProgressPercent).InclusiveBetween(0, 100);
        RuleFor(x => x.CompletedStudents).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AverageScore).InclusiveBetween(0, 100).When(x => x.AverageScore.HasValue);
    }
}
