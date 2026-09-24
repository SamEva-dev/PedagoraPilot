using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using PedagoraPilot.Application.Abstractions.Audit;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Domain.Audit;

namespace PedagoraPilot.Api.Audit;
/// <summary>
/// Creates append-only audit rows. This service does not call SaveChanges:
/// the existing UnitOfWork owns the transaction.
/// </summary>
public sealed class HttpAuditWriter(IAuditEntryRepository repository, ICurrentUser currentUser, IHttpContextAccessor http) : IAuditWriter
{
    public Task WriteAsync(string action, string entityType, string? entityId = null, string? beforeJson = null, string? afterJson = null, string? metadataJson = null, CancellationToken cancellationToken = default)
    {
        var ctx = http.HttpContext;
        var activity = Activity.Current;
        var entry = AuditEntry.Create(currentUser.OrganizationId, currentUser.UserId, currentUser.DisplayName, action, entityType, entityId, ctx?.Request.Path.Value, ctx?.TraceIdentifier, activity?.TraceId.ToString(), ctx?.Connection.RemoteIpAddress?.ToString(), beforeJson, afterJson, metadataJson);
        return repository.AddAsync(entry, cancellationToken);
    }
}
