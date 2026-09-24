using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedagoraPilot.Infrastructure.Persistence.Catalog;

namespace PedagoraPilot.Infrastructure.Persistence.Configurations;
public sealed class ProgramCapabilityRowConfiguration : IEntityTypeConfiguration<ProgramCapabilityRow>
{
    public void Configure(EntityTypeBuilder<ProgramCapabilityRow> b)
    {
        b.ToTable("training_program_capabilities", SchemaNames.Catalog);
        b.Property(x => x.ProgramId).HasColumnName("program_id");
        b.Property(x => x.CapabilityCode).HasColumnName("capability_code").HasMaxLength(80);
    }
}

public sealed class ReferentialVersionCapabilityRowConfiguration : IEntityTypeConfiguration<ReferentialVersionCapabilityRow>
{
    public void Configure(EntityTypeBuilder<ReferentialVersionCapabilityRow> b)
    {
        b.ToTable("referential_version_capabilities", SchemaNames.Catalog);
        b.Property(x => x.ReferentialVersionId).HasColumnName("referential_version_id");
        b.Property(x => x.CapabilityCode).HasColumnName("capability_code").HasMaxLength(80);
    }
}
