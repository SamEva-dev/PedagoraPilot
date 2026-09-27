using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PedagoraPilot.Domain.Catalog;

namespace PedagoraPilot.Infrastructure.Persistence.Seeding;

/// <summary>
/// Restores platform-owned catalog reference data that is required before any tenant
/// can create a training program. This seed is deliberately idempotent: it can run at
/// every API startup and also repairs a database whose business tables were truncated
/// while the EF migration history was kept.
/// </summary>
public static class CatalogReferenceDataSeeder
{
    private sealed record FamilySeed(string Code, string Name, string Icon);

    private static readonly FamilySeed[] Families =
    [
        new("ROAD_EDUCATION", "Enseignement de la conduite", "ph-steering-wheel"),
        new("MOTORCYCLE", "Deux-roues motorisés", "ph-motorcycle"),
        new("HEAVY_VEHICLE", "Groupe lourd", "ph-truck"),
        new("PASSENGER_TRANSPORT", "Transport de voyageurs", "ph-bus"),
        new("EMERGENCY_MEDICAL", "Transport sanitaire", "ph-ambulance"),
        new("FIRST_AID", "Premiers secours", "ph-first-aid-kit")
    ];

    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PedagoraPilotDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<PedagoraPilotDbContext>>();

        var existingCodes = await db.ProgramFamilies
            .AsNoTracking()
            .Select(x => x.Code)
            .ToListAsync(ct);

        var existing = existingCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = Families.Where(x => !existing.Contains(x.Code)).ToArray();
        if (missing.Length == 0)
            return;

        foreach (var family in missing)
            await db.ProgramFamilies.AddAsync(ProgramFamily.Create(family.Code, family.Name, family.Icon), ct);

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Catalog reference data restored. Added {Count} program family record(s): {Codes}",
            missing.Length,
            string.Join(", ", missing.Select(x => x.Code)));
    }
}
