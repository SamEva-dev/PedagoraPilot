using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedagoraPilot.Domain.DistanceLearning;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Infrastructure.Persistence.Configurations;
public sealed class DistanceLearningSessionConfiguration : IEntityTypeConfiguration<DistanceLearningSession>
{
    public void Configure(EntityTypeBuilder<DistanceLearningSession> b)
    {
        b.ToTable("live_sessions", SchemaNames.DistanceLearning);
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new DistanceLearningSessionId(x)).ValueGeneratedNever();
        b.Property(x => x.CohortId).HasConversion(x => x.Value, x => new CohortId(x));
        b.Property(x => x.Title).HasMaxLength(240).IsRequired();
        b.Property(x => x.TrainerDisplayName).HasMaxLength(200).IsRequired();
        b.Property(x => x.TrainerEmail).HasMaxLength(250);
        b.Property(x => x.Platform).HasConversion<string>().HasMaxLength(40);
        b.Property(x => x.JoinUrl).HasMaxLength(2000).IsRequired();
        b.Property(x => x.Objectives).HasMaxLength(4000);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => new { x.OrganizationId, x.CohortId, x.StartsAtUtc });
        b.HasMany(x => x.Participants).WithOne().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Participants).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Ignore(x => x.DomainEvents);
    }
}

public sealed class DistanceParticipantConfiguration : IEntityTypeConfiguration<DistanceParticipant>
{
    public void Configure(EntityTypeBuilder<DistanceParticipant> b)
    {
        b.ToTable("live_participants", SchemaNames.DistanceLearning);
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new DistanceParticipantId(x)).ValueGeneratedNever();
        b.Property(x => x.SessionId).HasConversion(x => x.Value, x => new DistanceLearningSessionId(x));
        b.Property(x => x.EnrollmentId).HasConversion(x => x.Value, x => new EnrollmentId(x));
        b.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        b.Property(x => x.Attendance).HasConversion<string>().HasMaxLength(40);
        b.HasIndex(x => new { x.SessionId, x.EnrollmentId }).IsUnique();
    }
}

public sealed class AsyncLearningModuleConfiguration : IEntityTypeConfiguration<AsyncLearningModule>
{
    public void Configure(EntityTypeBuilder<AsyncLearningModule> b)
    {
        b.ToTable("async_modules", SchemaNames.DistanceLearning);
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new AsyncLearningModuleId(x)).ValueGeneratedNever();
        b.Property(x => x.CohortId).HasConversion(x => x.Value, x => new CohortId(x));
        b.Property(x => x.Title).HasMaxLength(240).IsRequired();
        b.Property(x => x.Description).HasMaxLength(4000);
        b.Property(x => x.TrainerDisplayName).HasMaxLength(200).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => new { x.OrganizationId, x.CohortId, x.DueDate });
        b.HasMany(x => x.Steps).WithOne().HasForeignKey(x => x.ModuleId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Steps).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Ignore(x => x.DomainEvents);
    }
}

public sealed class AsyncModuleStepConfiguration : IEntityTypeConfiguration<AsyncModuleStep>
{
    public void Configure(EntityTypeBuilder<AsyncModuleStep> b)
    {
        b.ToTable("async_module_steps", SchemaNames.DistanceLearning);
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new AsyncModuleStepId(x)).ValueGeneratedNever();
        b.Property(x => x.ModuleId).HasConversion(x => x.Value, x => new AsyncLearningModuleId(x));
        b.Property(x => x.Code).HasMaxLength(100).IsRequired();
        b.Property(x => x.Label).HasMaxLength(300).IsRequired();
        b.HasIndex(x => new { x.ModuleId, x.Code }).IsUnique();
    }
}
