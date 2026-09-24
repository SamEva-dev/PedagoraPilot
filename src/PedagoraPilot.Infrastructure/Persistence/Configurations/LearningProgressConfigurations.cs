using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Learning;

namespace PedagoraPilot.Infrastructure.Persistence.Configurations;
public sealed class CompetencyDefinitionConfiguration : IEntityTypeConfiguration<CompetencyDefinition>
{
    public void Configure(EntityTypeBuilder<CompetencyDefinition> b)
    {
        b.ToTable("competency_definitions", SchemaNames.Learning);
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new CompetencyDefinitionId(x)).ValueGeneratedNever();
        b.Property(x => x.ParentId).HasConversion(x => x.HasValue ? x.Value.Value : (Guid? )null, x => x.HasValue ? new CompetencyDefinitionId(x.Value) : null);
        b.Property(x => x.Code).HasMaxLength(80).IsRequired();
        b.Property(x => x.Title).HasMaxLength(300).IsRequired();
        b.Property(x => x.Kind).HasConversion<string>().HasMaxLength(40);
        b.Property(x => x.ExternalKey).HasMaxLength(120);
        b.HasIndex(x => new { x.ReferentialVersionId, x.Code }).IsUnique();
        b.Ignore(x => x.DomainEvents);
    }
}

public sealed class LearnerCompetencyRecordConfiguration : IEntityTypeConfiguration<LearnerCompetencyRecord>
{
    public void Configure(EntityTypeBuilder<LearnerCompetencyRecord> b)
    {
        b.ToTable("learner_competency_records", SchemaNames.Learning);
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new LearnerCompetencyRecordId(x)).ValueGeneratedNever();
        b.Property(x => x.EnrollmentId).HasConversion(x => x.Value, x => new EnrollmentId(x));
        b.Property(x => x.CompetencyDefinitionId).HasConversion(x => x.Value, x => new CompetencyDefinitionId(x));
        b.Property(x => x.Level).HasConversion<string>().HasMaxLength(40);
        b.Property(x => x.Score).HasPrecision(5, 2);
        b.Property(x => x.Comment).HasMaxLength(2000);
        b.Property(x => x.EvaluatorDisplayName).HasMaxLength(200);
        b.HasIndex(x => new { x.EnrollmentId, x.CompetencyDefinitionId }).IsUnique();
        b.Ignore(x => x.DomainEvents);
    }
}

public sealed class PedagogicalTopicConfiguration : IEntityTypeConfiguration<PedagogicalTopic>
{
    public void Configure(EntityTypeBuilder<PedagogicalTopic> b)
    {
        b.ToTable("pedagogical_topics", SchemaNames.Learning);
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new PedagogicalTopicId(x)).ValueGeneratedNever();
        b.Property(x => x.Code).HasMaxLength(80).IsRequired();
        b.Property(x => x.Title).HasMaxLength(300).IsRequired();
        b.Property(x => x.Category).HasMaxLength(80).IsRequired();
        b.Property(x => x.Reference).HasMaxLength(500);
        b.Property(x => x.ExternalKey).HasMaxLength(120);
        b.HasIndex(x => new { x.ReferentialVersionId, x.Code }).IsUnique();
        b.HasIndex(x => new { x.ReferentialVersionId, x.Number });
        b.Ignore(x => x.DomainEvents);
    }
}

public sealed class LearnerTopicProgressConfiguration : IEntityTypeConfiguration<LearnerTopicProgress>
{
    public void Configure(EntityTypeBuilder<LearnerTopicProgress> b)
    {
        b.ToTable("learner_topic_progress", SchemaNames.Learning);
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new LearnerTopicProgressId(x)).ValueGeneratedNever();
        b.Property(x => x.EnrollmentId).HasConversion(x => x.Value, x => new EnrollmentId(x));
        b.Property(x => x.TopicId).HasConversion(x => x.Value, x => new PedagogicalTopicId(x));
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
        b.Property(x => x.EvaluatorDisplayName).HasMaxLength(200);
        b.Property(x => x.Comment).HasMaxLength(2000);
        b.HasIndex(x => new { x.EnrollmentId, x.TopicId }).IsUnique();
        b.Ignore(x => x.DomainEvents);
    }
}

public sealed class DrivingEvaluationConfiguration : IEntityTypeConfiguration<DrivingEvaluation>
{
    public void Configure(EntityTypeBuilder<DrivingEvaluation> b)
    {
        b.ToTable("driving_evaluations", SchemaNames.Learning);
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new DrivingEvaluationId(x)).ValueGeneratedNever();
        b.Property(x => x.EnrollmentId).HasConversion(x => x.Value, x => new EnrollmentId(x));
        b.Property(x => x.CompetencyDefinitionId).HasConversion(x => x.Value, x => new CompetencyDefinitionId(x));
        b.Property(x => x.TrainingSessionId).HasConversion(x => x.HasValue ? x.Value.Value : (Guid? )null, x => x.HasValue ? new TrainingSessionId(x.Value) : null);
        b.Property(x => x.TrainerAuthGateUserId).HasMaxLength(120);
        b.Property(x => x.TrainerDisplayName).HasMaxLength(200).IsRequired();
        b.Property(x => x.Subject).HasMaxLength(500).IsRequired();
        b.Property(x => x.Positive).HasMaxLength(2000);
        b.Property(x => x.Difficulty).HasMaxLength(2000);
        b.Property(x => x.NextGoal).HasMaxLength(2000);
        b.Property(x => x.FreeObservation).HasMaxLength(4000);
        b.HasMany(x => x.Criteria).WithOne().HasForeignKey(x => x.DrivingEvaluationId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Criteria).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Ignore(x => x.DomainEvents);
    }
}

public sealed class DrivingEvaluationCriterionConfiguration : IEntityTypeConfiguration<DrivingEvaluationCriterion>
{
    public void Configure(EntityTypeBuilder<DrivingEvaluationCriterion> b)
    {
        b.ToTable("driving_evaluation_criteria", SchemaNames.Learning);
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new DrivingEvaluationCriterionId(x)).ValueGeneratedNever();
        b.Property(x => x.DrivingEvaluationId).HasConversion(x => x.Value, x => new DrivingEvaluationId(x));
        b.Property(x => x.Code).HasMaxLength(80).IsRequired();
        b.Property(x => x.Label).HasMaxLength(300).IsRequired();
        b.Property(x => x.Level).HasConversion<string>().HasMaxLength(40);
        b.HasIndex(x => new { x.DrivingEvaluationId, x.Code }).IsUnique();
    }
}
