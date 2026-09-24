namespace PedagoraPilot.Application.Abstractions.Audit;
public interface IAuditWriter
{
    Task WriteAsync(string action, string entityType, string? entityId = null, string? beforeJson = null, string? afterJson = null, string? metadataJson = null, CancellationToken cancellationToken = default);
}
