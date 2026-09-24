using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedagoraPilot.Domain.Organizations;

namespace PedagoraPilot.Infrastructure.Persistence.Configurations;
public sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organizations", SchemaNames.Organization);
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);
        builder.Ignore(x => x.IsLoginAllowed);
        builder.Property(x => x.OwnerUserId).IsRequired();
        builder.HasIndex(x => x.OwnerUserId).IsUnique();
        builder.OwnsOne(x => x.Code, code =>
        {
            code.Property(x => x.Value).HasColumnName("code").HasMaxLength(32).IsRequired();
            code.HasIndex(x => x.Value).IsUnique();
        });
        builder.Property(x => x.LegalName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CountryCode).HasMaxLength(2).IsRequired();
        builder.Property(x => x.OwnerEmail).HasMaxLength(256).IsRequired();
        builder.Property(x => x.OwnerPhone).HasMaxLength(40);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();
    }
}
