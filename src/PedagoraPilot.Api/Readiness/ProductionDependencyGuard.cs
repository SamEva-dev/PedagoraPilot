using PedagoraPilot.Infrastructure.Storage;

namespace PedagoraPilot.Api.Readiness;

/// <summary>
/// Checks the actual implementations resolved from dependency injection, not just
/// the configured provider names. Run before migrations or background workers.
/// </summary>
public static class ProductionDependencyGuard
{
    public static void Validate(
        IHostEnvironment environment,
        ProductionReadinessOptions options,
        Type storageImplementation,
        Type scannerImplementation)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(storageImplementation);
        ArgumentNullException.ThrowIfNull(scannerImplementation);

        if (!environment.IsProduction())
            return;

        if (options.ForbidFileSystemStorage &&
            typeof(FileSystemObjectStorage).IsAssignableFrom(storageImplementation))
        {
            throw new InvalidOperationException(
                "Production startup refused: IObjectStorage resolves to FileSystemObjectStorage. " +
                "Register and verify a production object-storage implementation before deployment.");
        }

        if (options.RequireSecurityScanner &&
            typeof(NoOpFileSecurityScanner).IsAssignableFrom(scannerImplementation))
        {
            throw new InvalidOperationException(
                "Production startup refused: IFileSecurityScanner resolves to NoOpFileSecurityScanner. " +
                "Register and verify a real document security scanner before deployment.");
        }
    }
}
