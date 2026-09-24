using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PedagoraPilot.Domain.Certification;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Infrastructure.Persistence.Configurations;
public sealed class CertificationSchemeConfiguration : IEntityTypeConfiguration<CertificationScheme>
{
    public void Configure(EntityTypeBuilder<CertificationScheme> b)
    {
        b.ToTable("schemes", SchemaNames.Certification);
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new CertificationSchemeId(x)).ValueGeneratedNever();
        b.Property(x => x.Code).HasMaxLength(80).IsRequired();
        b.Property(x => x.Name).HasMaxLength(240).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => new { x.ReferentialVersionId, x.Code }).IsUnique();
        b.Navigation(x => x.Units).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(x => x.Steps).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.HasMany(x => x.Units).WithOne().HasForeignKey(x => x.SchemeId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Steps).WithOne().HasForeignKey(x => x.SchemeId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CertificationUnitConfiguration : IEntityTypeConfiguration<CertificationUnit>
{
    public void Configure(EntityTypeBuilder<CertificationUnit> b)
    {
        b.ToTable("scheme_units", SchemaNames.Certification);
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new CertificationUnitId(x)).ValueGeneratedNever();
        b.Property(x => x.SchemeId).HasConversion(x => x.Value, x => new CertificationSchemeId(x));
        b.Property(x => x.Code).HasMaxLength(80).IsRequired();
        b.Property(x => x.Title).HasMaxLength(300).IsRequired();
        b.HasIndex(x => new { x.SchemeId, x.Code }).IsUnique();
    }
}

public sealed class CertificationStepDefinitionConfiguration : IEntityTypeConfiguration<CertificationStepDefinition>
{
    public void Configure(EntityTypeBuilder<CertificationStepDefinition> b)
    {
        b.ToTable("step_definitions", SchemaNames.Certification);
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new CertificationStepDefinitionId(x)).ValueGeneratedNever();
        b.Property(x => x.SchemeId).HasConversion(x => x.Value, x => new CertificationSchemeId(x));
        b.Property(x => x.UnitId).HasConversion(x => x.HasValue ? x.Value.Value : (Guid? )null, x => x.HasValue ? new CertificationUnitId(x.Value) : null);
        b.Property(x => x.Code).HasMaxLength(100).IsRequired();
        b.Property(x => x.Title).HasMaxLength(300).IsRequired();
        b.Property(x => x.Kind).HasConversion<string>().HasMaxLength(48);
        b.HasIndex(x => new { x.SchemeId, x.Code }).IsUnique();
    }
}

public sealed class CertificationExamSessionConfiguration : IEntityTypeConfiguration<CertificationExamSession>
{
    public void Configure(EntityTypeBuilder<CertificationExamSession> b)
    {
        b.ToTable("exam_sessions", SchemaNames.Certification);
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new CertificationExamSessionId(x)).ValueGeneratedNever();
        b.Property(x => x.CohortId).HasConversion(x => x.Value, x => new CohortId(x));
        b.Property(x => x.SchemeId).HasConversion(x => x.Value, x => new CertificationSchemeId(x));
        b.Property(x => x.Title).HasMaxLength(240).IsRequired();
        b.Property(x => x.Venue).HasMaxLength(240);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => new { x.OrganizationId, x.CohortId, x.StartsAtUtc });
    }
}

public sealed class CertificationCandidateConfiguration : IEntityTypeConfiguration<CertificationCandidate>
{
    public void Configure(EntityTypeBuilder<CertificationCandidate> b)
    {
        b.ToTable("candidates", SchemaNames.Certification);
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new CertificationCandidateId(x)).ValueGeneratedNever();
        b.Property(x => x.ExamSessionId).HasConversion(x => x.Value, x => new CertificationExamSessionId(x));
        b.Property(x => x.EnrollmentId).HasConversion(x => x.Value, x => new EnrollmentId(x));
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        b.Property(x => x.Decision).HasConversion<string>().HasMaxLength(32);
        b.Property(x => x.EligibilitySnapshotJson).HasColumnType("jsonb");
        b.Property(x => x.DecisionComment).HasMaxLength(4000);
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => new { x.ExamSessionId, x.EnrollmentId }).IsUnique();
        b.Navigation(x => x.Assessments).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.HasMany(x => x.Assessments).WithOne().HasForeignKey(x => x.CandidateId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CertificationAssessmentConfiguration : IEntityTypeConfiguration<CertificationAssessment>
{
    public void Configure(EntityTypeBuilder<CertificationAssessment> b)
    {
        b.ToTable("assessments", SchemaNames.Certification);
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new CertificationAssessmentId(x)).ValueGeneratedNever();
        b.Property(x => x.CandidateId).HasConversion(x => x.Value, x => new CertificationCandidateId(x));
        b.Property(x => x.StepDefinitionId).HasConversion(x => x.Value, x => new CertificationStepDefinitionId(x));
        b.Property(x => x.JuryDisplayName).HasMaxLength(240).IsRequired();
        b.Property(x => x.Outcome).HasConversion<string>().HasMaxLength(32);
        b.Property(x => x.Comment).HasMaxLength(4000);
        b.HasIndex(x => new { x.CandidateId, x.StepDefinitionId }).IsUnique();
    }
}

public sealed class JuryAssignmentConfiguration : IEntityTypeConfiguration<JuryAssignment>
{
    public void Configure(EntityTypeBuilder<JuryAssignment> b)
    {
        b.ToTable("jury_assignments", SchemaNames.Certification);
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(x => x.Value, x => new JuryAssignmentId(x)).ValueGeneratedNever();
        b.Property(x => x.ExamSessionId).HasConversion(x => x.Value, x => new CertificationExamSessionId(x));
        b.Property(x => x.DisplayName).HasMaxLength(240).IsRequired();
        b.Property(x => x.Role).HasMaxLength(80).IsRequired();
        b.HasIndex(x => new { x.ExamSessionId, x.AuthGateUserId }).IsUnique();
    }
}
