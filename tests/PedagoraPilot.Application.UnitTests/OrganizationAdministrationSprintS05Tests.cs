using FluentAssertions;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Application.Organizations.Administration;
using PedagoraPilot.Contracts.Administration;
using PedagoraPilot.Domain.Organizations;
using Xunit;

namespace PedagoraPilot.Application.UnitTests;

public sealed class OrganizationAdministrationSprintS05Tests
{
    [Fact]
    public async Task Update_requires_explicit_version_and_rejects_stale_write()
    {
        var org = Organization.Provision(Guid.NewGuid(), "ORG-S05", "Centre S05", "FR", "owner@domain.test", null);
        var handler = new UpdateOrganizationAdministrationCommandHandler(new OrgRepository(org), new User(org.Id));
        var before = org.Version;

        var missing = () => handler.Handle(new UpdateOrganizationAdministrationCommand(org.Id, Request(null)), CancellationToken.None);
        (await missing.Should().ThrowAsync<ConflictApplicationException>()).Which.ErrorKey.Should().Be(ErrorKeys.ConcurrencyConflict);
        org.Version.Should().Be(before);

        var saved = await handler.Handle(new UpdateOrganizationAdministrationCommand(org.Id, Request(before)), CancellationToken.None);
        saved.Version.Should().Be(before + 1);
        org.Version.Should().Be(before + 1);

        var stale = () => handler.Handle(new UpdateOrganizationAdministrationCommand(org.Id, Request(before)), CancellationToken.None);
        (await stale.Should().ThrowAsync<ConflictApplicationException>()).Which.ErrorKey.Should().Be(ErrorKeys.ConcurrencyConflict);
        org.Version.Should().Be(before + 1);
    }

    [Fact]
    public async Task Update_rejects_cross_tenant_request()
    {
        var org = Organization.Provision(Guid.NewGuid(), "ORG-S05", "Centre S05", "FR", "owner@domain.test", null);
        var handler = new UpdateOrganizationAdministrationCommandHandler(new OrgRepository(org), new User(Guid.NewGuid()));
        var action = () => handler.Handle(new UpdateOrganizationAdministrationCommand(org.Id, Request(org.Version)), CancellationToken.None);
        await action.Should().ThrowAsync<ForbiddenApplicationException>();
    }

    private static UpdateOrganizationAdministrationRequest Request(uint? expectedVersion) => new(
        "Centre S05", "S05", "", "", "", "", "", "FR", "owner@domain.test", "", "", "",
        "#1456A0", "#F59E0B", "", "Centre", false, true, ["planning"],
        true, true, true, false, true, "fr", "Europe/Paris", "DD/MM/YYYY", "2026-2027",
        true, true, 2, true, true, expectedVersion);

    private sealed class OrgRepository(Organization organization) : IOrganizationRepository
    {
        public Task<Organization?> GetByIdAsync(Guid id, bool isTracking = false, CancellationToken cancellationToken = default)
            => Task.FromResult<Organization?>(id == organization.Id ? organization : null);
        public Task<Organization?> GetByOwnerUserIdAsync(Guid id, bool isTracking = false, CancellationToken cancellationToken = default)
            => Task.FromResult<Organization?>(id == organization.OwnerUserId ? organization : null);
        public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task AddAsync(Organization x, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public IQueryable<Organization> Query(bool isTracking = false) => new[] { organization }.AsQueryable();
        public void Update(Organization x) { }
        public void Remove(Organization x) { }
    }

    private sealed class User(Guid organizationId) : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => Guid.Empty;
        public Guid? OrganizationId => organizationId;
        public string? Application => "pedagora";
        public string? Email => "test@domain.test";
        public string? DisplayName => "Test";
        public IReadOnlySet<string> Roles => new HashSet<string>();
        public IReadOnlySet<string> Permissions => new HashSet<string>();
        public bool IsPlatformAdministrator => false;
        public IReadOnlyCollection<ContextualScopeAssignment> ContextualScopes => [];
        public bool HasContextualScopeRestrictions => false;
        public bool HasPermission(string code) => false;
        public bool CanViewSite(Guid x) => false;
        public bool CanManageSite(Guid x) => false;
        public bool CanViewProgram(Guid x, Guid y) => false;
        public bool CanManageProgram(Guid x, Guid y) => false;
        public bool CanViewCohort(Guid x, Guid y, Guid z) => false;
        public bool CanManageCohort(Guid x, Guid y, Guid z) => false;
        public bool CanViewExam(Guid x, Guid y, Guid z, Guid t) => false;
        public bool CanManageExam(Guid x, Guid y, Guid z, Guid t) => false;
    }
}
