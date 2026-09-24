using DomainRelay.EFCore.DomainEvents;
using FluentAssertions;
using PedagoraPilot.Domain.Common;
using Xunit;

namespace PedagoraPilot.Domain.UnitTests;
public sealed class DomainEventContractTests
{
    [Fact]
    public void Every_concrete_domain_event_must_use_the_common_DomainEvent_base()
    {
        var domainAssembly = typeof(DomainEvent).Assembly;
        var eventTypes = domainAssembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract).Where(t => typeof(IDomainEvent).IsAssignableFrom(t)).ToArray();
        eventTypes.Should().NotBeEmpty();
        foreach (var type in eventTypes)
        {
            type.Should().BeAssignableTo<DomainEvent>($"{type.FullName} must inherit DomainEvent so EventId and OccurredOnUtc are always initialized");
        }
    }
}
