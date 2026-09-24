using FluentAssertions;
using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Workforce;
using Xunit;

namespace PedagoraPilot.Domain.UnitTests;
public sealed class RemoteWorkTests
{
    [Fact]
    public void Custom_period_requires_a_valid_time_range()
    {
        var act = () => RemoteWorkRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Trainer", "trainer@example.com", new DateOnly(2026, 9, 23), RemoteWorkPeriod.Custom, new TimeOnly(14, 0), new TimeOnly(13, 0), null, []);
        act.Should().Throw<DomainException>().Which.ErrorKey.Should().Be("REMOTE_WORK_TIME_RANGE_INVALID");
    }

    [Fact]
    public void Decision_can_only_be_recorded_once()
    {
        var request = RemoteWorkRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Trainer", "trainer@example.com", new DateOnly(2026, 9, 23), RemoteWorkPeriod.FullDay, null, null, null, []);
        request.Approve(Guid.NewGuid(), "Manager");
        var act = () => request.Reject(Guid.NewGuid(), "Other manager");
        act.Should().Throw<DomainException>().Which.ErrorKey.Should().Be("REMOTE_WORK_REQUEST_ALREADY_DECIDED");
    }
}
