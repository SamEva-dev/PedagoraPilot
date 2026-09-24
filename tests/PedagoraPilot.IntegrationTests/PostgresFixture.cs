using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Infrastructure.Persistence;
using Xunit;

namespace PedagoraPilot.IntegrationTests;
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}

public sealed class PostgresFixture : IAsyncLifetime
{
    public string ConnectionString { get; } = Environment.GetEnvironmentVariable("PEDAGORA_TEST_DB") ?? "Host=localhost;Port=55432;Database=pedagora_pilot_tests;Username=postgres;Password=postgres;Include Error Detail=true";

    public PedagoraPilotDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PedagoraPilotDbContext>().UseNpgsql(ConnectionString).EnableDetailedErrors().Options;
        return new PedagoraPilotDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
