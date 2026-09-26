using FluentAssertions;
using NetArchTest.Rules;
using PedagoraPilot.Application;
using PedagoraPilot.Application.Abstractions.Messaging;
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
    [Fact]
    public void Application_commands_must_be_transactional_commands()
    {
        var commandTypes = typeof(DependencyInjection).Assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false } && type.Name.EndsWith("Command", StringComparison.Ordinal));

        var offenders = commandTypes
            .Where(type => !type.GetInterfaces().Any(@interface =>
                @interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(ICommand<>)))
            .Select(type => type.FullName)
            .OrderBy(name => name)
            .ToArray();

        offenders.Should().BeEmpty("every application mutation command must pass through UnitOfWorkBehavior");
    }

}
