using DomainRelay.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PedagoraPilot.Api.Authorization;
using PedagoraPilot.Application.Certification;
using PedagoraPilot.Contracts.Certification;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Security.Contracts;

namespace PedagoraPilot.Api.Controllers;
[ApiController]
[Route("api/v1/certification")]
[Authorize]
public sealed class CertificationController(IMediator mediator) : ControllerBase
{
    [HttpGet("schemes")]
    [HasPermission(PedagoraPilotPermissionCodes.Certification.View)]
    public Task<IReadOnlyCollection<CertificationSchemeDto>> Schemes([FromQuery] Guid? referentialVersionId, CancellationToken ct) => mediator.Send(new GetCertificationSchemesQuery(referentialVersionId), ct);
    [HttpGet("sessions")]
    [HasPermission(PedagoraPilotPermissionCodes.Certification.View)]
    public Task<IReadOnlyCollection<CertificationExamSessionDto>> Sessions([FromQuery] Guid? cohortId, CancellationToken ct) => mediator.Send(new GetCertificationExamSessionsQuery(cohortId.HasValue ? new CohortId(cohortId.Value) : null), ct);
    [HttpPost("sessions")]
    [HasPermission(PedagoraPilotPermissionCodes.Certification.Manage)]
    public Task<CertificationExamSessionDto> Create(CreateCertificationExamSessionRequest r, CancellationToken ct) => mediator.Send(new CreateCertificationExamSessionCommand(new CohortId(r.CohortId), new CertificationSchemeId(r.SchemeId), r.Title, r.StartsAtUtc, r.EndsAtUtc, r.Venue), ct);
    [HttpPost("sessions/{id:guid}/plan")]
    [HasPermission(PedagoraPilotPermissionCodes.Certification.Manage)]
    public Task<CertificationExamSessionDto> Plan(Guid id, CancellationToken ct) => mediator.Send(new PlanCertificationExamSessionCommand(new CertificationExamSessionId(id)), ct);
    [HttpPost("sessions/{id:guid}/candidates/from-cohort")]
    [HasPermission(PedagoraPilotPermissionCodes.Certification.Manage)]
    public Task<int> RegisterCandidates(Guid id, CancellationToken ct) => mediator.Send(new RegisterCohortCandidatesCommand(new CertificationExamSessionId(id)), ct);
    [HttpGet("sessions/{id:guid}/candidates")]
    [HasPermission(PedagoraPilotPermissionCodes.Certification.View)]
    public Task<IReadOnlyCollection<CertificationCandidateDto>> Candidates(Guid id, CancellationToken ct) => mediator.Send(new GetCertificationCandidatesQuery(new CertificationExamSessionId(id)), ct);
    [HttpPost("sessions/{id:guid}/jury")]
    [HasPermission(PedagoraPilotPermissionCodes.Jury.Evaluate)]
    public Task<JuryAssignmentDto> AssignJury(Guid id, AssignJuryRequest r, CancellationToken ct) => mediator.Send(new AssignJuryCommand(new CertificationExamSessionId(id), r.AuthGateUserId, r.DisplayName, r.Role), ct);
    [HttpPut("candidates/{id:guid}/eligibility")]
    [HasPermission(PedagoraPilotPermissionCodes.Certification.Manage)]
    public Task<CertificationCandidateDto> Eligibility(Guid id, EvaluateEligibilityRequest r, CancellationToken ct) => mediator.Send(new EvaluateCertificationEligibilityCommand(new CertificationCandidateId(id), r.Eligible, r.Blockers ?? []), ct);
    [HttpPost("candidates/{id:guid}/assessments")]
    [HasPermission(PedagoraPilotPermissionCodes.Jury.Evaluate)]
    public Task<CertificationCandidateDto> Assessment(Guid id, RecordCertificationAssessmentRequest r, CancellationToken ct) => mediator.Send(new RecordCertificationAssessmentCommand(new CertificationCandidateId(id), new CertificationStepDefinitionId(r.StepDefinitionId), r.JuryDisplayName, r.Outcome, r.Score, r.Comment), ct);
    [HttpPut("candidates/{id:guid}/decision")]
    [HasPermission(PedagoraPilotPermissionCodes.Results.Manage)]
    public Task<CertificationCandidateDto> Decision(Guid id, RecordCertificationDecisionRequest r, CancellationToken ct) => mediator.Send(new RecordCertificationDecisionCommand(new CertificationCandidateId(id), r.Decision, r.Comment), ct);
    [HttpPost("sessions/{id:guid}/publish-results")]
    [HasPermission(PedagoraPilotPermissionCodes.Results.Publish)]
    public Task<CertificationExamSessionDto> Publish(Guid id, CancellationToken ct) => mediator.Send(new PublishCertificationResultsCommand(new CertificationExamSessionId(id)), ct);
}
