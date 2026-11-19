using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedagoraPilot.Domain.Catalog;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Infrastructure.Persistence.Configurations;
public sealed class DeaReferentialStructureConfiguration : IEntityTypeConfiguration<DeaReferentialStructure>
{
    public void Configure(EntityTypeBuilder<DeaReferentialStructure> b)
    {
        b.ToTable("dea_referential_structures", SchemaNames.Catalog);
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new DeaReferentialStructureId(x)).ValueGeneratedNever();
        b.Property(x => x.ReferentialVersionId).HasColumnName("referential_version_id").IsRequired();
        b.HasIndex(x => x.ReferentialVersionId).IsUnique();
        b.Property(x => x.ContentJson).HasColumnName("content_json").HasColumnType("jsonb").IsRequired();
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
    }
}
