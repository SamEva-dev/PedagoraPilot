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

    [Theory]
    [InlineData("SuperAdmin")]
    [InlineData("PlatformAdmin")]
    [InlineData("PlatformAdministrator")]
    public void Signed_global_platform_administrator_can_access_multiple_organizations(string role)
    {
        var user = new User(null, role);
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
        public IReadOnlyCollection<ContextualScopeAssignment> ContextualScopes => Array.Empty<ContextualScopeAssignment>();
        public bool HasContextualScopeRestrictions => false;
        public bool IsPlatformAdministrator => Roles.Any(role => role.Equals("PlatformAdministrator", StringComparison.OrdinalIgnoreCase) || role.Equals("PlatformAdmin", StringComparison.OrdinalIgnoreCase) || role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase));
        public bool HasPermission(string permission) => IsPlatformAdministrator;
        public bool CanViewSite(Guid siteId) => true;
        public bool CanManageSite(Guid siteId) => true;
        public bool CanViewProgram(Guid siteId, Guid programId) => true;
        public bool CanManageProgram(Guid siteId, Guid programId) => true;
        public bool CanViewCohort(Guid siteId, Guid programId, Guid cohortId) => true;
        public bool CanManageCohort(Guid siteId, Guid programId, Guid cohortId) => true;
        public bool CanViewExam(Guid siteId, Guid programId, Guid cohortId, Guid examSessionId) => true;
        public bool CanManageExam(Guid siteId, Guid programId, Guid cohortId, Guid examSessionId) => true;
    }
}
