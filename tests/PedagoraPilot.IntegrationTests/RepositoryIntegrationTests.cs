using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Domain.Organizations;
using PedagoraPilot.Infrastructure.Persistence.Repositories;
using Xunit;

namespace PedagoraPilot.IntegrationTests;
[Collection(PostgresCollection.Name)]
public sealed class RepositoryIntegrationTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Repository_query_is_no_tracking_by_default()
    {
        await using var db = fixture.CreateContext();
        var organizationId = Guid.NewGuid();
        var site = TrainingSite.Create(organizationId, "NICE", "Nice", "Nice");
        db.TrainingSites.Add(site);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var repository = new TrainingSiteRepository(db);
        var loaded = await repository.GetByIdAsync(site.Id);
        loaded.Should().NotBeNull();
        db.ChangeTracker.Entries<TrainingSite>().Should().BeEmpty();
    }

    [Fact]
    public async Task Explicit_tracking_tracks_entity()
    {
        await using var db = fixture.CreateContext();
        var site = TrainingSite.Create(Guid.NewGuid(), $"S{Guid.NewGuid():N}"[..12], "Tracked", "Nice");
        db.TrainingSites.Add(site);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var repository = new TrainingSiteRepository(db);
        var loaded = await repository.GetByIdAsync(site.Id, isTracking: true);
        loaded.Should().NotBeNull();
        db.ChangeTracker.Entries<TrainingSite>().Should().ContainSingle();
    }
}
