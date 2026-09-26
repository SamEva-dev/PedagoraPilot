using FluentValidation;
using PedagoraPilot.Application.Abstractions.Messaging;
using PedagoraPilot.Contracts.Learning;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Application.Learning.Progress;
public sealed record EvaluateCompetencyCommand(EnrollmentId EnrollmentId, CompetencyDefinitionId CompetencyDefinitionId, string Level, decimal? Score, string? Comment, Guid? EvaluatorAuthGateUserId, string? EvaluatorDisplayName, DateTimeOffset? EvaluatedAtUtc) : ICommand<LearnerCompetencyDto>;
public sealed record CreatePedagogicalTopicCommand(Guid ReferentialVersionId, int Number, string Title, string Category, int DurationMinutes, string? Reference, bool Active, string? Objective, string? Example, string? Correction) : ICommand<PedagogicalTopicDto>;
public sealed record UpdatePedagogicalTopicCommand(Guid ReferentialVersionId, PedagogicalTopicId TopicId, int Number, string Title, string Category, int DurationMinutes, string? Reference, bool Active, string? Objective, string? Example, string? Correction) : ICommand<PedagogicalTopicDto>;
public sealed record DeletePedagogicalTopicCommand(Guid ReferentialVersionId, PedagogicalTopicId TopicId) : ICommand<bool>;
public sealed record UpdateTopicProgressCommand(
    EnrollmentId EnrollmentId,
    PedagogicalTopicId TopicId,
    string Status,
    DateOnly? PreparationDate,
    DateOnly? PresentationDate,
    int? PresentationDurationMinutes,
    string? EvaluatorDisplayName,
    string? PositivePoints,
    string? Improvements,
    string? Comment,
    string? NextObjective,
    IReadOnlyCollection<TopicEvaluationCriterionRequest>? EvaluationCriteria) : ICommand<LearnerTopicProgressDto>;
public sealed record RecordDrivingEvaluationCommand(EnrollmentId EnrollmentId, CompetencyDefinitionId CompetencyDefinitionId, TrainingSessionId? TrainingSessionId, DateTimeOffset EvaluatedAtUtc, string? TrainerAuthGateUserId, string TrainerDisplayName, string Subject, string? Positive, string? Difficulty, string? NextGoal, string? FreeObservation, IReadOnlyCollection<DrivingCriterionRequest> Criteria) : ICommand<DrivingEvaluationDto>;

public sealed class CreatePedagogicalTopicCommandValidator : AbstractValidator<CreatePedagogicalTopicCommand>
{
    public CreatePedagogicalTopicCommandValidator()
    {
        RuleFor(x => x.ReferentialVersionId).NotEqual(Guid.Empty);
        RuleFor(x => x.Number).GreaterThan(0);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(80);
        RuleFor(x => x.DurationMinutes).InclusiveBetween(1, 1440);
        RuleFor(x => x.Reference).MaximumLength(500);
        RuleFor(x => x.Objective).MaximumLength(2000);
        RuleFor(x => x.Example).MaximumLength(4000);
        RuleFor(x => x.Correction).MaximumLength(8000);
    }
}

public sealed class UpdatePedagogicalTopicCommandValidator : AbstractValidator<UpdatePedagogicalTopicCommand>
{
    public UpdatePedagogicalTopicCommandValidator()
    {
        RuleFor(x => x.ReferentialVersionId).NotEqual(Guid.Empty);
        RuleFor(x => x.TopicId).Must(x => !x.IsEmpty);
        RuleFor(x => x.Number).GreaterThan(0);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(80);
        RuleFor(x => x.DurationMinutes).InclusiveBetween(1, 1440);
        RuleFor(x => x.Reference).MaximumLength(500);
        RuleFor(x => x.Objective).MaximumLength(2000);
        RuleFor(x => x.Example).MaximumLength(4000);
        RuleFor(x => x.Correction).MaximumLength(8000);
    }
}

public sealed class EvaluateCompetencyCommandValidator : AbstractValidator<EvaluateCompetencyCommand>
{
    public EvaluateCompetencyCommandValidator()
    {
        RuleFor(x => x.EnrollmentId).Must(x => !x.IsEmpty);
        RuleFor(x => x.CompetencyDefinitionId).Must(x => !x.IsEmpty);
        RuleFor(x => x.Level).NotEmpty();
        RuleFor(x => x.Score).InclusiveBetween(0, 100).When(x => x.Score.HasValue);
    }
}

public sealed class UpdateTopicProgressCommandValidator : AbstractValidator<UpdateTopicProgressCommand>
{
    public UpdateTopicProgressCommandValidator()
    {
        RuleFor(x => x.EnrollmentId).Must(x => !x.IsEmpty);
        RuleFor(x => x.TopicId).Must(x => !x.IsEmpty);
        RuleFor(x => x.Status).NotEmpty();
        RuleFor(x => x.PositivePoints).MaximumLength(2000);
        RuleFor(x => x.Improvements).MaximumLength(2000);
        RuleFor(x => x.Comment).MaximumLength(2000);
        RuleFor(x => x.NextObjective).MaximumLength(2000);
        RuleFor(x => x.EvaluationCriteria).Must(x => x is null || x.Count <= 50)
            .WithMessage("TOPIC_EVALUATION_CRITERIA_INVALID");
        RuleForEach(x => x.EvaluationCriteria).ChildRules(c =>
        {
            c.RuleFor(x => x.Code).NotEmpty().MaximumLength(80);
            c.RuleFor(x => x.Level).NotEmpty().MaximumLength(40);
        }).When(x => x.EvaluationCriteria is not null);
    }
}

public sealed class RecordDrivingEvaluationCommandValidator : AbstractValidator<RecordDrivingEvaluationCommand>
{
    public RecordDrivingEvaluationCommandValidator()
    {
        RuleFor(x => x.EnrollmentId).Must(x => !x.IsEmpty);
        RuleFor(x => x.CompetencyDefinitionId).Must(x => !x.IsEmpty);
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Criteria).NotEmpty();
        RuleForEach(x => x.Criteria).ChildRules(c =>
        {
            c.RuleFor(x => x.Code).NotEmpty().MaximumLength(80);
            c.RuleFor(x => x.Level).NotEmpty();
        });
    }
}
