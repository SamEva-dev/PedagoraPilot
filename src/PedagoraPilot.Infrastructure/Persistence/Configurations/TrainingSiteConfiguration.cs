using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedagoraPilot.Domain.Organizations;

namespace PedagoraPilot.Infrastructure.Persistence.Configurations;
public sealed class TrainingSiteConfiguration : IEntityTypeConfiguration<TrainingSite>
{
    public void Configure(EntityTypeBuilder<TrainingSite> b)
    {
        b.ToTable("training_sites", SchemaNames.Organization);
        b.HasKey(x => x.Id);
        b.Ignore(x => x.DomainEvents);
        b.Ignore(x => x.IsActive);
        b.Property(x => x.OrganizationId).IsRequired();
        b.OwnsOne(x => x.Code, c =>
        {
            c.Property(x => x.Value).HasColumnName("code").HasMaxLength(32).IsRequired();
        });
        b.Property(x => x.Name).HasMaxLength(160).IsRequired();
        b.Property(x => x.City).HasMaxLength(120).IsRequired();
        b.Property(x => x.ExternalKey).HasMaxLength(80);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        b.Property(x => x.CreatedAtUtc).IsRequired();
        b.Property(x => x.UpdatedAtUtc).IsRequired();
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => new { x.OrganizationId, x.ExternalKey }).IsUnique();
    }
}
