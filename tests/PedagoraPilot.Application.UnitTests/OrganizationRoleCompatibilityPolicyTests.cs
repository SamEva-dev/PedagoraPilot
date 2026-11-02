using FluentAssertions;
using PedagoraPilot.Application.Access.Members;
using PedagoraPilot.Security.Contracts;
using Xunit;

namespace PedagoraPilot.Application.UnitTests;

public sealed class OrganizationRoleCompatibilityPolicyTests
{
    [Fact]
    public void Trainer_pedagogical_manager_and_jury_can_coexist() =>
        OrganizationRoleCompatibilityPolicy.IsAllowed(new[]
        {
            PedagoraPilotRoleCodes.Trainer,
            PedagoraPilotRoleCodes.PedagogicalManager,
            PedagoraPilotRoleCodes.Jury
        }).Should().BeTrue();

    [Theory]
    [InlineData("administrator")]
    [InlineData("direction")]
    [InlineData("site_direction")]
    [InlineData("student")]
    [InlineData("secretariat")]
    [InlineData("readonly")]
    public void Exclusive_roles_cannot_be_combined_with_trainer(string alias)
    {
        var exclusive = alias switch
        {
            "administrator" => PedagoraPilotRoleCodes.OrganizationAdministrator,
            "direction" => PedagoraPilotRoleCodes.OrganizationDirection,
            "site_direction" => PedagoraPilotRoleCodes.SiteDirection,
            "student" => PedagoraPilotRoleCodes.Student,
            "secretariat" => PedagoraPilotRoleCodes.Secretariat,
            _ => PedagoraPilotRoleCodes.ReadOnly
        };
        OrganizationRoleCompatibilityPolicy.IsAllowed(new[] { exclusive, PedagoraPilotRoleCodes.Trainer })
            .Should().BeFalse();
    }

    [Fact]
    public void Single_role_and_duplicate_same_role_are_valid()
    {
        OrganizationRoleCompatibilityPolicy.IsAllowed(new[] { PedagoraPilotRoleCodes.Student }).Should().BeTrue();
        OrganizationRoleCompatibilityPolicy.IsAllowed(new[] { PedagoraPilotRoleCodes.Trainer, PedagoraPilotRoleCodes.Trainer })
            .Should().BeTrue();
    }
}
