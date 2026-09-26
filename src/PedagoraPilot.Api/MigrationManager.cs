using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Infrastructure.Persistence;
using Serilog;

namespace PedagoraPilot.Api;

public static class MigrationManager
{
    public static IHost ApplyMigrations(this IHost host)
    {
        using var scope = host.Services.CreateScope();

        try
        {
            var db = scope.ServiceProvider.GetRequiredService<PedagoraPilotDbContext>();
            Log.Information("Applying PedagoraPilot database migrations...");
            db.Database.Migrate();
            Log.Information("PedagoraPilot database migrated successfully.");

        }
        catch (Exception ex)
        {
            Log.Error(ex, "❌ Migration failed.");
            throw;
        }

        return host;
    }
}
