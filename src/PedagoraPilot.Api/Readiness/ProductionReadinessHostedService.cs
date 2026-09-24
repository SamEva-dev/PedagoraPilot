using Microsoft.Extensions.Options;

namespace PedagoraPilot.Api.Readiness;
public sealed class ProductionReadinessHostedService(IOptions<ProductionReadinessOptions> options, ILogger<ProductionReadinessHostedService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = options.Value; // Forces ValidateOnStart / IValidateOptions execution.
        logger.LogInformation("Pedagora Pilot production-readiness checks passed.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
