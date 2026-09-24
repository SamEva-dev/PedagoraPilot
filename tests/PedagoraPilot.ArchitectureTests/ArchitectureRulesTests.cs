using FluentAssertions;
using NetArchTest.Rules;
using PedagoraPilot.Application;
using PedagoraPilot.Domain.Common;
using PedagoraPilot.Infrastructure.Persistence;
using Xunit;

namespace PedagoraPilot.ArchitectureTests;
public sealed class ArchitectureRulesTests
{
    [Fact]
    public void Domain_must_not_depend_on_application_infrastructure_api_or_contracts()
    {
        var result = Types.InAssembly(typeof(AggregateRoot).Assembly).ShouldNot().HaveDependencyOnAny("PedagoraPilot.Application", "PedagoraPilot.Infrastructure", "PedagoraPilot.Api", "PedagoraPilot.Contracts").GetResult();
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Application_must_not_depend_on_infrastructure_or_api()
    {
        var result = Types.InAssembly(typeof(DependencyInjection).Assembly).ShouldNot().HaveDependencyOnAny("PedagoraPilot.Infrastructure", "PedagoraPilot.Api").GetResult();
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Infrastructure_must_not_depend_on_api()
    {
        var result = Types.InAssembly(typeof(PedagoraPilotDbContext).Assembly).ShouldNot().HaveDependencyOn("PedagoraPilot.Api").GetResult();
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Contracts_must_not_depend_on_application_or_infrastructure()
    {
        var result = Types.InAssembly(typeof(PedagoraPilot.Contracts.Common.ApiErrorResponse).Assembly).ShouldNot().HaveDependencyOnAny("PedagoraPilot.Application", "PedagoraPilot.Infrastructure", "PedagoraPilot.Api").GetResult();
        result.IsSuccessful.Should().BeTrue();
    }
}
