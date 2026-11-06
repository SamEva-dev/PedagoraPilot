using PedagoraPilot.Application.Training.Cohorts;
using PedagoraPilot.Domain.Training;
using Xunit;

namespace PedagoraPilot.Application.UnitTests;

public sealed class CohortTemporalStatusTests
{
    private static readonly DateOnly Start = new(2026, 2, 1);
    private static readonly DateOnly End = new(2027, 1, 12);

    [Theory]
    [InlineData(2026, 1, 31, CohortStatus.Planned)]
    [InlineData(2026, 2, 1, CohortStatus.Active)]
    [InlineData(2026, 11, 4, CohortStatus.Active)]
    [InlineData(2027, 1, 12, CohortStatus.Active)]
    [InlineData(2027, 1, 13, CohortStatus.Completed)]
    public void Calendar_boundaries_are_inclusive(int year, int month, int day, CohortStatus expected)
        => Assert.Equal(expected, CohortTemporalStatus.FromDates(Start, End, new DateOnly(year, month, day)));

    [Theory]
    [InlineData(CohortStatus.Draft)]
    [InlineData(CohortStatus.Cancelled)]
    public void Administrative_states_are_preserved(CohortStatus status)
        => Assert.Equal(status, CohortTemporalStatus.DisplayStatus(status, Start, End, new DateOnly(2026, 11, 4)));

    [Fact]
    public void An_existing_planned_cohort_in_its_date_window_is_displayed_active()
        => Assert.Equal(CohortStatus.Active, CohortTemporalStatus.DisplayStatus(CohortStatus.Planned, Start, End, new DateOnly(2026, 11, 4)));
}
