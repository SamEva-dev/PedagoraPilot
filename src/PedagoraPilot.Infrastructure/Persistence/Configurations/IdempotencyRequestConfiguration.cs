using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedagoraPilot.Infrastructure.Persistence.Idempotency;

namespace PedagoraPilot.Infrastructure.Persistence.Configurations;
public sealed class IdempotencyRequestConfiguration : IEntityTypeConfiguration<IdempotencyRequest>
{
    public void Configure(EntityTypeBuilder<IdempotencyRequest> builder)
    {
        builder.ToTable("idempotency_requests", SchemaNames.Integration);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Key).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Scope).HasMaxLength(500).IsRequired();
        builder.Property(x => x.RequestHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ResponseContentType).HasMaxLength(200);
        builder.HasIndex(x => new { x.Key, x.Scope }).IsUnique();
        builder.HasIndex(x => x.ExpiresAtUtc);
    }
}
