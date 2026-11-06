using FluentAssertions;
using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Workplace;
using PedagoraPilot.Domain.Identifiers;
using Xunit;
namespace PedagoraPilot.Domain.UnitTests;
public sealed class WorkplacePeriodLifecycleTests
{
    private static WorkplacePeriod NewPeriod() => WorkplacePeriod.Create(Guid.NewGuid(), new EnrollmentId(Guid.NewGuid()),
        new CohortId(Guid.NewGuid()), Guid.NewGuid(), "STAGE", "Entreprise", "Nice", "Tuteur", null, null,
        new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 1), 120, false, null);

    [Fact] public void Planned_placement_can_be_soft_deleted_but_not_edited_afterwards()
    {
        var period = NewPeriod(); period.DeleteUnused("Created in error", Guid.NewGuid());
        period.Status.Should().Be(WorkplacePeriodStatus.Deleted);
        Action update = () => period.RecordCompletedMinutes(60);
        update.Should().Throw<DomainException>().WithMessage("WORKPLACE_PERIOD_LOCKED");
    }

    [Fact] public void Cancellation_locks_further_work_and_preserves_record()
    {
        var period = NewPeriod(); period.Cancel("No host company", Guid.NewGuid());
        period.Status.Should().Be(WorkplacePeriodStatus.Cancelled);
        period.Notes.Should().Contain("No host company");
        Action update = () => period.RecordCompletedMinutes(60);
        update.Should().Throw<DomainException>();
    }

    [Fact] public void Completed_placement_can_only_be_archived()
    {
        var period = NewPeriod(); period.RecordCompletedMinutes(120);
        period.Status.Should().Be(WorkplacePeriodStatus.Completed);
        Action cancel = () => period.Cancel("Wrong action", Guid.NewGuid());
        cancel.Should().Throw<DomainException>().WithMessage("WORKPLACE_LIFECYCLE_INVALID");
        period.Archive(Guid.NewGuid());
        period.Status.Should().Be(WorkplacePeriodStatus.Archived);
        Action evaluate = () => period.AddEvaluation(WorkplaceEvaluationKind.Tutor, "Tuteur", DateTimeOffset.UtcNow, "OK", null, null, true);
        evaluate.Should().Throw<DomainException>().WithMessage("WORKPLACE_PERIOD_LOCKED");
    }

    [Fact] public void A_period_with_hours_cannot_be_deleted()
    {
        var period = NewPeriod(); period.RecordCompletedMinutes(30);
        Action deletion = () => period.DeleteUnused("Remove", Guid.NewGuid());
        deletion.Should().Throw<DomainException>().WithMessage("WORKPLACE_DELETE_HAS_EVIDENCE");
    }

    [Fact] public void Reason_is_mandatory()
    {
        var period = NewPeriod(); Action action = () => period.Cancel(" ", Guid.NewGuid());
        action.Should().Throw<DomainException>().WithMessage("WORKPLACE_LIFECYCLE_REASON_INVALID");
    }
}
