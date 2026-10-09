using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Training;

namespace PedagoraPilot.Infrastructure.Persistence.Configurations;

public sealed class CohortTrainerAssignmentConfiguration : IEntityTypeConfiguration<CohortTrainerAssignment>
{
    public void Configure(EntityTypeBuilder<CohortTrainerAssignment> b)
    {
        b.ToTable("cohort_trainer_assignments", SchemaNames.Training);
        b.HasKey(x => x.Id);
        b.Ignore(x => x.DomainEvents);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new CohortTrainerAssignmentId(x)).ValueGeneratedNever();
        b.Property(x => x.CohortId).HasConversion(x => x.Value, x => new CohortId(x)).IsRequired();
        b.Property(x => x.MemberId).HasConversion(x => x.Value, x => new OrganizationMemberId(x)).IsRequired();
        b.Property(x => x.OrganizationId).IsRequired();
        b.Property(x => x.AuthGateUserId).IsRequired();
        b.Property(x => x.IsActive).IsRequired();
        b.Property(x => x.CreatedAtUtc).IsRequired();
        b.Property(x => x.UpdatedAtUtc).IsRequired();
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => new { x.OrganizationId, x.CohortId, x.AuthGateUserId }).IsUnique();
        b.HasIndex(x => new { x.OrganizationId, x.AuthGateUserId, x.IsActive });
        b.HasIndex(x => new { x.OrganizationId, x.CohortId, x.IsActive });
    }
}
