using FluentAssertions;
using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Learning;
using Xunit;

namespace PedagoraPilot.Domain.UnitTests;

public sealed class PedagogicalTopicTests
{
    [Fact]
    public void UpdateCatalog_persists_complete_administration_content()
    {
        var topic = PedagogicalTopic.Create(Guid.NewGuid(), "F-01", 1, "Titre", "rules", 40);

        topic.UpdateCatalog(2, "Nouveau titre", "risk", 55, "R.412-6", false, "Objectif", "Exemple", "Correction");

        topic.Number.Should().Be(2);
        topic.Title.Should().Be("Nouveau titre");
        topic.Category.Should().Be("risk");
        topic.DurationMinutes.Should().Be(55);
        topic.Reference.Should().Be("R.412-6");
        topic.Active.Should().BeFalse();
        topic.Objective.Should().Be("Objectif");
        topic.Example.Should().Be("Exemple");
        topic.Correction.Should().Be("Correction");
        topic.Version.Should().Be(2);
    }

    [Fact]
    public void UpdateCatalog_rejects_invalid_duration()
    {
        var topic = PedagogicalTopic.Create(Guid.NewGuid(), "F-01", 1, "Titre", "rules", 40);
        var action = () => topic.UpdateCatalog(1, "Titre", "rules", 0, null, true, null, null, null);
        action.Should().Throw<DomainException>().Which.ErrorKey.Should().Be("TOPIC_DURATION_INVALID");
    }
}
