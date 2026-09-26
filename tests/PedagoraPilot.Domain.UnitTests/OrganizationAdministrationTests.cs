using FluentAssertions;
using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Organizations;
using PedagoraPilot.Domain.Organizations.Events;
using Xunit;

namespace PedagoraPilot.Domain.UnitTests;

public sealed class OrganizationAdministrationTests
{
    [Fact]
    public void UpdateAdministration_normalizes_values_and_raises_domain_event()
    {
        var organization = Organization.Provision(
            Guid.NewGuid(),
            "AFPA-NICE",
            "AFPA Nice",
            "FR",
            "OWNER@EXAMPLE.COM",
            null);
        organization.PullDomainEvents();
        var versionBefore = organization.Version;

        organization.UpdateAdministration(
            " AFPA Nice Côte d'Azur ",
            " AFPA Nice ",
            " 12345678901234 ",
            " NDA-001 ",
            " 1 rue Test ",
            " 06000 ",
            " Nice ",
            "fr",
            " CONTACT@EXAMPLE.COM ",
            " +33400000000 ",
            " https://example.test ",
            " Direction ",
            "#abcdef",
            "#123456",
            " Formation professionnelle ",
            "",
            true,
            false,
            ["planning", "documents", "planning"],
            true,
            false,
            true,
            false,
            true,
            "FR",
            "Europe/Paris",
            "DD/MM/YYYY",
            "2026-2027",
            true,
            true,
            3,
            true,
            false);

        organization.LegalName.Should().Be("AFPA Nice Côte d'Azur");
        organization.ShortName.Should().Be("AFPA Nice");
        organization.OwnerEmail.Should().Be("contact@example.com");
        organization.CountryCode.Should().Be("FR");
        organization.PrimaryColor.Should().Be("#ABCDEF");
        organization.Language.Should().Be("fr");
        organization.EnabledModules.Should().BeEquivalentTo(["documents", "planning"]);
        organization.RemoteWorkMaxDaysPerWeek.Should().Be(3);
        organization.Version.Should().Be(versionBefore + 1);
        organization.UpdatedAtUtc.Should().BeOnOrAfter(organization.CreatedAtUtc);
        organization.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<OrganizationAdministrationUpdatedDomainEvent>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(8)]
    public void UpdateAdministration_rejects_invalid_remote_work_max_days(int maxDays)
    {
        var organization = Organization.Provision(Guid.NewGuid(), "AFPA-NICE", "AFPA Nice", "FR", "owner@example.com", null);

        var action = () => organization.UpdateAdministration(
            "AFPA Nice", "AFPA Nice", "", "", "", "", "", "FR", "owner@example.com", "", "", "",
            "#1456A0", "#F59E0B", "", "AFPA Nice", false, true, [], true, true, true, false, true,
            "fr", "Europe/Paris", "DD/MM/YYYY", "2026-2027", true, true, maxDays, true, true);

        action.Should().Throw<DomainException>().Which.ErrorKey.Should().Be("REMOTE_WORK_MAX_DAYS_INVALID");
    }
}
