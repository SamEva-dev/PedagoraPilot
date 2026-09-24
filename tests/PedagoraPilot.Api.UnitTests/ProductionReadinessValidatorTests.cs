using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using PedagoraPilot.Api.Readiness;
using Xunit;

namespace PedagoraPilot.Api.UnitTests;
public sealed class ProductionReadinessValidatorTests
{
    [Fact]
    public void Development_never_fails_production_constraints()
    {
        var validator = new ProductionReadinessValidator(new ConfigurationBuilder().Build(), new TestHostEnvironment(Environments.Development));
        validator.Validate(null, new ProductionReadinessOptions()).Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Production_rejects_demo_filesystem_noop_scanner_http_authgate_and_wildcard_cors()
    {
        var settings = new Dictionary<string, string?>
        {
            ["AuthGate:BaseUrl"] = "http://authgate.local",
            ["Storage:Provider"] = "FileSystem",
            ["Storage:SecurityScanner:Provider"] = "NoOp",
            ["Demo:Enabled"] = "true",
            ["Brevo:EnableSending"] = "false",
            ["Cors:AllowedOrigins:0"] = "*"
        };
        var validator = new ProductionReadinessValidator(new ConfigurationBuilder().AddInMemoryCollection(settings).Build(), new TestHostEnvironment(Environments.Production));
        var result = validator.Validate(null, new ProductionReadinessOptions());
        result.Failed.Should().BeTrue();
        result.Failures.Should().HaveCountGreaterThanOrEqualTo(6);
    }

    [Fact]
    public void Production_accepts_hardened_configuration()
    {
        var settings = new Dictionary<string, string?>
        {
            ["AuthGate:BaseUrl"] = "https://auth.example.com",
            ["Storage:Provider"] = "S3",
            ["Storage:SecurityScanner:Provider"] = "ClamAV",
            ["Demo:Enabled"] = "false",
            ["Brevo:EnableSending"] = "true",
            ["Cors:AllowedOrigins:0"] = "https://pedagora.example.com"
        };
        var validator = new ProductionReadinessValidator(new ConfigurationBuilder().AddInMemoryCollection(settings).Build(), new TestHostEnvironment(Environments.Production));
        validator.Validate(null, new ProductionReadinessOptions()).Succeeded.Should().BeTrue();
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "PedagoraPilot.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
