using FluentAssertions;
using PedagoraPilot.Domain.Training;
using Xunit;

namespace PedagoraPilot.Domain.UnitTests;
public sealed class DomainEventLifecycleTests
{
    [Fact]
    public void PullDomainEvents_returns_then_clears_events()
    {
        var cohort = Cohort.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "ECSR-2026", "TP ECSR", new DateOnly(2026, 9, 1), new DateOnly(2027, 2, 28), 20);
        cohort.DomainEvents.Should().ContainSingle();
        var pulled = cohort.PullDomainEvents();
        pulled.Should().ContainSingle();
        cohort.DomainEvents.Should().BeEmpty();
    }
}
