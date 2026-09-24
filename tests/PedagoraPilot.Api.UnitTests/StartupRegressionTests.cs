using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PedagoraPilot.Api.Extensions;
using PedagoraPilot.Infrastructure;
using PedagoraPilot.Infrastructure.Persistence;
using Xunit;

namespace PedagoraPilot.Api.UnitTests;
public sealed class StartupRegressionTests
{
    [Fact]
    public async Task Anonymous_request_does_not_require_AuthGate_signing_keys()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["AuthGate:BaseUrl"] = "http://localhost:8081", ["AuthGate:Issuer"] = "test-issuer", ["AuthGate:Audience"] = "test-audience" }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPedagoraAuthentication(configuration);
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);
        var context = new MessageReceivedContext(new DefaultHttpContext(), new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler)), options);
        await options.Events.MessageReceived(context);
        context.Token.Should().BeNullOrEmpty();
    }

    [Fact]
    public void Outbox_context_factory_resolves_from_root_and_builds_the_model()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = "Host=localhost;Database=registration_test;Username=test;Password=test" }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var factory = provider.GetRequiredService<IDbContextFactory<PedagoraPilotDbContext>>();
        using var context = factory.CreateDbContext();
        context.Model.GetEntityTypes().Should().NotBeEmpty();
    }
}
