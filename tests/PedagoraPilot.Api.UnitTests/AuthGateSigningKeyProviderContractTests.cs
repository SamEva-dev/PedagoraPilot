using FluentAssertions;
using PedagoraPilot.Api.Authorization;
using Xunit;

namespace PedagoraPilot.Api.UnitTests;

public sealed class AuthGateSigningKeyProviderContractTests
{
    [Fact]
    public void JwtBearer_configuration_must_not_use_InstancePropertyBag()
    {
        var source = File.ReadAllText(
            Path.Combine(
                FindRepositoryRoot(),
                "src",
                "PedagoraPilot.Api",
                "Extensions",
                "AuthenticationExtensions.cs"));

        source.Should().NotContain("InstancePropertyBag");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "src")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Unable to locate PedagoraPilot repository root.");
    }
}
