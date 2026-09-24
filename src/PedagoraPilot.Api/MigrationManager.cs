using Itech.Emailing.Persistence;
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
            // Apply AuthDbContext migrations
            var authDb = scope.ServiceProvider.GetRequiredService<PedagoraPilotDbContext>();
            Log.Information("Applying PedagoraPilot database migrations...");
            authDb.Database.Migrate();
            Log.Information("✅ AuthGate PedagoraPilot migrated successfully.");

            // Apply EmailingDbContext migrations
            var emailingDb = scope.ServiceProvider.GetRequiredService<EmailingDbContext>();
            Log.Information("Applying Emailing database migrations...");
            emailingDb.Database.Migrate();
            Log.Information("✅ Emailing database migrated successfully.");


        }
        catch (Exception ex)
        {
            Log.Error(ex, "❌ Migration failed.");
            throw;
        }

        return host;
    }
}
