using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedagoraPilot.Domain.Catalog;

namespace PedagoraPilot.Infrastructure.Persistence.Configurations;
public sealed class ProgramFamilyConfiguration : IEntityTypeConfiguration<ProgramFamily>
{
    public void Configure(EntityTypeBuilder<ProgramFamily> b)
    {
        b.ToTable("program_families", SchemaNames.Catalog);
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(64).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(180).IsRequired();
        b.Property(x => x.Icon).HasColumnName("icon").HasMaxLength(80);
        b.Property(x => x.IsActive).HasColumnName("is_active");
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
    }
}

public sealed class TrainingProgramConfiguration : IEntityTypeConfiguration<TrainingProgram>
{
    public void Configure(EntityTypeBuilder<TrainingProgram> b)
    {
        b.ToTable("training_programs", SchemaNames.Catalog);
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasConversion(x => x.Value, x => PedagoraPilot.Domain.Catalog.ValueObjects.ProgramCode.Create(x)).HasColumnName("code").HasMaxLength(64);
        b.HasIndex(x => x.Code).IsUnique();
        b.Property(x => x.FamilyId).HasColumnName("family_id");
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(220);
        b.Property(x => x.DescriptionKey).HasColumnName("description_key").HasMaxLength(220);
        b.Property(x => x.Icon).HasColumnName("icon").HasMaxLength(80);
        b.Property(x => x.DurationHours).HasColumnName("duration_hours");
        b.Property(x => x.Status).HasConversion<string>().HasColumnName("status").HasMaxLength(32);
        b.Property(x => x.ExternalKey).HasColumnName("external_key").HasMaxLength(120);
        b.HasIndex(x => x.ExternalKey).IsUnique();
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
        b.Ignore(x => x.Capabilities);
        b.Ignore("_capabilities");
    }
}

public sealed class ProgramOfferingConfiguration : IEntityTypeConfiguration<ProgramOffering>
{
    public void Configure(EntityTypeBuilder<ProgramOffering> b)
    {
        b.ToTable("program_offerings", SchemaNames.Catalog);
        b.HasKey(x => x.Id);
        b.Property(x => x.SiteId).HasColumnName("site_id");
        b.Property(x => x.ProgramId).HasColumnName("program_id");
        b.Property(x => x.IsActive).HasColumnName("is_active");
        b.Property(x => x.ExternalKey).HasColumnName("external_key").HasMaxLength(140);
        b.HasIndex(x => new { x.SiteId, x.ProgramId }).IsUnique();
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
    }
}

public sealed class ReferentialConfiguration : IEntityTypeConfiguration<Referential>
{
    public void Configure(EntityTypeBuilder<Referential> b)
    {
        b.ToTable("referentials", SchemaNames.Catalog);
        b.HasKey(x => x.Id);
        b.Property(x => x.ProgramId).HasColumnName("program_id");
        b.Property(x => x.Code).HasColumnName("code").HasMaxLength(96);
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(220);
        b.Property(x => x.ExternalKey).HasColumnName("external_key").HasMaxLength(140);
        b.HasIndex(x => new { x.ProgramId, x.Code }).IsUnique();
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
    }
}

public sealed class ReferentialVersionConfiguration : IEntityTypeConfiguration<ReferentialVersion>
{
    public void Configure(EntityTypeBuilder<ReferentialVersion> b)
    {
        b.ToTable("referential_versions", SchemaNames.Catalog);
        b.HasKey(x => x.Id);
        b.Property(x => x.ReferentialId).HasColumnName("referential_id");
        b.Property(x => x.VersionLabel).HasColumnName("version_label").HasMaxLength(160);
        b.Property(x => x.CertificationCode).HasColumnName("certification_code").HasMaxLength(80);
        b.Property(x => x.Status).HasConversion<string>().HasColumnName("status").HasMaxLength(32);
        b.Property(x => x.EffectiveFrom).HasColumnName("effective_from");
        b.Property(x => x.EffectiveTo).HasColumnName("effective_to");
        b.Property(x => x.TotalHours).HasColumnName("total_hours");
        b.Property(x => x.SheetCount).HasColumnName("sheet_count");
        b.Property(x => x.RequiredDocumentCount).HasColumnName("required_document_count");
        b.Property(x => x.NotesKey).HasColumnName("notes_key").HasMaxLength(220);
        b.Property(x => x.ExternalKey).HasColumnName("external_key").HasMaxLength(140);
        b.Property(x => x.PublishedAtUtc).HasColumnName("published_at_utc");
        b.HasIndex(x => new { x.ReferentialId, x.VersionLabel }).IsUnique();
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
        b.Ignore(x => x.Capabilities);
        b.Ignore("_capabilities");
    }
}
