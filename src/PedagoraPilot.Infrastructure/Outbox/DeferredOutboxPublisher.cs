using DomainRelay.EFCore.Outbox;
using DomainRelay.EFCore.Outbox.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace PedagoraPilot.Infrastructure.Outbox;
/// <summary>
/// Resolves the transport publisher lazily from the host. This keeps the
/// Infrastructure project independent from SignalR while allowing the API host
/// to replace IRealtimeOutboxTransport.
/// </summary>
public sealed class DeferredOutboxPublisher : IOutboxPublisher
{
    private readonly IServiceScopeFactory _scopeFactory;
    public DeferredOutboxPublisher(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;
    public async Task PublishAsync(OutboxEnvelope envelope, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var transport = scope.ServiceProvider.GetRequiredService<IRealtimeOutboxTransport>();
        await transport.PublishAsync(envelope, ct).ConfigureAwait(false);
    }
}

public interface IRealtimeOutboxTransport
{
    Task PublishAsync(OutboxEnvelope envelope, CancellationToken cancellationToken);
}
