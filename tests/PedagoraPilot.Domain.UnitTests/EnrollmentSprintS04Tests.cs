using FluentAssertions;
using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Training;
using PedagoraPilot.Domain.Training.Events;
using Xunit;

namespace PedagoraPilot.Domain.UnitTests;

public sealed class EnrollmentSprintS04Tests
{
    private static readonly DateOnly StartingOn = new(2026, 1, 1);

    private static Enrollment Create() => Enrollment.Create(Guid.NewGuid(), LearnerProfileId.New(), CohortId.New(), StartingOn);

    [Fact]
    public void Active_can_be_suspended_and_resumed_without_an_end_date()
    {
        var e = Create();
        e.ChangeStatus(EnrollmentStatus.Suspended);
        e.Status.Should().Be(EnrollmentStatus.Suspended);
        e.EndedOn.Should().BeNull();
        e.ChangeStatus(EnrollmentStatus.Active);
        e.Status.Should().Be(EnrollmentStatus.Active);
    }

    [Theory]
    [InlineData(EnrollmentStatus.Completed)]
    [InlineData(EnrollmentStatus.Withdrawn)]
    [InlineData(EnrollmentStatus.Cancelled)]
    public void Terminal_states_cannot_be_resurrected(EnrollmentStatus finalStatus)
    {
        var e = Create();
        e.ChangeStatus(finalStatus, new DateOnly(2026, 2, 1));
        var action = () => e.ChangeStatus(EnrollmentStatus.Active);
        action.Should().Throw<DomainException>().Which.ErrorKey.Should().Be("ENROLLMENT_STATUS_TRANSITION_FORBIDDEN");
    }

    [Fact]
    public void An_active_enrollment_can_be_cancelled_but_never_resumed()
    {
        var e = Create();
        e.ChangeStatus(EnrollmentStatus.Cancelled, StartingOn.AddDays(2));
        e.Status.Should().Be(EnrollmentStatus.Cancelled);
        e.EndedOn.Should().Be(StartingOn.AddDays(2));
        var action = () => e.ChangeStatus(EnrollmentStatus.Active);
        action.Should().Throw<DomainException>().Which.ErrorKey.Should().Be("ENROLLMENT_STATUS_TRANSITION_FORBIDDEN");
    }

    [Fact]
    public void Cancellation_before_the_planned_start_date_is_allowed()
    {
        var e = Create();
        e.ChangeStatus(EnrollmentStatus.Cancelled, StartingOn.AddDays(-3));
        e.Status.Should().Be(EnrollmentStatus.Cancelled);
        e.EndedOn.Should().Be(StartingOn.AddDays(-3));
    }

    [Fact]
    public void Active_cannot_go_back_to_pending()
    {
        var e = Create();
        var action = () => e.ChangeStatus(EnrollmentStatus.Pending);
        action.Should().Throw<DomainException>().Which.ErrorKey.Should().Be("ENROLLMENT_STATUS_TRANSITION_FORBIDDEN");
    }

    [Fact]
    public void Terminal_end_date_cannot_precede_enrollment()
    {
        var e = Create();
        var action = () => e.ChangeStatus(EnrollmentStatus.Completed, StartingOn.AddDays(-1));
        action.Should().Throw<DomainException>().Which.ErrorKey.Should().Be("ENROLLMENT_END_DATE_BEFORE_START");
        e.Status.Should().Be(EnrollmentStatus.Active);
    }

    [Fact]
    public void Active_or_suspended_enrollment_cannot_receive_an_end_date()
    {
        var e = Create();
        var action = () => e.ChangeStatus(EnrollmentStatus.Suspended, StartingOn.AddDays(1));
        action.Should().Throw<DomainException>().Which.ErrorKey.Should().Be("ENROLLMENT_END_DATE_ONLY_FOR_CLOSURE");
    }

    [Fact]
    public void Duplicate_status_is_idempotent_and_emits_no_extra_event()
    {
        var e = Create();
        var before = e.DomainEvents.Count;
        e.ChangeStatus(EnrollmentStatus.Active);
        e.DomainEvents.Count.Should().Be(before);
    }

    [Fact]
    public void Same_terminal_status_with_different_end_date_is_rejected()
    {
        var e = Create();
        e.ChangeStatus(EnrollmentStatus.Completed, StartingOn.AddDays(7));
        var action = () => e.ChangeStatus(EnrollmentStatus.Completed, StartingOn.AddDays(8));
        action.Should().Throw<DomainException>().Which.ErrorKey.Should().Be("ENROLLMENT_END_DATE_CHANGE_FORBIDDEN");
    }

    [Fact]
    public void Unknown_enum_value_is_rejected()
    {
        var e = Create();
        var action = () => e.ChangeStatus((EnrollmentStatus)987);
        action.Should().Throw<DomainException>().Which.ErrorKey.Should().Be("ENROLLMENT_STATUS_INVALID");
    }

    [Fact]
    public void A_valid_change_emits_one_status_event()
    {
        var e = Create();
        var before = e.DomainEvents.Count;
        e.ChangeStatus(EnrollmentStatus.Suspended);
        e.DomainEvents.Count.Should().Be(before + 1);
        e.DomainEvents.Should().ContainSingle(x => x is EnrollmentStatusChangedDomainEvent);
    }
}
