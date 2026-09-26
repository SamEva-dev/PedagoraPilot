using DomainRelay.EFCore;
using DomainRelay.EFCore.Outbox.Abstractions;
using Itech.Emailing.Registration;
using Itech.Emailing.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Domain.Organizations.Events;
using PedagoraPilot.Domain.Catalog.Events;
using PedagoraPilot.Domain.Training.Events;
using PedagoraPilot.Domain.Learning.Events;
using PedagoraPilot.Domain.Training.Delivery.Events;
using PedagoraPilot.Domain.Workplace.Events;
using PedagoraPilot.Domain.Documents.Events;
using PedagoraPilot.Domain.Certification.Events;
using PedagoraPilot.Domain.DistanceLearning.Events;
using PedagoraPilot.Domain.Workforce.Events;
using PedagoraPilot.Infrastructure.Storage;
using PedagoraPilot.Application.Abstractions.Storage;
using PedagoraPilot.Infrastructure.Persistence;
using PedagoraPilot.Infrastructure.Persistence.Idempotency;
using PedagoraPilot.Infrastructure.Persistence.Repositories;
using PedagoraPilot.Infrastructure.Outbox;

namespace PedagoraPilot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default") ?? throw new InvalidOperationException("ConnectionStrings:Default is required.");
        // The outbox uses a singleton factory, so its context options must also be singleton.
        services.AddDbContext<PedagoraPilotDbContext>(optionsLifetime: ServiceLifetime.Singleton);
        services.AddDomainRelayEfCoreOutbox<PedagoraPilotDbContext>(builder =>
        {
            builder.WithDbContextOptions((_, options) => options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(PedagoraPilotDbContext).Assembly.FullName).MigrationsHistoryTable("__EFMigrationsHistory", SchemaNames.Platform)));
            builder.WithOutboxOptions(options =>
            {
                options.Schema = SchemaNames.Integration;
                options.TableName = "outbox_messages";
                options.BatchSize = configuration.GetValue("DomainRelay:Outbox:BatchSize", 100);
                options.PollingInterval = TimeSpan.FromSeconds(configuration.GetValue("DomainRelay:Outbox:PollingIntervalSeconds", 2));
                options.LeaseDuration = TimeSpan.FromSeconds(configuration.GetValue("DomainRelay:Outbox:LeaseDurationSeconds", 30));
                options.MaxAttempts = configuration.GetValue("DomainRelay:Outbox:MaxAttempts", 12);
                options.ProcessedRetention = TimeSpan.FromDays(configuration.GetValue("DomainRelay:Outbox:ProcessedRetentionDays", 14));
            });
            builder.WithTypeRegistry(registry =>
            {
                registry.Register<OrganizationProvisionedDomainEvent>("pedagora.organization.provisioned.v1");
                registry.Register<OrganizationAdministrationUpdatedDomainEvent>("pedagora.organization.administration.updated.v1");
                registry.Register<TrainingSiteCreatedDomainEvent>("pedagora.organization.training-site.created.v1");
                registry.Register<TrainingSiteUpdatedDomainEvent>("pedagora.organization.training-site.updated.v1");
                registry.Register<TrainingProgramCreatedDomainEvent>("pedagora.catalog.program.created.v1");
                registry.Register<TrainingProgramUpdatedDomainEvent>("pedagora.catalog.program.updated.v1");
                registry.Register<ProgramOfferingChangedDomainEvent>("pedagora.catalog.offering.changed.v1");
                registry.Register<ReferentialVersionCreatedDomainEvent>("pedagora.catalog.referential-version.created.v1");
                registry.Register<ReferentialVersionPublishedDomainEvent>("pedagora.catalog.referential-version.published.v1");
                registry.Register<CohortCreatedDomainEvent>("pedagora.training.cohort.created.v1");
                registry.Register<CohortUpdatedDomainEvent>("pedagora.training.cohort.updated.v1");
                registry.Register<PersonCreatedDomainEvent>("pedagora.learning.person.created.v1");
                registry.Register<LearnerProfileCreatedDomainEvent>("pedagora.learning.learner-profile.created.v1");
                registry.Register<EnrollmentCreatedDomainEvent>("pedagora.training.enrollment.created.v1");
                registry.Register<EnrollmentStatusChangedDomainEvent>("pedagora.training.enrollment.status-changed.v1");
                registry.Register<TrainingSessionCreatedDomainEvent>("pedagora.training.session.created.v1");
                registry.Register<TrainingSessionUpdatedDomainEvent>("pedagora.training.session.updated.v1");
                registry.Register<TrainingSessionCancelledDomainEvent>("pedagora.training.session.cancelled.v1");
                registry.Register<AttendanceSheetInitializedDomainEvent>("pedagora.training.attendance.initialized.v1");
                registry.Register<AttendanceRecordedDomainEvent>("pedagora.training.attendance.recorded.v1");
                registry.Register<LearnerCompetencyEvaluatedDomainEvent>("pedagora.learning.competency.evaluated.v1");
                registry.Register<LearnerTopicProgressUpdatedDomainEvent>("pedagora.learning.topic-progress.updated.v1");
                registry.Register<DrivingEvaluationRecordedDomainEvent>("pedagora.learning.driving-evaluation.recorded.v1");
                registry.Register<WorkplacePeriodCreatedDomainEvent>("pedagora.workplace.period.created.v1");
                registry.Register<WorkplacePeriodUpdatedDomainEvent>("pedagora.workplace.period.updated.v1");
                registry.Register<WorkplaceHoursUpdatedDomainEvent>("pedagora.workplace.hours.updated.v1");
                registry.Register<WorkplaceActivityUpdatedDomainEvent>("pedagora.workplace.activity.updated.v1");
                registry.Register<WorkplaceDocumentStatusUpdatedDomainEvent>("pedagora.workplace.document.updated.v1");
                registry.Register<WorkplaceEvaluationRecordedDomainEvent>("pedagora.workplace.evaluation.recorded.v1");
                registry.Register<DocumentCreatedDomainEvent>("pedagora.document.created.v1");
                registry.Register<DocumentVersionAddedDomainEvent>("pedagora.document.version-added.v1");
                registry.Register<DocumentMetadataUpdatedDomainEvent>("pedagora.document.metadata-updated.v1");
                registry.Register<DocumentDeletedDomainEvent>("pedagora.document.deleted.v1");
                registry.Register<CertificationSchemeCreatedDomainEvent>("pedagora.certification.scheme.created.v1");
                registry.Register<CertificationSchemePublishedDomainEvent>("pedagora.certification.scheme.published.v1");
                registry.Register<CertificationSchemeArchivedDomainEvent>("pedagora.certification.scheme.archived.v1");
                registry.Register<CertificationExamSessionCreatedDomainEvent>("pedagora.certification.session.created.v1");
                registry.Register<CertificationExamSessionPlannedDomainEvent>("pedagora.certification.session.planned.v1");
                registry.Register<CertificationCandidateRegisteredDomainEvent>("pedagora.certification.candidate.registered.v1");
                registry.Register<CertificationEligibilityEvaluatedDomainEvent>("pedagora.certification.eligibility.evaluated.v1");
                registry.Register<CertificationAssessmentRecordedDomainEvent>("pedagora.certification.assessment.recorded.v1");
                registry.Register<CertificationDecisionRecordedDomainEvent>("pedagora.certification.decision.recorded.v1");
                registry.Register<CertificationResultsPublishedDomainEvent>("pedagora.certification.results.published.v1");
                registry.Register<DistanceLearningSessionCreatedDomainEvent>("pedagora.distance.session.created.v1");
                registry.Register<DistanceLearningSessionStatusChangedDomainEvent>("pedagora.distance.session.status-changed.v1");
                registry.Register<DistanceParticipantAttendanceUpdatedDomainEvent>("pedagora.distance.participant.attendance-updated.v1");
                registry.Register<AsyncLearningModuleCreatedDomainEvent>("pedagora.distance.module.created.v1");
                registry.Register<AsyncLearningModuleProgressChangedDomainEvent>("pedagora.distance.module.progress-changed.v1");
                registry.Register<RemoteWorkRequestedDomainEvent>("pedagora.workforce.remote-work.requested.v1");
                registry.Register<RemoteWorkDecisionRecordedDomainEvent>("pedagora.workforce.remote-work.decision-recorded.v1");
                registry.Register<RemoteWorkActivityUpdatedDomainEvent>("pedagora.workforce.remote-work.activity-updated.v1");
            });
        });
        // Preserve the outbox's option callbacks while matching the singleton factory lifetime.
        for (var index = 0; index < services.Count; index++)
        {
            var descriptor = services[index];
            if (descriptor.ServiceType == typeof(IDbContextOptionsConfiguration<PedagoraPilotDbContext>) && descriptor.ImplementationFactory is not null)
            {
                services[index] = ServiceDescriptor.Singleton(descriptor.ServiceType, descriptor.ImplementationFactory);
            }
        }

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<ITrainingSiteRepository, TrainingSiteRepository>();
        services.AddScoped<IProgramFamilyRepository, ProgramFamilyRepository>();
        services.AddScoped<ITrainingProgramRepository, TrainingProgramRepository>();
        services.AddScoped<IProgramOfferingRepository, ProgramOfferingRepository>();
        services.AddScoped<IReferentialRepository, ReferentialRepository>();
        services.AddScoped<IReferentialVersionRepository, ReferentialVersionRepository>();
        services.AddScoped<ICohortRepository, CohortRepository>();
        services.AddScoped<IPersonRepository, PersonRepository>();
        services.AddScoped<ILearnerProfileRepository, LearnerProfileRepository>();
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<ITrainingSessionRepository, TrainingSessionRepository>();
        services.AddScoped<IAttendanceSheetRepository, AttendanceSheetRepository>();
        services.AddScoped<ICompetencyDefinitionRepository, CompetencyDefinitionRepository>();
        services.AddScoped<ILearnerCompetencyRecordRepository, LearnerCompetencyRecordRepository>();
        services.AddScoped<IPedagogicalTopicRepository, PedagogicalTopicRepository>();
        services.AddScoped<ILearnerTopicProgressRepository, LearnerTopicProgressRepository>();
        services.AddScoped<IDrivingEvaluationRepository, DrivingEvaluationRepository>();
        services.AddScoped<IWorkplacePeriodRepository, WorkplacePeriodRepository>();
        services.AddScoped<IWorkplaceActivityDefinitionRepository, WorkplaceActivityDefinitionRepository>();
        services.AddScoped<IWorkplaceDocumentRequirementRepository, WorkplaceDocumentRequirementRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<ICertificationSchemeRepository, CertificationSchemeRepository>();
        services.AddScoped<ICertificationExamSessionRepository, CertificationExamSessionRepository>();
        services.AddScoped<ICertificationCandidateRepository, CertificationCandidateRepository>();
        services.AddScoped<IJuryAssignmentRepository, JuryAssignmentRepository>();
        services.AddScoped<IDistanceLearningSessionRepository, DistanceLearningSessionRepository>();
        services.AddScoped<IAsyncLearningModuleRepository, AsyncLearningModuleRepository>();
        services.AddScoped<IRemoteWorkRequestRepository, RemoteWorkRequestRepository>();
        services.AddScoped<IAuditEntryRepository, AuditEntryRepository>();
        services.AddScoped<IReportingReadRepository, ReportingReadRepository>();
        services.Configure<ObjectStorageOptions>(configuration.GetSection(ObjectStorageOptions.SectionName));
        services.AddSingleton<IDocumentStoragePolicy, DocumentStoragePolicy>();
        services.AddSingleton<IObjectStorage, FileSystemObjectStorage>();
        services.AddSingleton<IFileSecurityScanner, NoOpFileSecurityScanner>();
        services.AddScoped<IdempotencyStore>();
        // Itech.Emailing 2.0.1 owns its own EF Core migrations.
        // Keep EmailingDbContext migrations in the Itech.Emailing assembly.
        services.AddItechEmailing(configuration, emailing => emailing.UsePostgres(connectionString));
        services.AddHostedService<EmailDispatcherWorker>();
        // The host (PedagoraPilot.Api) registers IOutboxPublisher because the
        // current transport is SignalR and therefore belongs to the HTTP host.
        services.AddSingleton<IOutboxPublisher, DeferredOutboxPublisher>();
        return services;
    }
}
