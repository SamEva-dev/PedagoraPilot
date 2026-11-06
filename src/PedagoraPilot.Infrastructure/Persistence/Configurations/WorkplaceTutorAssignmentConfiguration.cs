using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedagoraPilot.Domain.Workplace;
using PedagoraPilot.Domain.Identifiers;
namespace PedagoraPilot.Infrastructure.Persistence.Configurations;
public sealed class WorkplaceTutorAssignmentConfiguration : IEntityTypeConfiguration<WorkplaceTutorAssignment>
{
    public void Configure(EntityTypeBuilder<WorkplaceTutorAssignment> b)
    {
        b.ToTable("tutor_assignments", SchemaNames.Workplace);
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.InvitedEmail).HasMaxLength(250).IsRequired();
        b.Property(x => x.WorkplacePeriodId)
            .HasConversion(id => id.Value, value => new WorkplacePeriodId(value))
            .HasColumnType("uuid")
            .IsRequired();
        b.HasIndex(x => new { x.OrganizationId, x.InvitationId });
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.HasOne<WorkplacePeriod>().WithMany().HasForeignKey(x => x.WorkplacePeriodId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.OrganizationId, x.WorkplacePeriodId, x.AuthGateUserId });
        b.HasIndex(x => new { x.OrganizationId, x.WorkplacePeriodId, x.InvitedEmail }).IsUnique()
            .HasFilter("\"Status\" <> 'Revoked'");
    }
}
