using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using PedagoraPilot.Api.Authorization;
using Xunit;

namespace PedagoraPilot.Api.UnitTests;

public sealed class HttpCurrentUserTests
{
    [Theory]
    [InlineData("SuperAdmin")]
    [InlineData("PlatformAdmin")]
    [InlineData("PlatformAdministrator")]
    public void Exact_global_role_aliases_are_platform_administrators(string role)
    {
        var current = Create(new Claim(ClaimTypes.Role, role));
        current.IsPlatformAdministrator.Should().BeTrue();
    }

    [Fact]
    public void Product_specific_platform_role_is_not_global()
    {
        var current = Create(new Claim(ClaimTypes.Role, "PedagoraPlatformAdministrator"));
        current.IsPlatformAdministrator.Should().BeFalse();
    }

    [Fact]
    public void AuthGate_platform_admin_claim_is_global()
    {
        var current = Create(new Claim(AuthGateClaimNames.PlatformAdmin, "true"));
        current.IsPlatformAdministrator.Should().BeTrue();
    }


    [Fact]
    public void Cohort_scope_can_view_parent_context_but_not_manage_parent()
    {
        var siteId = Guid.NewGuid();
        var programId = Guid.NewGuid();
        var cohortId = Guid.NewGuid();
        var current = Create(new Claim(AuthGateClaimNames.Permissions,
            $"pedagora.scope.cohort:{siteId:D}:{programId:D}:{cohortId:D}"));

        current.HasContextualScopeRestrictions.Should().BeTrue();
        current.CanViewSite(siteId).Should().BeTrue();
        current.CanManageSite(siteId).Should().BeFalse();
        current.CanViewProgram(siteId, programId).Should().BeTrue();
        current.CanManageProgram(siteId, programId).Should().BeFalse();
        current.CanViewCohort(siteId, programId, cohortId).Should().BeTrue();
        current.CanManageCohort(siteId, programId, cohortId).Should().BeTrue();
        current.CanViewCohort(siteId, programId, Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void Exam_scope_can_only_manage_its_exam_not_the_cohort()
    {
        var siteId = Guid.NewGuid();
        var programId = Guid.NewGuid();
        var cohortId = Guid.NewGuid();
        var examId = Guid.NewGuid();
        var current = Create(new Claim(AuthGateClaimNames.Permissions,
            $"pedagora.scope.exam:{siteId:D}:{programId:D}:{cohortId:D}:{examId:D}"));

        current.CanViewCohort(siteId, programId, cohortId).Should().BeTrue();
        current.CanManageCohort(siteId, programId, cohortId).Should().BeFalse();
        current.CanViewExam(siteId, programId, cohortId, examId).Should().BeTrue();
        current.CanManageExam(siteId, programId, cohortId, examId).Should().BeTrue();
        current.CanViewExam(siteId, programId, cohortId, Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void Organization_scope_keeps_organization_wide_access()
    {
        var current = Create(new Claim(AuthGateClaimNames.Permissions, "pedagora.scope.organization"));

        current.HasContextualScopeRestrictions.Should().BeFalse();
        current.CanViewSite(Guid.NewGuid()).Should().BeTrue();
        current.CanManageSite(Guid.NewGuid()).Should().BeTrue();
    }

    [Fact]
    public void No_scope_is_backward_compatible_and_not_restricted()
    {
        var current = Create(new Claim(AuthGateClaimNames.Permissions, "training.sessions.read"));

        current.HasContextualScopeRestrictions.Should().BeFalse();
        current.CanViewCohort(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).Should().BeTrue();
    }

    private static HttpCurrentUser Create(params Claim[] claims)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
        };
        return new HttpCurrentUser(new HttpContextAccessor { HttpContext = context });
    }
}
