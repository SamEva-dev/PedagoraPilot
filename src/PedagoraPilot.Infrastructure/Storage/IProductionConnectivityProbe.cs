namespace PedagoraPilot.Infrastructure.Storage;

/// <summary>Implement on real infrastructure adapters to prove connectivity before
/// Production migrations. A type name alone never constitutes a readiness check.</summary>
public interface IProductionConnectivityProbe
{
    Task VerifyConnectivityAsync(CancellationToken cancellationToken = default);
}
