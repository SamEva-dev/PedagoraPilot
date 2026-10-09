using PedagoraPilot.Application.Abstractions.Storage;
using PedagoraPilot.Infrastructure.Storage;

namespace PedagoraPilot.Api.Readiness;

/// <summary>Readiness probes execute before migrations. They fail closed on unreachable
/// services, invalid credentials and unavailable buckets or scanner engines.</summary>
public static class ProductionExternalDependencies
{
    public static async Task VerifyAsync(IServiceProvider provider, IHostEnvironment environment, CancellationToken ct = default)
    {
        if (!environment.IsProduction()) return;
        var scanner = provider.GetRequiredService<IFileSecurityScanner>();
        var storage = provider.GetRequiredService<IObjectStorage>();
        if (scanner is not IProductionConnectivityProbe scannerProbe)
            throw new InvalidOperationException("Production scanner must implement IProductionConnectivityProbe.");
        if (storage is not IProductionConnectivityProbe storageProbe)
            throw new InvalidOperationException("Production storage must implement IProductionConnectivityProbe.");
        await scannerProbe.VerifyConnectivityAsync(ct);
        await storageProbe.VerifyConnectivityAsync(ct);
    }
}
