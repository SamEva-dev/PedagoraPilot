using PedagoraPilot.Application.Training.Delivery;
using Xunit;

namespace PedagoraPilot.Application.UnitTests;

public sealed class TrainingSessionScheduleGuardTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 17, 8, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(0, 60, 30, 90, true)]
    [InlineData(0, 60, 0, 60, true)]
    [InlineData(0, 60, 60, 120, false)]
    [InlineData(60, 120, 0, 60, false)]
    [InlineData(0, 120, 30, 60, true)]
    [InlineData(30, 60, 0, 120, true)]
    public void Intervals_are_half_open(int existingStart, int existingEnd, int proposedStart, int proposedEnd, bool expected)
    {
        Assert.Equal(expected, TrainingSessionScheduleGuard.Overlaps(
            Start.AddMinutes(existingStart), Start.AddMinutes(existingEnd),
            Start.AddMinutes(proposedStart), Start.AddMinutes(proposedEnd)));
    }

    [Fact]
    public void Timezone_offsets_are_compared_as_instants()
    {
        Assert.True(TrainingSessionScheduleGuard.Overlaps(
            new DateTimeOffset(2026, 11, 17, 9, 0, 0, TimeSpan.FromHours(1)),
            new DateTimeOffset(2026, 11, 17, 10, 0, 0, TimeSpan.FromHours(1)),
            Start.AddMinutes(30), Start.AddMinutes(90)));
    }
}
