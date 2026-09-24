using FluentAssertions;
using PedagoraPilot.Domain.Catalog.ValueObjects;
using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Organizations.ValueObjects;
using Xunit;

namespace PedagoraPilot.Domain.UnitTests;
public sealed class ValueObjectTests
{
    [Theory]
    [InlineData("aftral", "AFTRAL")]
    [InlineData("ecf-nice", "ECF-NICE")]
    public void OrganizationCode_normalizes(string raw, string expected) => OrganizationCode.Create(raw).Value.Should().Be(expected);
    [Fact]
    public void OrganizationCode_rejects_invalid_format()
    {
        var act = () => OrganizationCode.Create("a");
        act.Should().Throw<DomainException>().Which.ErrorKey.Should().Be("ORGANIZATION_CODE_INVALID");
    }

    [Theory]
    [InlineData("nice_01", "NICE_01")]
    [InlineData("site-a", "SITE-A")]
    public void SiteCode_normalizes(string raw, string expected) => SiteCode.Create(raw).Value.Should().Be(expected);
    [Fact]
    public void SiteCode_requires_value()
    {
        var act = () => SiteCode.Create(" ");
        act.Should().Throw<DomainException>().Which.ErrorKey.Should().Be("SITE_CODE_REQUIRED");
    }

    [Fact]
    public void ProgramCode_normalizes() => ProgramCode.Create(" tp-ecsr ").Value.Should().Be("TP-ECSR");
}
