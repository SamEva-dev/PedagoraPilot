using DomainRelay.Abstractions;
using PedagoraPilot.Application.Abstractions.Audit;
using PedagoraPilot.Application.Abstractions.Messaging;

namespace PedagoraPilot.Application.Common.Behaviors;
/// <summary>
/// Audits successful transactional commands. It intentionally stores only command metadata by default:
/// sensitive request payloads must never be dumped automatically into the audit trail.
/// Register this behavior inside UnitOfWorkBehavior so the audit row commits atomically with the command.
/// </summary>
public sealed class AuditBehavior<TRequest, TResponse>(IAuditWriter auditWriter) : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, HandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next().ConfigureAwait(false);
        if (request is ITransactionalRequest)
        {
            var requestName = typeof(TRequest).Name;
            var entityType = requestName.Replace("Command", string.Empty, StringComparison.Ordinal).Replace("Create", string.Empty, StringComparison.Ordinal).Replace("Update", string.Empty, StringComparison.Ordinal).Replace("Delete", string.Empty, StringComparison.Ordinal).Replace("Record", string.Empty, StringComparison.Ordinal).Replace("Publish", string.Empty, StringComparison.Ordinal).Replace("Cancel", string.Empty, StringComparison.Ordinal);
            await auditWriter.WriteAsync(action: requestName, entityType: string.IsNullOrWhiteSpace(entityType) ? requestName : entityType, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        return response;
    }
}
