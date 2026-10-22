using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedagoraPilot.Domain.Branding;
namespace PedagoraPilot.Infrastructure.Persistence.Configurations;
public sealed class OrganizationBrandingConfiguration : IEntityTypeConfiguration<OrganizationBranding>
{
    public void Configure(EntityTypeBuilder<OrganizationBranding> b) { b.ToTable("organization_brandings", "branding"); b.HasKey(x => x.OrganizationId); b.Property(x=>x.UpdatedAtUtc).IsRequired(); }
}
public sealed class BrandingRevisionConfiguration : IEntityTypeConfiguration<BrandingRevision>
{
    public void Configure(EntityTypeBuilder<BrandingRevision> b) { b.ToTable("branding_revisions", "branding"); b.HasKey(x=>x.Id); b.Property(x=>x.ConfigurationJson).HasColumnType("jsonb").IsRequired(); b.HasIndex(x=>new {x.OrganizationId,x.CreatedAtUtc}); }
}
public sealed class BrandingAssetConfiguration : IEntityTypeConfiguration<BrandingAsset>
{
    public void Configure(EntityTypeBuilder<BrandingAsset> b) { b.ToTable("branding_assets", "branding"); b.HasKey(x=>x.Id); b.Property(x=>x.StorageKey).HasMaxLength(512).IsRequired(); b.Property(x=>x.ContentType).HasMaxLength(64).IsRequired(); b.HasIndex(x=>x.OrganizationId); }
}
