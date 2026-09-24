using FluentAssertions;
using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Training.Delivery;
using Xunit;

namespace PedagoraPilot.Domain.UnitTests;
public sealed class TrainingSessionTests
{
    private static TrainingSession CreateValid(SessionAudienceMode audience = SessionAudienceMode.WholeCohort, IEnumerable<EnrollmentId>? participants = null) => TrainingSession.Create(Guid.NewGuid(), Guid.NewGuid(), CohortId.New(), TrainingSessionType.Classroom, TrainingSessionModality.Onsite, "Code de la route", new DateTimeOffset(2026, 9, 23, 8, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 9, 23, 10, 0, 0, TimeSpan.Zero), "Europe/Paris", audienceMode: audience, participantEnrollmentIds: participants);
    [Fact]
    public void Planned_minutes_are_computed_from_utc_range() => CreateValid().PlannedMinutes.Should().Be(120);
    [Fact]
    public void Selected_audience_requires_at_least_one_enrollment()
    {
        var act = () => CreateValid(SessionAudienceMode.SelectedEnrollments, []);
        act.Should().Throw<DomainException>().Which.ErrorKey.Should().Be("SESSION_PARTICIPANTS_REQUIRED");
    }

    [Fact]
    public void Start_then_complete_changes_status()
    {
        var session = CreateValid();
        session.Start();
        session.Status.Should().Be(TrainingSessionStatus.InProgress);
        session.Complete();
        session.Status.Should().Be(TrainingSessionStatus.Completed);
    }

    [Fact]
    public void Completed_session_cannot_be_cancelled()
    {
        var session = CreateValid();
        session.Complete();
        var act = session.Cancel;
        act.Should().Throw<DomainException>().Which.ErrorKey.Should().Be("SESSION_COMPLETED_LOCKED");
    }
}
