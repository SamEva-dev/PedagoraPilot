using DomainRelay.Mapping.Abstractions.Configuration;
using DomainRelay.Mapping.Abstractions.Profiles;
using PedagoraPilot.Domain.Certification;

namespace PedagoraPilot.Application.Mapping;
public sealed class CertificationMappingProfile : MappingProfile
{
    public override void Configure(IMappingConfiguration configuration)
    {
        configuration.CreateMap<CertificationScheme, CertificationSchemeReadModel>();
        configuration.CreateMap<CertificationExamSession, CertificationExamSessionReadModel>();
        configuration.CreateMap<CertificationCandidate, CertificationCandidateReadModel>();
        configuration.CreateMap<CertificationAssessment, CertificationAssessmentReadModel>();
        configuration.CreateMap<JuryAssignment, JuryAssignmentReadModel>();
    }
}

public sealed class CertificationSchemeReadModel
{
    public PedagoraPilot.Domain.Identifiers.CertificationSchemeId Id { get; set; }
    public Guid ReferentialVersionId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public CertificationSchemeStatus Status { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
}

public sealed class CertificationExamSessionReadModel
{
    public PedagoraPilot.Domain.Identifiers.CertificationExamSessionId Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid SiteId { get; set; }
    public PedagoraPilot.Domain.Identifiers.CohortId CohortId { get; set; }
    public PedagoraPilot.Domain.Identifiers.CertificationSchemeId SchemeId { get; set; }
    public string Title { get; set; } = "";
    public DateTimeOffset StartsAtUtc { get; set; }
    public DateTimeOffset EndsAtUtc { get; set; }
    public string? Venue { get; set; }
    public CertificationExamSessionStatus Status { get; set; }
}

public sealed class CertificationCandidateReadModel
{
    public PedagoraPilot.Domain.Identifiers.CertificationCandidateId Id { get; set; }
    public PedagoraPilot.Domain.Identifiers.CertificationExamSessionId ExamSessionId { get; set; }
    public PedagoraPilot.Domain.Identifiers.EnrollmentId EnrollmentId { get; set; }
    public CertificationCandidateStatus Status { get; set; }
    public bool? Eligible { get; set; }
    public CertificationDecision Decision { get; set; }
    public string? DecisionComment { get; set; }
    public DateTime? DecisionAtUtc { get; set; }
}

public sealed class CertificationAssessmentReadModel
{
    public PedagoraPilot.Domain.Identifiers.CertificationAssessmentId Id { get; set; }
    public PedagoraPilot.Domain.Identifiers.CertificationStepDefinitionId StepDefinitionId { get; set; }
    public string JuryDisplayName { get; set; } = "";
    public CertificationAssessmentOutcome Outcome { get; set; }
    public decimal? Score { get; set; }
    public string? Comment { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; }
}

public sealed class JuryAssignmentReadModel
{
    public PedagoraPilot.Domain.Identifiers.JuryAssignmentId Id { get; set; }
    public PedagoraPilot.Domain.Identifiers.CertificationExamSessionId ExamSessionId { get; set; }
    public Guid AuthGateUserId { get; set; }
    public string DisplayName { get; set; } = "";
    public string Role { get; set; } = "";
    public DateTimeOffset AssignedAtUtc { get; set; }
}
