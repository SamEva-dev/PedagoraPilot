using FluentAssertions;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using Xunit;

namespace PedagoraPilot.Application.UnitTests;

public sealed class TenantScopeTests
{
    private static readonly Guid OrganizationA = Guid.NewGuid();
    private static readonly Guid OrganizationB = Guid.NewGuid();

    [Fact]
    public void Tenant_without_organization_cannot_access_any_resource() =>
        ((Action)(() => TenantScope.Ensure(new User(null), OrganizationA)))
            .Should().Throw<ForbiddenApplicationException>();

    [Fact]
    public void Tenant_cannot_access_other_organization() =>
        ((Action)(() => TenantScope.Ensure(new User(OrganizationA), OrganizationB)))
            .Should().Throw<ForbiddenApplicationException>();

    [Fact]
    public void Tenant_can_access_own_organization() =>
        TenantScope.Organization(new User(OrganizationA)).Should().Be(OrganizationA);

    [Fact]
    public void Product_administrator_without_tenant_is_not_global() =>
        ((Action)(() => TenantScope.Organization(new User(null, "PedagoraPlatformAdministrator"))))
            .Should().Throw<ForbiddenApplicationException>();

    [Fact]
    public void Signed_global_super_admin_can_access_multiple_organizations()
    {
        var user = new User(null, "SuperAdmin");
        TenantScope.Organization(user).Should().BeNull();
        TenantScope.Ensure(user, OrganizationA);
        TenantScope.Ensure(user, OrganizationB);
    }

    private sealed class User(Guid? organizationId, params string[] roles) : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => Guid.NewGuid();
        public Guid? OrganizationId => organizationId;
        public string? Application => "pedagora-pilot-web";
        public string? Email => null;
        public string? DisplayName => null;
        public IReadOnlySet<string> Roles => new HashSet<string>(roles);
        public IReadOnlySet<string> Permissions => new HashSet<string>();
        public bool HasPermission(string permission) => false;
    }
}
