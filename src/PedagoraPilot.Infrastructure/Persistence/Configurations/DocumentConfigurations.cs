using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedagoraPilot.Domain.Documents;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Infrastructure.Persistence.Configurations;
public sealed class ManagedDocumentConfiguration : IEntityTypeConfiguration<ManagedDocument>
{
    public void Configure(EntityTypeBuilder<ManagedDocument> b)
    {
        b.ToTable("documents", SchemaNames.Document);
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new DocumentId(x)).ValueGeneratedNever();
        b.Property(x => x.OwnerType).HasConversion<string>().HasMaxLength(50).IsRequired();
        b.Property(x => x.Title).HasMaxLength(300).IsRequired();
        b.Property(x => x.Description).HasMaxLength(4000);
        b.Property(x => x.Category).HasConversion<string>().HasMaxLength(50).IsRequired();
        b.Property(x => x.Visibility).HasConversion<string>().HasMaxLength(50).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        b.Property(x => x.CreatedByUserId).HasMaxLength(250).IsRequired();
        b.Property(x => x.CreatedByDisplayName).HasMaxLength(250).IsRequired();
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasMany(x => x.Versions).WithOne().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.OrganizationId, x.CohortId, x.Category });
        b.HasIndex(x => new { x.OrganizationId, x.OwnerType, x.OwnerId });
        b.Ignore(x => x.CurrentVersion);
        b.Ignore(x => x.DomainEvents);
    }
}

public sealed class DocumentVersionConfiguration : IEntityTypeConfiguration<DocumentVersion>
{
    public void Configure(EntityTypeBuilder<DocumentVersion> b)
    {
        b.ToTable("document_versions", SchemaNames.Document);
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new DocumentVersionId(x)).ValueGeneratedNever();
        b.Property(x => x.DocumentId).HasConversion(x => x.Value, x => new DocumentId(x)).IsRequired();
        b.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        b.Property(x => x.ContentType).HasMaxLength(200).IsRequired();
        b.Property(x => x.Sha256).HasMaxLength(64).IsRequired();
        b.Property(x => x.StorageKey).HasMaxLength(1024).IsRequired();
        b.Property(x => x.SecurityStatus).HasConversion<string>().HasMaxLength(40).IsRequired();
        b.Property(x => x.UploadedByUserId).HasMaxLength(250).IsRequired();
        b.Property(x => x.UploadedByDisplayName).HasMaxLength(250).IsRequired();
        b.HasIndex(x => new { x.DocumentId, x.VersionNumber }).IsUnique();
        b.HasIndex(x => x.Sha256);
    }
}
