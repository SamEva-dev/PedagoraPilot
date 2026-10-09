using FluentAssertions;
using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Training;
using Xunit;

namespace PedagoraPilot.Domain.UnitTests;

public sealed class CohortSprintS03Tests
{
    private static Cohort Create(CohortStatus status = CohortStatus.Planned) => Cohort.Create(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        "ECSR-S03", "Promotion S03", new DateOnly(2026, 10, 1),
        new DateOnly(2027, 3, 1), 20, Guid.NewGuid(), "Premier responsable", initialStatus: status);

    [Theory]
    [InlineData(CohortStatus.Planned)]
    [InlineData(CohortStatus.Draft)]
    [InlineData(CohortStatus.Active)]
    public void Create_persists_authorized_initial_status_in_same_aggregate(CohortStatus status)
        => Create(status).Status.Should().Be(status);

    [Theory]
    [InlineData(CohortStatus.Completed)]
    [InlineData(CohortStatus.Cancelled)]
    public void Create_rejects_closed_initial_status(CohortStatus status)
    {
        var action = () => Create(status);
        action.Should().Throw<DomainException>().Which.ErrorKey.Should().Be("COHORT_STATUS_INVALID");
    }

    [Fact]
    public void Reassignment_changes_cohort_manager_without_mutating_program()
    {
        var cohort = Create();
        var original = cohort.PedagogicalManagerAuthGateUserId.HasValue ? cohort.PedagogicalManagerAuthGateUserId.Value : Guid.Empty;
        var secondManager = Guid.NewGuid();
        cohort.ReassignPedagogicalManager(secondManager, "Deuxième responsable");
        cohort.PedagogicalManagerAuthGateUserId.Should().Be(secondManager);
        cohort.PedagogicalManagerDisplayName.Should().Be("Deuxième responsable");
        cohort.PedagogicalManagerAuthGateUserId.Should().NotBe(original);
        cohort.Status.Should().Be(CohortStatus.Planned);
    }

    [Fact]
    public void Reassignment_is_idempotent_for_identical_manager()
    {
        var cohort = Create();
        var manager = cohort.PedagogicalManagerAuthGateUserId!.Value;
        var before = cohort.DomainEvents.Count;
        cohort.ReassignPedagogicalManager(manager, "Premier responsable");
        cohort.DomainEvents.Count.Should().Be(before);
    }

    [Theory]
    [InlineData(CohortStatus.Completed)]
    [InlineData(CohortStatus.Cancelled)]
    public void Reassignment_rejects_closed_cohorts(CohortStatus terminal)
    {
        var cohort = Create();
        cohort.Update(cohort.Name, cohort.StartDate, cohort.EndDate, cohort.Capacity, terminal);
        var action = () => cohort.ReassignPedagogicalManager(Guid.NewGuid(), "Nouveau responsable");
        action.Should().Throw<DomainException>().Which.ErrorKey.Should().Be("COHORT_CLOSED");
    }

    [Fact]
    public void Reassignment_rejects_empty_user_id()
    {
        var action = () => Create().ReassignPedagogicalManager(Guid.Empty, "Nom");
        action.Should().Throw<DomainException>().Which.ErrorKey.Should().Be("COHORT_PEDAGOGICAL_MANAGER_REQUIRED");
    }
}
