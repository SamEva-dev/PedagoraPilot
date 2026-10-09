using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using PedagoraPilot.Api.Readiness;
using PedagoraPilot.Infrastructure.Storage;
using Xunit;

namespace PedagoraPilot.Api.UnitTests;

public sealed class ProductionDependencyGuardSprintS09Tests
{
    [Fact]
    public void Production_rejects_real_filesystem_registration_even_when_configuration_claims_s3()
    {
        var validate = () => ProductionDependencyGuard.Validate(
            new TestEnvironment(Environments.Production), new ProductionReadinessOptions(),
            typeof(FileSystemObjectStorage), typeof(object));
        validate.Should().Throw<InvalidOperationException>().WithMessage("*FileSystemObjectStorage*");
    }

    [Fact]
    public void Production_rejects_real_noop_scanner_registration_even_when_configuration_claims_clamav()
    {
        var validate = () => ProductionDependencyGuard.Validate(
            new TestEnvironment(Environments.Production), new ProductionReadinessOptions(),
            typeof(object), typeof(NoOpFileSecurityScanner));
        validate.Should().Throw<InvalidOperationException>().WithMessage("*NoOpFileSecurityScanner*");
    }

    [Fact]
    public void Development_keeps_local_implementations_available()
    {
        var validate = () => ProductionDependencyGuard.Validate(
            new TestEnvironment(Environments.Development), new ProductionReadinessOptions(),
            typeof(FileSystemObjectStorage), typeof(NoOpFileSecurityScanner));
        validate.Should().NotThrow();
    }

    [Fact]
    public void Production_allows_independent_implementations_when_both_are_registered()
    {
        // typeof(object) represents a non-legacy implementation for this pure type guard test.
        // This test does not claim a production scanner/storage adapter exists in the solution.
        var validate = () => ProductionDependencyGuard.Validate(
            new TestEnvironment(Environments.Production), new ProductionReadinessOptions(),
            typeof(object), typeof(object));
        validate.Should().NotThrow();
    }

    private sealed class TestEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "PedagoraPilot.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
