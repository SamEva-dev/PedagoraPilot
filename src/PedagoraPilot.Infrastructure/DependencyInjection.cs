using Amazon;
using Amazon.S3;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
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
using PedagoraPilot.Domain.Access.Events;
using PedagoraPilot.Infrastructure.Storage;
using PedagoraPilot.Application.Abstractions.Storage;
using PedagoraPilot.Infrastructure.Persistence;
using PedagoraPilot.Infrastructure.Persistence.Idempotency;
using PedagoraPilot.Infrastructure.Persistence.Repositories;
using PedagoraPilot.Infrastructure.Outbox;
using PedagoraPilot.Infrastructure.Identity;
using PedagoraPilot.Application.Abstractions.Identity;
using PedagoraPilot.Application.Access.Invitations;
using PedagoraPilot.Application.Access.Members;
using PedagoraPilot.Application.Access.Assignments;
using PedagoraPilot.Infrastructure.Access;

namespace PedagoraPilot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, IHostEnvironment? environment = null)
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
                registry.Register<TrainingSiteDeletedDomainEvent>("pedagora.organization.training-site.deleted.v1");
                registry.Register<TrainingProgramCreatedDomainEvent>("pedagora.catalog.program.created.v1");
                registry.Register<TrainingProgramUpdatedDomainEvent>("pedagora.catalog.program.updated.v1");
                registry.Register<TrainingProgramDeletedDomainEvent>("pedagora.catalog.program.deleted.v1");
                registry.Register<ProgramOfferingChangedDomainEvent>("pedagora.catalog.offering.changed.v1");
                registry.Register<ReferentialVersionCreatedDomainEvent>("pedagora.catalog.referential-version.created.v1");
                registry.Register<ReferentialVersionPublishedDomainEvent>("pedagora.catalog.referential-version.published.v1");
                registry.Register<CohortCreatedDomainEvent>("pedagora.training.cohort.created.v1");
                registry.Register<CohortUpdatedDomainEvent>("pedagora.training.cohort.updated.v1");
                registry.Register<CohortDeletedDomainEvent>("pedagora.training.cohort.deleted.v1");
                registry.Register<CohortTrainerAssignmentChangedDomainEvent>("pedagora.training.cohort-trainer-assignment.changed.v1");
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
                registry.Register<JuryAssignmentCreatedDomainEvent>("pedagora.certification.jury-assignment.created.v1");
                registry.Register<DistanceLearningSessionCreatedDomainEvent>("pedagora.distance.session.created.v1");
                registry.Register<DistanceLearningSessionStatusChangedDomainEvent>("pedagora.distance.session.status-changed.v1");
                registry.Register<DistanceParticipantAddedDomainEvent>("pedagora.distance.participant.added.v1");
                registry.Register<DistanceParticipantAttendanceUpdatedDomainEvent>("pedagora.distance.participant.attendance-updated.v1");
                registry.Register<AsyncLearningModuleCreatedDomainEvent>("pedagora.distance.module.created.v1");
                registry.Register<AsyncLearningModuleProgressChangedDomainEvent>("pedagora.distance.module.progress-changed.v1");
                registry.Register<RemoteWorkRequestedDomainEvent>("pedagora.workforce.remote-work.requested.v1");
                registry.Register<RemoteWorkDecisionRecordedDomainEvent>("pedagora.workforce.remote-work.decision-recorded.v1");
                registry.Register<RemoteWorkActivityUpdatedDomainEvent>("pedagora.workforce.remote-work.activity-updated.v1");
                registry.Register<AccessInvitationCreatedDomainEvent>("pedagora.access.invitation.created.v1");
                registry.Register<AccessInvitationAcceptedDomainEvent>("pedagora.access.invitation.accepted.v1");
                registry.Register<AccessMemberRoleChangedDomainEvent>("pedagora.access.member.role-changed.v1");
                registry.Register<AccessMemberStatusChangedDomainEvent>("pedagora.access.member.status-changed.v1");
                registry.Register<AccessMemberRevokedDomainEvent>("pedagora.access.member.revoked.v1");
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
        services.AddScoped<IOrganizationMemberRepository, OrganizationMemberRepository>();
        services.AddScoped<IAccessInvitationRepository, AccessInvitationRepository>();
        services.AddScoped<IApplicationIdentityAccess, AuthGateApplicationIdentityAccess>();
        services.AddScoped<IStudentMemberProvisioner, StudentMemberProvisioner>();
        services.AddHostedService<StudentMemberProjectionBackfillService>();
        services.AddHostedService<CohortTrainerAssignmentBackfillService>();
        services.AddScoped<IAccessInvitationProcessor, AccessInvitationProcessor>();
        services.AddScoped<ITrainingSiteRepository, TrainingSiteRepository>();
        services.AddScoped<IProgramFamilyRepository, ProgramFamilyRepository>();
        services.AddScoped<ITrainingProgramRepository, TrainingProgramRepository>();
        services.AddScoped<IProgramOfferingRepository, ProgramOfferingRepository>();
        services.AddScoped<IReferentialRepository, ReferentialRepository>();
        services.AddScoped<IReferentialVersionRepository, ReferentialVersionRepository>();
        services.AddScoped<ICohortRepository, CohortRepository>();
        services.AddScoped<ICohortTrainerAssignmentRepository, CohortTrainerAssignmentRepository>();
        services.AddScoped<IUserBusinessAssignmentService, UserBusinessAssignmentService>();
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
        services.Configure<S3StorageOptions>(configuration.GetSection("Storage:S3"));
        services.Configure<ClamAvScannerOptions>(configuration.GetSection("Storage:SecurityScanner"));
        services.AddSingleton<IDocumentStoragePolicy, DocumentStoragePolicy>();
        var provider = configuration["Storage:Provider"] ?? "FileSystem";
        if (provider.Equals("S3", StringComparison.OrdinalIgnoreCase))
        {
            var s3 = configuration.GetSection("Storage:S3").Get<S3StorageOptions>() ?? new();
            if (string.IsNullOrWhiteSpace(s3.Bucket))
                throw new InvalidOperationException("Storage:S3:Bucket is required for S3 storage.");
            if (!string.IsNullOrWhiteSpace(s3.ServiceUrl))
            {
                if (!Uri.TryCreate(s3.ServiceUrl, UriKind.Absolute, out var endpoint) ||
                    (endpoint.Scheme != Uri.UriSchemeHttps && endpoint.Scheme != Uri.UriSchemeHttp))
                    throw new InvalidOperationException("Storage:S3:ServiceUrl must be an absolute HTTP(S) endpoint.");
                // Production must never send object data or credentials over plaintext HTTP.
                if (endpoint.Scheme != Uri.UriSchemeHttps &&
                    environment?.IsDevelopment() != true)
                    throw new InvalidOperationException("S3 custom endpoints require HTTPS outside Development.");
            }
            services.AddSingleton<IAmazonS3>(_ =>
            {
                var config = new AmazonS3Config { ForcePathStyle = s3.ForcePathStyle };
                if (!string.IsNullOrWhiteSpace(s3.ServiceUrl))
                {
                    config.ServiceURL = s3.ServiceUrl;
                    config.AuthenticationRegion = s3.Region;
                }
                else config.RegionEndpoint = RegionEndpoint.GetBySystemName(s3.Region);
                return new AmazonS3Client(config);
            });
            services.AddSingleton<IObjectStorage, S3ObjectStorage>();
        }
        else if (provider.Equals("FileSystem", StringComparison.OrdinalIgnoreCase))
            services.AddSingleton<IObjectStorage, FileSystemObjectStorage>();
        else throw new InvalidOperationException($"Unsupported Storage:Provider '{provider}'.");

        var scanner = configuration["Storage:SecurityScanner:Provider"] ?? "NoOp";
        if (scanner.Equals("ClamAV", StringComparison.OrdinalIgnoreCase))
        {
            var opts = configuration.GetSection("Storage:SecurityScanner").Get<ClamAvScannerOptions>() ?? new();
            if (string.IsNullOrWhiteSpace(opts.Host) || opts.Port is < 1 or > 65535 || opts.TimeoutSeconds is < 1 or > 300)
                throw new InvalidOperationException("Invalid Storage:SecurityScanner ClamAV endpoint/timeout.");
            services.AddSingleton<IFileSecurityScanner, ClamAvFileSecurityScanner>();
        }
        else if (scanner.Equals("NoOp", StringComparison.OrdinalIgnoreCase))
            services.AddSingleton<IFileSecurityScanner, NoOpFileSecurityScanner>();
        else throw new InvalidOperationException($"Unsupported Storage:SecurityScanner:Provider '{scanner}'.");
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
