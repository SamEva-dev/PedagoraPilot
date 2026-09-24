using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Training;

namespace PedagoraPilot.Infrastructure.Persistence.Configurations;
public sealed class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> b)
    {
        b.ToTable("enrollments", SchemaNames.Training);
        b.HasKey(x => x.Id);
        b.Ignore(x => x.DomainEvents);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new EnrollmentId(x)).ValueGeneratedNever();
        b.Property(x => x.LearnerProfileId).HasConversion(x => x.Value, x => new LearnerProfileId(x)).IsRequired();
        b.Property(x => x.CohortId).HasConversion(x => x.Value, x => new CohortId(x)).IsRequired();
        b.Property(x => x.OrganizationId).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        b.Property(x => x.ExternalKey).HasMaxLength(100);
        b.Property(x => x.CreatedAtUtc).IsRequired();
        b.Property(x => x.UpdatedAtUtc).IsRequired();
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => new { x.LearnerProfileId, x.CohortId }).IsUnique();
        b.HasIndex(x => new { x.OrganizationId, x.CohortId, x.Status });
        b.HasIndex(x => x.ExternalKey).IsUnique();
    }
}
