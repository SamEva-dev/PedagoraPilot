using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Training;

namespace PedagoraPilot.Infrastructure.Persistence.Configurations;
public sealed class CohortConfiguration : IEntityTypeConfiguration<Cohort>
{
    public void Configure(EntityTypeBuilder<Cohort> b)
    {
        b.ToTable("cohorts", SchemaNames.Training);
        b.HasKey(x => x.Id);
        b.Ignore(x => x.DomainEvents);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new CohortId(x)).ValueGeneratedNever();
        b.Property(x => x.OrganizationId).IsRequired();
        b.Property(x => x.SiteId).IsRequired();
        b.Property(x => x.ProgramOfferingId).IsRequired();
        b.Property(x => x.ReferentialVersionId).IsRequired();
        b.Property(x => x.Code).HasMaxLength(64).IsRequired();
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        b.Property(x => x.ExternalKey).HasMaxLength(100);
        b.Property(x => x.CreatedAtUtc).IsRequired();
        b.Property(x => x.UpdatedAtUtc).IsRequired();
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => new { x.OrganizationId, x.Code }).IsUnique();
        b.HasIndex(x => new { x.SiteId, x.ProgramOfferingId });
        b.HasIndex(x => x.ExternalKey).IsUnique();
    }
}
