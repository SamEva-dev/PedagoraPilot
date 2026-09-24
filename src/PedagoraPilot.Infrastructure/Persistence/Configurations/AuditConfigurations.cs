using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedagoraPilot.Domain.Audit;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Infrastructure.Persistence.Configurations;
public sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> b)
    {
        b.ToTable("audit_entries", SchemaNames.Audit);
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new AuditEntryId(x)).ValueGeneratedNever();
        b.Property(x => x.OrganizationId);
        b.Property(x => x.UserId);
        b.Property(x => x.UserDisplayName).HasMaxLength(200);
        b.Property(x => x.Action).HasMaxLength(160).IsRequired();
        b.Property(x => x.EntityType).HasMaxLength(160).IsRequired();
        b.Property(x => x.EntityId).HasMaxLength(120);
        b.Property(x => x.Route).HasMaxLength(500);
        b.Property(x => x.CorrelationId).HasMaxLength(120);
        b.Property(x => x.TraceId).HasMaxLength(120);
        b.Property(x => x.IpAddress).HasMaxLength(80);
        b.Property(x => x.BeforeJson).HasColumnType("jsonb");
        b.Property(x => x.AfterJson).HasColumnType("jsonb");
        b.Property(x => x.MetadataJson).HasColumnType("jsonb");
        b.Property(x => x.OccurredAtUtc).IsRequired();
        b.HasIndex(x => new { x.OrganizationId, x.OccurredAtUtc });
        b.HasIndex(x => new { x.EntityType, x.EntityId });
        b.HasIndex(x => x.CorrelationId);
        b.HasIndex(x => x.UserId);
        b.Ignore(x => x.DomainEvents);
    }
}
