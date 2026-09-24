namespace PedagoraPilot.Domain.Common;
public interface IAuditableEntity
{
    DateTimeOffset CreatedAtUtc { get; }

    Guid? CreatedByUserId { get; }

    DateTimeOffset? LastModifiedAtUtc { get; }

    Guid? LastModifiedByUserId { get; }

    void SetCreatedAudit(DateTimeOffset createdAtUtc, Guid? createdByUserId);
    void SetModifiedAudit(DateTimeOffset modifiedAtUtc, Guid? modifiedByUserId);
}
