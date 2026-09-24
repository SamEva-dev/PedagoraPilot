using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Domain.Audit;
/// <summary>Append-only audit aggregate. It must never be edited after creation.</summary>
public sealed class AuditEntry : AggregateRoot<AuditEntryId>
{
    private AuditEntry()
    {
    }

    private AuditEntry(AuditEntryId id, Guid? organizationId, Guid? userId, string? userDisplayName, string action, string entityType, string? entityId, string? route, string? correlationId, string? traceId, string? ipAddress, string? beforeJson, string? afterJson, string? metadataJson) : base(id)
    {
        OrganizationId = organizationId;
        UserId = userId;
        UserDisplayName = Trim(userDisplayName, 200);
        Action = Required(action, "AUDIT_ACTION_REQUIRED", 160);
        EntityType = Required(entityType, "AUDIT_ENTITY_TYPE_REQUIRED", 160);
        EntityId = Trim(entityId, 120);
        Route = Trim(route, 500);
        CorrelationId = Trim(correlationId, 120);
        TraceId = Trim(traceId, 120);
        IpAddress = Trim(ipAddress, 80);
        BeforeJson = beforeJson;
        AfterJson = afterJson;
        MetadataJson = metadataJson;
        OccurredAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid? OrganizationId { get; private set; }
    public Guid? UserId { get; private set; }
    public string? UserDisplayName { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public string? EntityId { get; private set; }
    public string? Route { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? TraceId { get; private set; }
    public string? IpAddress { get; private set; }
    public string? BeforeJson { get; private set; }
    public string? AfterJson { get; private set; }
    public string? MetadataJson { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }

    public static AuditEntry Create(Guid? organizationId, Guid? userId, string? userDisplayName, string action, string entityType, string? entityId, string? route, string? correlationId, string? traceId, string? ipAddress, string? beforeJson = null, string? afterJson = null, string? metadataJson = null) => new(AuditEntryId.New(), organizationId, userId, userDisplayName, action, entityType, entityId, route, correlationId, traceId, ipAddress, beforeJson, afterJson, metadataJson);
    private static string Required(string? value, string key, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(key);
        var v = value.Trim();
        if (v.Length > max)
            throw new DomainException(key);
        return v;
    }

    private static string? Trim(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var v = value.Trim();
        return v.Length <= max ? v : v[..max];
    }
}
