using FluentAssertions;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using Xunit;

namespace PedagoraPilot.Application.UnitTests;

/// <summary>
/// Unit-level regression of tenant/context guards. HTTP authorization and real DB queries
/// are separate integration gates and are not claimed by this suite.
/// </summary>
public sealed class TenantAndCertificationScopeRegressionTests
{
    [Fact]
    public void Cross_tenant_access_is_rejected()
    {
        var user = new ScopedUser(Guid.NewGuid());
        Action action = () => TenantScope.Ensure(user, Guid.NewGuid());
        action.Should().Throw<ForbiddenApplicationException>();
    }

    [Fact]
    public void Same_tenant_access_is_allowed()
    {
        var tenant = Guid.NewGuid();
        Action action = () => TenantScope.Ensure(new ScopedUser(tenant), tenant);
        action.Should().NotThrow();
    }

    [Fact]
    public void Unauthenticated_and_missing_tenant_users_are_rejected()
    {
        Action unauthenticated = () => TenantScope.Ensure(new ScopedUser(Guid.NewGuid(), authenticated: false), Guid.NewGuid());
        Action missingTenant = () => TenantScope.Ensure(new ScopedUser(null), Guid.NewGuid());
        unauthenticated.Should().Throw<ForbiddenApplicationException>();
        missingTenant.Should().Throw<ForbiddenApplicationException>();
    }

    [Fact]
    public void Platform_administrator_bypass_is_explicit_and_requires_authentication()
    {
        Action allowed = () => TenantScope.Ensure(new ScopedUser(null, platformAdmin: true), Guid.NewGuid());
        Action denied = () => TenantScope.Ensure(new ScopedUser(null, platformAdmin: true, authenticated: false), Guid.NewGuid());
        allowed.Should().NotThrow();
        denied.Should().Throw<ForbiddenApplicationException>();
    }

    [Fact]
    public void Exam_scoped_user_can_view_and_manage_only_assigned_exam()
    {
        var site = Guid.NewGuid();
        var program = Guid.NewGuid();
        var cohort = Guid.NewGuid();
        var exam = Guid.NewGuid();
        var user = new ScopedUser(Guid.NewGuid(), site: site, program: program, cohort: cohort, exam: exam);
        Action allowedView = () => ContextualScope.EnsureCanViewExam(user, site, program, cohort, exam);
        Action allowedManage = () => ContextualScope.EnsureCanManageExam(user, site, program, cohort, exam);
        Action wrongExam = () => ContextualScope.EnsureCanViewExam(user, site, program, cohort, Guid.NewGuid());
        Action wrongCohort = () => ContextualScope.EnsureCanManageExam(user, site, program, Guid.NewGuid(), exam);
        Action wrongProgram = () => ContextualScope.EnsureCanViewExam(user, site, Guid.NewGuid(), cohort, exam);
        Action wrongSite = () => ContextualScope.EnsureCanManageExam(user, Guid.NewGuid(), program, cohort, exam);
        allowedView.Should().NotThrow();
        allowedManage.Should().NotThrow();
        wrongExam.Should().Throw<ForbiddenApplicationException>();
        wrongCohort.Should().Throw<ForbiddenApplicationException>();
        wrongProgram.Should().Throw<ForbiddenApplicationException>();
        wrongSite.Should().Throw<ForbiddenApplicationException>();
    }

    [Fact]
    public void Enrollment_scope_rejects_other_cohort_or_program()
    {
        var site = Guid.NewGuid();
        var program = Guid.NewGuid();
        var cohort = Guid.NewGuid();
        var user = new ScopedUser(Guid.NewGuid(), site: site, program: program, cohort: cohort);
        Action allowed = () => ContextualScope.EnsureCanManageCohort(user, site, program, cohort);
        Action otherCohort = () => ContextualScope.EnsureCanManageCohort(user, site, program, Guid.NewGuid());
        Action otherProgram = () => ContextualScope.EnsureCanViewCohort(user, site, Guid.NewGuid(), cohort);
        allowed.Should().NotThrow();
        otherCohort.Should().Throw<ForbiddenApplicationException>();
        otherProgram.Should().Throw<ForbiddenApplicationException>();
    }

    private sealed class ScopedUser(
        Guid? organization, bool authenticated = true, bool platformAdmin = false,
        Guid? site = null, Guid? program = null, Guid? cohort = null, Guid? exam = null) : ICurrentUser
    {
        public bool IsAuthenticated => authenticated;
        public Guid? UserId => null;
        public Guid? OrganizationId => organization;
        public string? Application => "pedagora";
        public string? Email => null;
        public string? DisplayName => null;
        public IReadOnlySet<string> Roles { get; } = new HashSet<string>();
        public IReadOnlySet<string> Permissions { get; } = new HashSet<string>();
        public bool IsPlatformAdministrator => platformAdmin;
        public IReadOnlyCollection<ContextualScopeAssignment> ContextualScopes => [];
        public bool HasContextualScopeRestrictions => site.HasValue;
        public bool HasPermission(string permission) => false;
        public bool CanViewSite(Guid id) => !site.HasValue || id == site.Value;
        public bool CanManageSite(Guid id) => CanViewSite(id);
        public bool CanViewProgram(Guid s, Guid p) => CanViewSite(s) && (!program.HasValue || p == program.Value);
        public bool CanManageProgram(Guid s, Guid p) => CanViewProgram(s, p);
        public bool CanViewCohort(Guid s, Guid p, Guid c) => CanViewProgram(s, p) && (!cohort.HasValue || c == cohort.Value);
        public bool CanManageCohort(Guid s, Guid p, Guid c) => CanViewCohort(s, p, c);
        public bool CanViewExam(Guid s, Guid p, Guid c, Guid e) => CanViewCohort(s, p, c) && (!exam.HasValue || e == exam.Value);
        public bool CanManageExam(Guid s, Guid p, Guid c, Guid e) => CanViewExam(s, p, c, e);
    }
}
