using FluentAssertions;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Catalog.Referentials;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Domain.Catalog;
using PedagoraPilot.Domain.Organizations;
using PedagoraPilot.Infrastructure.Persistence.Repositories;
using Xunit;

namespace PedagoraPilot.IntegrationTests;

/// <summary>
/// Requires the PostgreSQL test database configured by PEDAGORA_TEST_DB.
/// Verifies the same business visibility contract for the list and detail endpoints.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class TenantReferentialAccessRegressionTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Tenant_owned_referential_without_offering_is_visible_only_to_unrestricted_owner()
    {
        await using var db = fixture.CreateContext();
        var organizationId = Guid.NewGuid();
        var family = ProgramFamily.Create($"F{Guid.NewGuid():N}"[..12], "Test family", "ph-book");
        var program = TrainingProgram.Create(family.Id, $"P{Guid.NewGuid():N}"[..12], "Training", "desc", "ph-book", 120, [], organizationId: organizationId);
        var referential = Referential.Create(program.Id, $"R{Guid.NewGuid():N}"[..12], "Referential");
        var version = ReferentialVersion.CreateDraft(referential.Id, "1", null, new DateOnly(2026, 10, 1), 120, 0, 0, []);
        version.Publish(DateTime.UtcNow);

        db.ProgramFamilies.Add(family);
        db.TrainingPrograms.Add(program);
        db.Referentials.Add(referential);
        db.ReferentialVersions.Add(version);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var programs = new TrainingProgramRepository(db);
        var versions = new ReferentialVersionRepository(db);
        var referentials = new ReferentialRepository(db);
        var offerings = new ProgramOfferingRepository(db);
        var sites = new TrainingSiteRepository(db);
        var owner = new TestCurrentUser(organizationId);
        var otherTenant = new TestCurrentUser(Guid.NewGuid());
        var restrictedOwner = new TestCurrentUser(organizationId, restricted: true);

        var listing = new GetReferentialsQueryHandler(referentials, versions, programs, offerings, sites, owner);
        var results = await listing.Handle(new GetReferentialsQuery(), CancellationToken.None);
        results.Should().ContainSingle(x => x.Id == version.Id);
        await TenantCatalogAccess.EnsureReferentialVersionAsync(owner, version.Id, versions, referentials, offerings, sites, programs, CancellationToken.None);

        var restrictedListing = new GetReferentialsQueryHandler(referentials, versions, programs, offerings, sites, restrictedOwner);
        (await restrictedListing.Handle(new GetReferentialsQuery(), CancellationToken.None)).Should().NotContain(x => x.Id == version.Id);
        Func<Task> restrictedRead = () => TenantCatalogAccess.EnsureReferentialVersionAsync(restrictedOwner, version.Id, versions, referentials, offerings, sites, programs, CancellationToken.None);
        await restrictedRead.Should().ThrowAsync<ForbiddenApplicationException>();

        Func<Task> crossTenantRead = () => TenantCatalogAccess.EnsureReferentialVersionAsync(otherTenant, version.Id, versions, referentials, offerings, sites, programs, CancellationToken.None);
        await crossTenantRead.Should().ThrowAsync<ForbiddenApplicationException>();
    }

    [Fact]
    public async Task Contextually_restricted_user_requires_a_matching_active_offering()
    {
        await using var db = fixture.CreateContext();
        var organizationId = Guid.NewGuid();
        var family = ProgramFamily.Create($"F{Guid.NewGuid():N}"[..12], "Test family", "ph-book");
        var program = TrainingProgram.Create(family.Id, $"P{Guid.NewGuid():N}"[..12], "Training", "desc", "ph-book", 120, [], organizationId: organizationId);
        var referential = Referential.Create(program.Id, $"R{Guid.NewGuid():N}"[..12], "Referential");
        var version = ReferentialVersion.CreateDraft(referential.Id, "1", null, new DateOnly(2026, 10, 1), 120, 0, 0, []);
        version.Publish(DateTime.UtcNow);
        var site = TrainingSite.Create(organizationId, $"S{Guid.NewGuid():N}"[..12], "Nice", "Nice");
        var offering = ProgramOffering.Create(site.Id, program.Id);
        db.ProgramFamilies.Add(family);
        db.TrainingPrograms.Add(program);
        db.Referentials.Add(referential);
        db.ReferentialVersions.Add(version);
        db.TrainingSites.Add(site);
        db.ProgramOfferings.Add(offering);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var programs = new TrainingProgramRepository(db);
        var versions = new ReferentialVersionRepository(db);
        var referentials = new ReferentialRepository(db);
        var offerings = new ProgramOfferingRepository(db);
        var sites = new TrainingSiteRepository(db);
        var allowed = new TestCurrentUser(organizationId, restricted: true, siteId: site.Id, programId: program.Id);
        var wrongProgram = new TestCurrentUser(organizationId, restricted: true, siteId: site.Id, programId: Guid.NewGuid());

        await TenantCatalogAccess.EnsureReferentialVersionAsync(allowed, version.Id, versions, referentials, offerings, sites, programs, CancellationToken.None);
        Func<Task> denied = () => TenantCatalogAccess.EnsureReferentialVersionAsync(wrongProgram, version.Id, versions, referentials, offerings, sites, programs, CancellationToken.None);
        await denied.Should().ThrowAsync<ForbiddenApplicationException>();
    }

    private sealed class TestCurrentUser(Guid organizationId, bool restricted = false, Guid? siteId = null, Guid? programId = null) : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => null;
        public Guid? OrganizationId => organizationId;
        public string? Application => "pedagora";
        public string? Email => null;
        public string? DisplayName => null;
        public IReadOnlySet<string> Roles { get; } = new HashSet<string>();
        public IReadOnlySet<string> Permissions { get; } = new HashSet<string>();
        public bool IsPlatformAdministrator => false;
        public IReadOnlyCollection<ContextualScopeAssignment> ContextualScopes => restricted ? [new ContextualScopeAssignment(ContextualScopeLevel.Program, siteId, programId)] : [];
        public bool HasContextualScopeRestrictions => restricted;
        public bool HasPermission(string permission) => false;
        public bool CanViewSite(Guid candidateSiteId) => !restricted || candidateSiteId == siteId;
        public bool CanManageSite(Guid candidateSiteId) => CanViewSite(candidateSiteId);
        public bool CanViewProgram(Guid candidateSiteId, Guid candidateProgramId) => !restricted || (candidateSiteId == siteId && candidateProgramId == programId);
        public bool CanManageProgram(Guid candidateSiteId, Guid candidateProgramId) => CanViewProgram(candidateSiteId, candidateProgramId);
        public bool CanViewCohort(Guid candidateSiteId, Guid candidateProgramId, Guid candidateCohortId) => CanViewProgram(candidateSiteId, candidateProgramId);
        public bool CanManageCohort(Guid candidateSiteId, Guid candidateProgramId, Guid candidateCohortId) => CanViewCohort(candidateSiteId, candidateProgramId, candidateCohortId);
        public bool CanViewExam(Guid candidateSiteId, Guid candidateProgramId, Guid candidateCohortId, Guid candidateExamId) => CanViewProgram(candidateSiteId, candidateProgramId);
        public bool CanManageExam(Guid candidateSiteId, Guid candidateProgramId, Guid candidateCohortId, Guid candidateExamId) => CanViewExam(candidateSiteId, candidateProgramId, candidateCohortId, candidateExamId);
    }
}
