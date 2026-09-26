using FluentAssertions;
using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Learning;
using Xunit;

namespace PedagoraPilot.Domain.UnitTests;

public sealed class LearnerTopicProgressTests
{
    [Fact]
    public void Update_preserves_complete_sheet_evaluation()
    {
        var progress = LearnerTopicProgress.Create(Guid.NewGuid(), EnrollmentId.New(), PedagogicalTopicId.New());

        progress.Update(
            TopicProgressStatus.Validated,
            new DateOnly(2026, 9, 20),
            new DateOnly(2026, 9, 26),
            40,
            "Formateur Test",
            "Bonne structure",
            "Renforcer le RIP",
            "Présentation claire",
            "Préparer la fiche suivante",
            [
                ("structure", TopicEvaluationLevel.Acquired),
                ("rip", TopicEvaluationLevel.InProgress),
            ]);

        progress.Status.Should().Be(TopicProgressStatus.Validated);
        progress.PreparationDate.Should().Be(new DateOnly(2026, 9, 20));
        progress.PresentationDate.Should().Be(new DateOnly(2026, 9, 26));
        progress.PresentationDurationMinutes.Should().Be(40);
        progress.EvaluatorDisplayName.Should().Be("Formateur Test");
        progress.PositivePoints.Should().Be("Bonne structure");
        progress.Improvements.Should().Be("Renforcer le RIP");
        progress.Comment.Should().Be("Présentation claire");
        progress.NextObjective.Should().Be("Préparer la fiche suivante");
        progress.EvaluationCriteria.Should().HaveCount(2);
        progress.EvaluationCriteria.Single(x => x.Code == "structure").Level.Should().Be(TopicEvaluationLevel.Acquired);
        progress.EvaluationCriteria.Single(x => x.Code == "rip").Level.Should().Be(TopicEvaluationLevel.InProgress);
        progress.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public void Update_replaces_previous_criteria_instead_of_accumulating_them()
    {
        var progress = LearnerTopicProgress.Create(Guid.NewGuid(), EnrollmentId.New(), PedagogicalTopicId.New());
        progress.Update(TopicProgressStatus.InProgress, null, null, null, null, null, null, null, null,
            [("structure", TopicEvaluationLevel.InProgress)]);

        progress.Update(TopicProgressStatus.Rework, null, null, null, null, null, null, null, null,
            [("rip", TopicEvaluationLevel.Review)]);

        progress.EvaluationCriteria.Should().ContainSingle();
        progress.EvaluationCriteria.Single().Code.Should().Be("rip");
        progress.EvaluationCriteria.Single().Level.Should().Be(TopicEvaluationLevel.Review);
    }

    [Fact]
    public void Update_rejects_duplicate_criterion_codes_case_insensitively()
    {
        var progress = LearnerTopicProgress.Create(Guid.NewGuid(), EnrollmentId.New(), PedagogicalTopicId.New());

        var action = () => progress.Update(
            TopicProgressStatus.Validated,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            [
                ("structure", TopicEvaluationLevel.Acquired),
                ("STRUCTURE", TopicEvaluationLevel.InProgress),
            ]);

        action.Should().Throw<DomainException>()
            .Which.ErrorKey.Should().Be("TOPIC_EVALUATION_CRITERIA_INVALID");
    }
}
