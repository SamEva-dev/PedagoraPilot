using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Training.Delivery;

namespace PedagoraPilot.Infrastructure.Persistence.Configurations;
public sealed class TrainingSessionConfiguration : IEntityTypeConfiguration<TrainingSession>
{
    public void Configure(EntityTypeBuilder<TrainingSession> b)
    {
        b.ToTable("training_sessions", SchemaNames.Training);
        b.HasKey(x => x.Id);
        b.Ignore(x => x.DomainEvents);
        b.Ignore(x => x.PlannedMinutes);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new TrainingSessionId(x)).ValueGeneratedNever();
        b.Property(x => x.CohortId).HasConversion(x => x.Value, x => new CohortId(x)).IsRequired();
        b.Property(x => x.OrganizationId).IsRequired();
        b.Property(x => x.SiteId).IsRequired();
        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(32).IsRequired();
        b.Property(x => x.Modality).HasConversion<string>().HasMaxLength(32).IsRequired();
        b.Property(x => x.Title).HasMaxLength(240).IsRequired();
        b.Property(x => x.StartsAtUtc).IsRequired();
        b.Property(x => x.EndsAtUtc).IsRequired();
        b.Property(x => x.TimeZoneId).HasMaxLength(100).IsRequired();
        b.Property(x => x.TrainerAuthGateUserId).HasMaxLength(200);
        b.Property(x => x.TrainerDisplayName).HasMaxLength(200);
        b.Property(x => x.Location).HasMaxLength(240);
        b.Property(x => x.Objective).HasMaxLength(2000);
        b.Property(x => x.Supports).HasMaxLength(2000);
        b.Property(x => x.Comments).HasMaxLength(4000);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        b.Property(x => x.AudienceMode).HasConversion<string>().HasMaxLength(32).IsRequired();
        b.Property(x => x.ExternalKey).HasMaxLength(100);
        b.Property(x => x.CreatedAtUtc).IsRequired();
        b.Property(x => x.UpdatedAtUtc).IsRequired();
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => new { x.OrganizationId, x.CohortId, x.StartsAtUtc });
        b.HasIndex(x => new { x.SiteId, x.StartsAtUtc });
        b.HasIndex(x => x.ExternalKey).IsUnique();
        b.HasMany(x => x.Participants).WithOne().HasForeignKey("training_session_id").OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Participants).HasField("_participants").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class TrainingSessionParticipantConfiguration : IEntityTypeConfiguration<TrainingSessionParticipant>
{
    public void Configure(EntityTypeBuilder<TrainingSessionParticipant> b)
    {
        b.ToTable("training_session_participants", SchemaNames.Training);
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new TrainingSessionParticipantId(x)).ValueGeneratedNever();
        b.Property(x => x.EnrollmentId).HasConversion(x => x.Value, x => new EnrollmentId(x)).IsRequired();
        b.HasIndex("training_session_id", nameof(TrainingSessionParticipant.EnrollmentId)).IsUnique();
        b.HasIndex(x => x.EnrollmentId);
    }
}
