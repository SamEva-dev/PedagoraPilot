using DomainRelay.EFCore.Outbox;
using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Domain.Catalog;
using PedagoraPilot.Domain.Learning;
using PedagoraPilot.Domain.Organizations;
using PedagoraPilot.Domain.Training;
using PedagoraPilot.Domain.Training.Delivery;
using PedagoraPilot.Domain.Workplace;
using PedagoraPilot.Domain.Documents;
using PedagoraPilot.Domain.Certification;
using PedagoraPilot.Domain.DistanceLearning;
using PedagoraPilot.Domain.Workforce;
using PedagoraPilot.Domain.Audit;
using PedagoraPilot.Infrastructure.Persistence.Catalog;
using PedagoraPilot.Infrastructure.Persistence.Idempotency;

namespace PedagoraPilot.Infrastructure.Persistence;
public sealed class PedagoraPilotDbContext : DbContext
{
    public PedagoraPilotDbContext(DbContextOptions<PedagoraPilotDbContext> options) : base(options)
    {
    }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<TrainingSite> TrainingSites => Set<TrainingSite>();
    public DbSet<IdempotencyRequest> IdempotencyRequests => Set<IdempotencyRequest>();
    public DbSet<ProgramFamily> ProgramFamilies => Set<ProgramFamily>();
    public DbSet<TrainingProgram> TrainingPrograms => Set<TrainingProgram>();
    public DbSet<ProgramOffering> ProgramOfferings => Set<ProgramOffering>();
    public DbSet<Referential> Referentials => Set<Referential>();
    public DbSet<ReferentialVersion> ReferentialVersions => Set<ReferentialVersion>();
    public DbSet<ProgramCapabilityRow> ProgramCapabilities => Set<ProgramCapabilityRow>();
    public DbSet<ReferentialVersionCapabilityRow> ReferentialVersionCapabilities => Set<ReferentialVersionCapabilityRow>();
    public DbSet<Cohort> Cohorts => Set<Cohort>();
    public DbSet<Person> People => Set<Person>();
    public DbSet<LearnerProfile> LearnerProfiles => Set<LearnerProfile>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<TrainingSession> TrainingSessions => Set<TrainingSession>();
    public DbSet<AttendanceSheet> AttendanceSheets => Set<AttendanceSheet>();
    public DbSet<AttendanceEntry> AttendanceEntries => Set<AttendanceEntry>();
    public DbSet<TrainingSessionParticipant> TrainingSessionParticipants => Set<TrainingSessionParticipant>();
    public DbSet<CompetencyDefinition> CompetencyDefinitions => Set<CompetencyDefinition>();
    public DbSet<LearnerCompetencyRecord> LearnerCompetencyRecords => Set<LearnerCompetencyRecord>();
    public DbSet<PedagogicalTopic> PedagogicalTopics => Set<PedagogicalTopic>();
    public DbSet<LearnerTopicProgress> LearnerTopicProgressRows => Set<LearnerTopicProgress>();
    public DbSet<LearnerTopicEvaluationCriterion> LearnerTopicEvaluationCriteria => Set<LearnerTopicEvaluationCriterion>();
    public DbSet<DrivingEvaluation> DrivingEvaluations => Set<DrivingEvaluation>();
    public DbSet<DrivingEvaluationCriterion> DrivingEvaluationCriteria => Set<DrivingEvaluationCriterion>();
    public DbSet<WorkplacePeriod> WorkplacePeriods => Set<WorkplacePeriod>();
    public DbSet<WorkplaceActivity> WorkplaceActivities => Set<WorkplaceActivity>();
    public DbSet<WorkplaceDocumentChecklistItem> WorkplaceDocumentChecklist => Set<WorkplaceDocumentChecklistItem>();
    public DbSet<WorkplaceEvaluation> WorkplaceEvaluations => Set<WorkplaceEvaluation>();
    public DbSet<WorkplaceActivityDefinition> WorkplaceActivityDefinitions => Set<WorkplaceActivityDefinition>();
    public DbSet<WorkplaceDocumentRequirement> WorkplaceDocumentRequirements => Set<WorkplaceDocumentRequirement>();
    public DbSet<ManagedDocument> Documents => Set<ManagedDocument>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    public DbSet<CertificationScheme> CertificationSchemes => Set<CertificationScheme>();
    public DbSet<CertificationUnit> CertificationUnits => Set<CertificationUnit>();
    public DbSet<CertificationStepDefinition> CertificationStepDefinitions => Set<CertificationStepDefinition>();
    public DbSet<CertificationExamSession> CertificationExamSessions => Set<CertificationExamSession>();
    public DbSet<CertificationCandidate> CertificationCandidates => Set<CertificationCandidate>();
    public DbSet<CertificationAssessment> CertificationAssessments => Set<CertificationAssessment>();
    public DbSet<JuryAssignment> JuryAssignments => Set<JuryAssignment>();
    public DbSet<DistanceLearningSession> DistanceLearningSessions => Set<DistanceLearningSession>();
    public DbSet<DistanceParticipant> DistanceParticipants => Set<DistanceParticipant>();
    public DbSet<AsyncLearningModule> AsyncLearningModules => Set<AsyncLearningModule>();
    public DbSet<AsyncModuleStep> AsyncModuleSteps => Set<AsyncModuleStep>();
    public DbSet<RemoteWorkRequest> RemoteWorkRequests => Set<RemoteWorkRequest>();
    public DbSet<RemoteWorkActivity> RemoteWorkActivities => Set<RemoteWorkActivity>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PedagoraPilotDbContext).Assembly);
        modelBuilder.AddDomainRelayOutbox("outbox_messages", SchemaNames.Integration);
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        Touch<Organization>(now);
        Touch<TrainingSite>(now);
        Touch<ProgramFamily>(now);
        Touch<TrainingProgram>(now);
        Touch<ProgramOffering>(now);
        Touch<Referential>(now);
        Touch<ReferentialVersion>(now);
        Touch<Cohort>(now);
        Touch<Person>(now);
        Touch<LearnerProfile>(now);
        Touch<Enrollment>(now);
        Touch<TrainingSession>(now);
        Touch<AttendanceSheet>(now);
        Touch<CompetencyDefinition>(now);
        Touch<LearnerCompetencyRecord>(now);
        Touch<PedagogicalTopic>(now);
        Touch<LearnerTopicProgress>(now);
        Touch<DrivingEvaluation>(now);
        Touch<WorkplacePeriod>(now);
        Touch<WorkplaceActivityDefinition>(now);
        Touch<WorkplaceDocumentRequirement>(now);
        Touch<ManagedDocument>(now);
        Touch<CertificationScheme>(now);
        Touch<CertificationExamSession>(now);
        Touch<CertificationCandidate>(now);
        Touch<DistanceLearningSession>(now);
        Touch<AsyncLearningModule>(now);
        Touch<RemoteWorkRequest>(now);
        SyncCapabilities();
        return base.SaveChangesAsync(ct);
    }

    private void Touch<T>(DateTime now)
        where T : class
    {
        foreach (var e in ChangeTracker.Entries<T>().Where(e => e.State == EntityState.Modified))
        {
            var updated = e.Metadata.FindProperty("UpdatedAtUtc");
            if (updated is not null)
                e.Property("UpdatedAtUtc").CurrentValue = now;
            var version = e.Metadata.FindProperty("Version");
            if (version is not null)
            {
                var original = Convert.ToInt64(e.Property("Version").OriginalValue ?? 0L);
                e.Property("Version").CurrentValue = original + 1;
            }
        }
    }

    private void SyncCapabilities()
    {
        foreach (var e in ChangeTracker.Entries<TrainingProgram>().Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            var id = e.Entity.Id;
            ProgramCapabilities.RemoveRange(ProgramCapabilities.Where(x => x.ProgramId == id));
            foreach (var c in e.Entity.Capabilities)
                ProgramCapabilities.Add(new ProgramCapabilityRow { ProgramId = id, CapabilityCode = c });
        }

        foreach (var e in ChangeTracker.Entries<ReferentialVersion>().Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            var id = e.Entity.Id;
            ReferentialVersionCapabilities.RemoveRange(ReferentialVersionCapabilities.Where(x => x.ReferentialVersionId == id));
            foreach (var c in e.Entity.Capabilities)
                ReferentialVersionCapabilities.Add(new ReferentialVersionCapabilityRow { ReferentialVersionId = id, CapabilityCode = c });
        }
    }
}
