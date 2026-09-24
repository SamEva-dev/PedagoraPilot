using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PedagoraPilot.Infrastructure.Persistence;
public sealed class PedagoraPilotDbContextFactory : IDesignTimeDbContextFactory<PedagoraPilotDbContext>
{
    public PedagoraPilotDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("PEDAGORA_PILOT_CONNECTION_STRING") ?? "Host=localhost;Port=5432;Database=PedagoraPilot;Username=postgres;Password=CHANGE_ME";
        var options = new DbContextOptionsBuilder<PedagoraPilotDbContext>().UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(PedagoraPilotDbContext).Assembly.FullName).MigrationsHistoryTable("__EFMigrationsHistory", SchemaNames.Platform)).Options;
        return new PedagoraPilotDbContext(options);
    }
}
