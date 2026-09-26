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
        builder.Property(x => x.ShortName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Siret).HasMaxLength(32).IsRequired();
        builder.Property(x => x.TrainingDeclarationNumber).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(240).IsRequired();
        builder.Property(x => x.PostalCode).HasMaxLength(16).IsRequired();
        builder.Property(x => x.City).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Website).HasMaxLength(240).IsRequired();
        builder.Property(x => x.ManagerName).HasMaxLength(160).IsRequired();
        builder.Property(x => x.PrimaryColor).HasMaxLength(16).IsRequired();
        builder.Property(x => x.SecondaryColor).HasMaxLength(16).IsRequired();
        builder.Property(x => x.LoginTagline).HasMaxLength(240).IsRequired();
        builder.Property(x => x.LogoLabel).HasMaxLength(120).IsRequired();
        builder.Property(x => x.EnabledModulesCsv).HasMaxLength(1000).IsRequired();
        builder.Ignore(x => x.EnabledModules);
        builder.Property(x => x.Language).HasMaxLength(16).IsRequired();
        builder.Property(x => x.Timezone).HasMaxLength(64).IsRequired();
        builder.Property(x => x.DateFormat).HasMaxLength(32).IsRequired();
        builder.Property(x => x.AcademicYear).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();
    }
}
