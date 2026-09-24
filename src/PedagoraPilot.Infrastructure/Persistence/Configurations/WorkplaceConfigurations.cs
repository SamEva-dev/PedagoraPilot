using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Domain.Workplace;

namespace PedagoraPilot.Infrastructure.Persistence.Configurations;
public sealed class WorkplacePeriodConfiguration : IEntityTypeConfiguration<WorkplacePeriod>
{
    public void Configure(EntityTypeBuilder<WorkplacePeriod> b)
    {
        b.ToTable("periods", SchemaNames.Workplace);
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new WorkplacePeriodId(x)).ValueGeneratedNever();
        b.Property(x => x.EnrollmentId).HasConversion(x => x.Value, x => new EnrollmentId(x));
        b.Property(x => x.CohortId).HasConversion(x => x.Value, x => new CohortId(x));
        b.Property(x => x.PeriodTypeCode).HasMaxLength(64).IsRequired();
        b.Property(x => x.Company).HasMaxLength(200).IsRequired();
        b.Property(x => x.City).HasMaxLength(120).IsRequired();
        b.Property(x => x.TutorName).HasMaxLength(200).IsRequired();
        b.Property(x => x.TutorEmail).HasMaxLength(250);
        b.Property(x => x.TutorPhone).HasMaxLength(60);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
        b.Property(x => x.Notes).HasMaxLength(4000);
        b.Property(x => x.TutorObservation).HasMaxLength(4000);
        b.HasIndex(x => new { x.OrganizationId, x.CohortId });
        b.HasIndex(x => new { x.EnrollmentId, x.StartDate, x.EndDate });
        b.HasMany(x => x.Activities).WithOne().HasForeignKey(x => x.PeriodId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Activities).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.HasMany(x => x.Documents).WithOne().HasForeignKey(x => x.PeriodId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Documents).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.HasMany(x => x.Evaluations).WithOne().HasForeignKey(x => x.PeriodId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Evaluations).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Ignore(x => x.DomainEvents);
    }
}

public sealed class WorkplaceActivityConfiguration : IEntityTypeConfiguration<WorkplaceActivity>
{
    public void Configure(EntityTypeBuilder<WorkplaceActivity> b)
    {
        b.ToTable("activities", SchemaNames.Workplace);
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new WorkplaceActivityId(x)).ValueGeneratedNever();
        b.Property(x => x.PeriodId).HasConversion(x => x.Value, x => new WorkplacePeriodId(x));
        b.Property(x => x.DefinitionId).HasConversion(x => x.Value, x => new WorkplaceActivityDefinitionId(x));
        b.Property(x => x.Code).HasMaxLength(100).IsRequired();
        b.Property(x => x.Title).HasMaxLength(300).IsRequired();
        b.Property(x => x.LabelKey).HasMaxLength(200);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
        b.Property(x => x.Comment).HasMaxLength(2000);
        b.HasIndex(x => new { x.PeriodId, x.DefinitionId }).IsUnique();
    }
}

public sealed class WorkplaceDocumentChecklistItemConfiguration : IEntityTypeConfiguration<WorkplaceDocumentChecklistItem>
{
    public void Configure(EntityTypeBuilder<WorkplaceDocumentChecklistItem> b)
    {
        b.ToTable("document_checklist", SchemaNames.Workplace);
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new WorkplaceDocumentChecklistItemId(x)).ValueGeneratedNever();
        b.Property(x => x.PeriodId).HasConversion(x => x.Value, x => new WorkplacePeriodId(x));
        b.Property(x => x.RequirementId).HasConversion(x => x.Value, x => new WorkplaceDocumentRequirementId(x));
        b.Property(x => x.Code).HasMaxLength(100).IsRequired();
        b.Property(x => x.Title).HasMaxLength(300).IsRequired();
        b.Property(x => x.LabelKey).HasMaxLength(200);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
        b.HasIndex(x => new { x.PeriodId, x.RequirementId }).IsUnique();
    }
}

public sealed class WorkplaceEvaluationConfiguration : IEntityTypeConfiguration<WorkplaceEvaluation>
{
    public void Configure(EntityTypeBuilder<WorkplaceEvaluation> b)
    {
        b.ToTable("evaluations", SchemaNames.Workplace);
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new WorkplaceEvaluationId(x)).ValueGeneratedNever();
        b.Property(x => x.PeriodId).HasConversion(x => x.Value, x => new WorkplacePeriodId(x));
        b.Property(x => x.Kind).HasConversion<string>().HasMaxLength(40);
        b.Property(x => x.EvaluatorDisplayName).HasMaxLength(200).IsRequired();
        b.Property(x => x.Summary).HasMaxLength(4000).IsRequired();
        b.Property(x => x.Strengths).HasMaxLength(4000);
        b.Property(x => x.ImprovementAreas).HasMaxLength(4000);
    }
}

public sealed class WorkplaceActivityDefinitionConfiguration : IEntityTypeConfiguration<WorkplaceActivityDefinition>
{
    public void Configure(EntityTypeBuilder<WorkplaceActivityDefinition> b)
    {
        b.ToTable("activity_definitions", SchemaNames.Workplace);
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new WorkplaceActivityDefinitionId(x)).ValueGeneratedNever();
        b.Property(x => x.PeriodTypeCode).HasMaxLength(64).IsRequired();
        b.Property(x => x.Code).HasMaxLength(100).IsRequired();
        b.Property(x => x.Title).HasMaxLength(300).IsRequired();
        b.Property(x => x.LabelKey).HasMaxLength(200);
        b.HasIndex(x => new { x.ReferentialVersionId, x.PeriodTypeCode, x.Code }).IsUnique();
        b.Ignore(x => x.DomainEvents);
    }
}

public sealed class WorkplaceDocumentRequirementConfiguration : IEntityTypeConfiguration<WorkplaceDocumentRequirement>
{
    public void Configure(EntityTypeBuilder<WorkplaceDocumentRequirement> b)
    {
        b.ToTable("document_requirements", SchemaNames.Workplace);
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new WorkplaceDocumentRequirementId(x)).ValueGeneratedNever();
        b.Property(x => x.PeriodTypeCode).HasMaxLength(64).IsRequired();
        b.Property(x => x.Code).HasMaxLength(100).IsRequired();
        b.Property(x => x.Title).HasMaxLength(300).IsRequired();
        b.Property(x => x.LabelKey).HasMaxLength(200);
        b.HasIndex(x => new { x.ReferentialVersionId, x.PeriodTypeCode, x.Code }).IsUnique();
        b.Ignore(x => x.DomainEvents);
    }
}
