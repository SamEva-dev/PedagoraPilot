using FluentAssertions;
using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Training;
using Xunit;

namespace PedagoraPilot.Domain.UnitTests;
public sealed class CohortTests
{
    private static Cohort CreateValid() => Cohort.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "ECSR-2026", "TP ECSR 2026", new DateOnly(2026, 9, 1), new DateOnly(2027, 2, 28), 20);
    [Fact]
    public void Create_raises_domain_event_and_assigns_strong_identifier()
    {
        var cohort = CreateValid();
        cohort.Id.IsEmpty.Should().BeFalse();
        cohort.DomainEvents.Should().ContainSingle();
        cohort.Status.Should().Be(CohortStatus.Planned);
    }

    [Fact]
    public void Create_rejects_invalid_date_range()
    {
        var act = () => Cohort.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "ECSR", "ECSR", new DateOnly(2026, 9, 2), new DateOnly(2026, 9, 1), 20);
        act.Should().Throw<DomainException>().Which.ErrorKey.Should().Be("COHORT_DATE_RANGE_INVALID");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(501)]
    public void Create_rejects_invalid_capacity(int capacity)
    {
        var act = () => Cohort.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "ECSR", "ECSR", new DateOnly(2026, 9, 1), new DateOnly(2027, 1, 1), capacity);
        act.Should().Throw<DomainException>().Which.ErrorKey.Should().Be("COHORT_CAPACITY_INVALID");
    }

    [Fact]
    public void Completed_cohort_is_locked()
    {
        var cohort = CreateValid();
        cohort.Update("TP ECSR 2026", cohort.StartDate, cohort.EndDate, 20, CohortStatus.Completed);
        var act = () => cohort.Update("Changed", cohort.StartDate, cohort.EndDate, 20, CohortStatus.Active);
        act.Should().Throw<DomainException>().Which.ErrorKey.Should().Be("COHORT_CLOSED");
    }
}
