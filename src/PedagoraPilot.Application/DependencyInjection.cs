using DomainRelay.Abstractions;
using DomainRelay.DependencyInjection;
using DomainRelay.Diagnostics;
using DomainRelay.Mapping.DependencyInjection.Extensions;
using DomainRelay.Validation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PedagoraPilot.Application.Common.Behaviors;
using PedagoraPilot.Application.Mapping;
using PedagoraPilot.Application.Training.Delivery;
using PedagoraPilot.Application.Organizations.Provision;
using PedagoraPilot.Application.Catalog.Programs;
using PedagoraPilot.Application.Catalog.Referentials;
using PedagoraPilot.Application.Training.Cohorts;
using PedagoraPilot.Application.Training.Learners;
using PedagoraPilot.Application.Learning.Progress;
using PedagoraPilot.Application.Workplace;
using PedagoraPilot.Application.Documents;
using PedagoraPilot.Application.Certification;
using PedagoraPilot.Application.DistanceLearning;
using PedagoraPilot.Application.Workforce;

namespace PedagoraPilot.Application;
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;
        services.AddDomainRelay(configureOptions: _ =>
        {
        }, configureRegistration: registration =>
        {
            registration.Assemblies.Add(assembly);
            registration.EnableAssemblyScanning = false;
        });
        // Register closed handlers separately from the explicitly ordered open-generic behaviors.
        foreach (var implementation in assembly.GetTypes().Where(type => type is { IsAbstract: false, IsInterface: false, ContainsGenericParameters: false }))
        {
            foreach (var service in implementation.GetInterfaces().Where(type => type.IsGenericType))
            {
                var definition = service.GetGenericTypeDefinition();
                if (definition == typeof(IRequestHandler<, >) || definition == typeof(INotificationHandler<>))
                    services.AddTransient(service, implementation);
            }
        }

        services.AddDomainRelayValidation();
        services.AddDomainRelayDiagnostics();
        services.AddTransient(typeof(IPipelineBehavior<, >), typeof(UnitOfWorkBehavior<, >));
        services.AddTransient(typeof(IPipelineBehavior<, >), typeof(AuditBehavior<, >));
        services.AddTransient<IValidator<ProvisionOrganizationCommand>, ProvisionOrganizationCommandValidator>();
        services.AddTransient<IValidator<CreateProgramCommand>, CreateProgramCommandValidator>();
        services.AddTransient<IValidator<UpdateProgramCommand>, UpdateProgramCommandValidator>();
        services.AddTransient<IValidator<CreateReferentialVersionCommand>, CreateReferentialVersionCommandValidator>();
        services.AddTransient<IValidator<CreateCohortCommand>, CreateCohortCommandValidator>();
        services.AddTransient<IValidator<UpdateCohortCommand>, UpdateCohortCommandValidator>();
        services.AddTransient<IValidator<EnrollLearnerCommand>, EnrollLearnerCommandValidator>();
        services.AddTransient<IValidator<CreateTrainingSessionCommand>, CreateTrainingSessionCommandValidator>();
        services.AddTransient<IValidator<UpdateTrainingSessionCommand>, UpdateTrainingSessionCommandValidator>();
        services.AddTransient<IValidator<SaveAttendanceCommand>, SaveAttendanceCommandValidator>();
        services.AddTransient<IValidator<EvaluateCompetencyCommand>, EvaluateCompetencyCommandValidator>();
        services.AddTransient<IValidator<UpdateTopicProgressCommand>, UpdateTopicProgressCommandValidator>();
        services.AddTransient<IValidator<RecordDrivingEvaluationCommand>, RecordDrivingEvaluationCommandValidator>();
        services.AddTransient<IValidator<CreateWorkplacePeriodCommand>, CreateWorkplacePeriodCommandValidator>();
        services.AddTransient<IValidator<UpdateWorkplacePeriodCommand>, UpdateWorkplacePeriodCommandValidator>();
        services.AddTransient<IValidator<UpdateWorkplaceHoursCommand>, UpdateWorkplaceHoursCommandValidator>();
        services.AddTransient<IValidator<RecordWorkplaceEvaluationCommand>, RecordWorkplaceEvaluationCommandValidator>();
        services.AddTransient<IValidator<UploadDocumentCommand>, UploadDocumentCommandValidator>();
        services.AddTransient<IValidator<ReplaceDocumentVersionCommand>, ReplaceDocumentVersionCommandValidator>();
        services.AddTransient<IValidator<UpdateDocumentMetadataCommand>, UpdateDocumentMetadataCommandValidator>();
        services.AddTransient<IValidator<CreateDistanceLearningSessionCommand>, CreateDistanceLearningSessionCommandValidator>();
        services.AddTransient<IValidator<CreateAsyncLearningModuleCommand>, CreateAsyncLearningModuleCommandValidator>();
        services.AddTransient<IValidator<UpdateAsyncModuleProgressCommand>, UpdateAsyncModuleProgressCommandValidator>();
        services.AddTransient<IValidator<CreateRemoteWorkCommand>, CreateRemoteWorkCommandValidator>();
        services.AddDomainRelayMapping(builder =>
        {
            builder.AddProfile<OrganizationMappingProfile>();
            builder.AddProfile<OrganizationWorkspaceMappingProfile>();
            builder.AddProfile<CatalogMappingProfile>();
            builder.AddProfile<TrainingMappingProfile>();
            builder.AddProfile<LearningProgressMappingProfile>();
            builder.AddProfile<WorkplaceMappingProfile>();
            builder.AddProfile<DocumentMappingProfile>();
            builder.AddProfile<CertificationMappingProfile>();
            builder.AddProfile<DistanceLearningMappingProfile>();
            builder.AddProfile<WorkforceMappingProfile>();
            builder.ValidateConfigurationOnBuild();
        });
        return services;
    }
}
