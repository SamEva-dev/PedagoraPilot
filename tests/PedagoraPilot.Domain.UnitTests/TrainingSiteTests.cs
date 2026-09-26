using FluentAssertions;
using PedagoraPilot.Domain.Organizations;
using Xunit;

namespace PedagoraPilot.Domain.UnitTests;

public sealed class TrainingSiteTests
{
    [Fact]
    public void Create_preserves_profile_fields_without_nulls()
    {
        var site = TrainingSite.Create(Guid.NewGuid(), "NCE", "Nice", "Nice", null,
            "14 rue Test", "06000", "+33400000000", "nice@example.test", "Sam",
            TrainingSiteStatus.Attention);

        site.Code.Value.Should().Be("NCE");
        site.Address.Should().Be("14 rue Test");
        site.PostalCode.Should().Be("06000");
        site.Phone.Should().Be("+33400000000");
        site.Email.Should().Be("nice@example.test");
        site.Manager.Should().Be("Sam");
        site.Status.Should().Be(TrainingSiteStatus.Attention);
        site.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Update_can_change_code_profile_and_operational_status()
    {
        var site = TrainingSite.Create(Guid.NewGuid(), "NCE", "Nice", "Nice");

        site.Update("NCE2", "Nice Centre", "Nice", "1 avenue Test", "06000", "", "", "Manager", TrainingSiteStatus.Inactive);

        site.Code.Value.Should().Be("NCE2");
        site.Name.Should().Be("Nice Centre");
        site.Address.Should().Be("1 avenue Test");
        site.Phone.Should().BeEmpty();
        site.Email.Should().BeEmpty();
        site.Status.Should().Be(TrainingSiteStatus.Inactive);
        site.IsActive.Should().BeFalse();
    }
}
