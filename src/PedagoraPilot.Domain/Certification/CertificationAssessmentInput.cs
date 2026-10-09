using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Domain.Certification;

public sealed record CertificationAssessmentInput(
    CertificationStepDefinitionId StepDefinitionId,
    string JuryDisplayName,
    CertificationAssessmentOutcome Outcome,
    decimal? Score,
    string? Comment);
