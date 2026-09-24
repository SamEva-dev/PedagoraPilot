using FluentValidation;
using PedagoraPilot.Application.Abstractions.Messaging;
using PedagoraPilot.Contracts.Learning;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Application.Learning.Progress;
public sealed record EvaluateCompetencyCommand(EnrollmentId EnrollmentId, CompetencyDefinitionId CompetencyDefinitionId, string Level, decimal? Score, string? Comment, Guid? EvaluatorAuthGateUserId, string? EvaluatorDisplayName, DateTimeOffset? EvaluatedAtUtc) : ICommand<LearnerCompetencyDto>;
public sealed record UpdateTopicProgressCommand(EnrollmentId EnrollmentId, PedagogicalTopicId TopicId, string Status, DateOnly? PreparationDate, DateOnly? PresentationDate, int? PresentationDurationMinutes, string? EvaluatorDisplayName, string? Comment) : ICommand<LearnerTopicProgressDto>;
public sealed record RecordDrivingEvaluationCommand(EnrollmentId EnrollmentId, CompetencyDefinitionId CompetencyDefinitionId, TrainingSessionId? TrainingSessionId, DateTimeOffset EvaluatedAtUtc, string? TrainerAuthGateUserId, string TrainerDisplayName, string Subject, string? Positive, string? Difficulty, string? NextGoal, string? FreeObservation, IReadOnlyCollection<DrivingCriterionRequest> Criteria) : ICommand<DrivingEvaluationDto>;
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
    }
}

public sealed class RecordDrivingEvaluationCommandValidator : AbstractValidator<RecordDrivingEvaluationCommand>
{
    public RecordDrivingEvaluationCommandValidator()
    {
        RuleFor(x => x.EnrollmentId).Must(x => !x.IsEmpty);
        RuleFor(x => x.CompetencyDefinitionId).Must(x => !x.IsEmpty);
        RuleFor(x => x.TrainerDisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Criteria).NotEmpty();
        RuleForEach(x => x.Criteria).ChildRules(c =>
        {
            c.RuleFor(x => x.Code).NotEmpty().MaximumLength(80);
            c.RuleFor(x => x.Label).NotEmpty().MaximumLength(300);
            c.RuleFor(x => x.Level).NotEmpty();
        });
    }
}
