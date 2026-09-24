using FluentAssertions;
using System.Reflection;
using DomainRelay.Mapping.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;
using PedagoraPilot.Application;
using Xunit;

namespace PedagoraPilot.Application.UnitTests;
public sealed class ApplicationRegistrationTests
{
    [Fact]
    public void AddApplication_builds_service_provider_and_validates_DomainRelay_mapping()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        // Supply the infrastructure ports that the application layer intentionally leaves to its host.
        foreach (var port in typeof(DependencyInjection).Assembly.GetTypes().Where(type => type.IsInterface && !type.ContainsGenericParameters && type.Namespace?.StartsWith("PedagoraPilot.Application.Abstractions", StringComparison.Ordinal) == true))
        {
            services.AddScoped(port, _ => DispatchProxy.Create(port, typeof(UnusedPortProxy)));
        }

        var act = () => services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var provider = act.Should().NotThrow().Subject;
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IObjectMapper>().Should().NotBeNull();
    }

    public class UnusedPortProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object? []? args) => throw new InvalidOperationException("Infrastructure should not be invoked during registration validation.");
    }
}
