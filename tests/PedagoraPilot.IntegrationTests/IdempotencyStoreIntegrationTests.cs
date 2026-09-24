using FluentAssertions;
using PedagoraPilot.Infrastructure.Persistence.Idempotency;
using Xunit;

namespace PedagoraPilot.IntegrationTests;
[Collection(PostgresCollection.Name)]
public sealed class IdempotencyStoreIntegrationTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Processing_request_can_be_completed_and_read_no_tracking()
    {
        await using var db = fixture.CreateContext();
        var store = new IdempotencyStore(db);
        var request = new IdempotencyRequest
        {
            Id = Guid.NewGuid(),
            Key = Guid.NewGuid().ToString("N"),
            Scope = "qa|org|POST|/api/test",
            RequestHash = "abc123",
            Status = "Processing",
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
        };
        await store.AddProcessingAsync(request);
        db.ChangeTracker.Clear();
        var loaded = await store.FindAsync(request.Key, request.Scope, isTracking: false);
        loaded.Should().NotBeNull();
        db.ChangeTracker.Entries<IdempotencyRequest>().Should().BeEmpty();
        await store.CompleteAsync(request.Id, 201, "application/json", "{\"ok\":true}");
        db.ChangeTracker.Clear();
        var completed = await store.FindAsync(request.Key, request.Scope, isTracking: false);
        completed!.Status.Should().Be("Completed");
        completed.ResponseStatusCode.Should().Be(201);
        completed.ResponseBody.Should().Be("{\"ok\":true}");
    }
}
