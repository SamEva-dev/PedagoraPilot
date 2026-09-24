using DomainRelay.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PedagoraPilot.Api.Authorization;
using PedagoraPilot.Application.Workplace;
using PedagoraPilot.Contracts.Workplace;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Security.Contracts;

namespace PedagoraPilot.Api.Controllers;
[ApiController, Route("api/v1/workplace"), Authorize]
public sealed class WorkplaceController(IMediator mediator) : ControllerBase
{
    [HttpGet("periods"), HasPermission(PedagoraPilotPermissionCodes.Internships.View)]
    public Task<IReadOnlyCollection<WorkplacePeriodDto>> Get([FromQuery] Guid? cohortId, [FromQuery] Guid? enrollmentId, CancellationToken ct) => mediator.Send(new GetWorkplacePeriodsQuery(cohortId.HasValue ? new CohortId(cohortId.Value) : null, enrollmentId.HasValue ? new EnrollmentId(enrollmentId.Value) : null), ct);
    [HttpGet("periods/{id:guid}"), HasPermission(PedagoraPilotPermissionCodes.Internships.View)]
    public Task<WorkplacePeriodDto> GetOne(Guid id, CancellationToken ct) => mediator.Send(new GetWorkplacePeriodQuery(new WorkplacePeriodId(id)), ct);
    [HttpGet("referentials/{referentialVersionId:guid}/requirements"), HasPermission(PedagoraPilotPermissionCodes.Internships.View)]
    public Task<IReadOnlyCollection<WorkplaceRequirementDto>> Requirements(Guid referentialVersionId, [FromQuery] string periodTypeCode, CancellationToken ct) => mediator.Send(new GetWorkplaceRequirementsQuery(referentialVersionId, periodTypeCode), ct);
    [HttpPost("periods"), HasPermission(PedagoraPilotPermissionCodes.Internships.Manage)]
    public async Task<ActionResult<WorkplacePeriodDto>> Create(CreateWorkplacePeriodRequest r, CancellationToken ct)
    {
        var x = await mediator.Send(new CreateWorkplacePeriodCommand(new EnrollmentId(r.EnrollmentId), r.PeriodTypeCode, r.Company, r.City, r.TutorName, r.TutorEmail, r.TutorPhone, r.StartDate, r.EndDate, r.PlannedHours, r.AgreementReceived, r.Notes), ct);
        return Created($"/api/v1/workplace/periods/{x.Id}", x);
    }

    [HttpPut("periods/{id:guid}"), HasPermission(PedagoraPilotPermissionCodes.Internships.Manage)]
    public Task<WorkplacePeriodDto> Update(Guid id, UpdateWorkplacePeriodRequest r, CancellationToken ct) => mediator.Send(new UpdateWorkplacePeriodCommand(new WorkplacePeriodId(id), r.Company, r.City, r.TutorName, r.TutorEmail, r.TutorPhone, r.StartDate, r.EndDate, r.PlannedHours, r.TrainerVisible, r.Notes), ct);
    [HttpPut("periods/{id:guid}/hours"), HasPermission(PedagoraPilotPermissionCodes.Internships.Manage)]
    public Task<WorkplacePeriodDto> Hours(Guid id, UpdateWorkplaceHoursRequest r, CancellationToken ct) => mediator.Send(new UpdateWorkplaceHoursCommand(new WorkplacePeriodId(id), r.CompletedHours, r.TutorObservation), ct);
    [HttpPut("periods/{id:guid}/activities/{activityId:guid}"), HasPermission(PedagoraPilotPermissionCodes.Internships.Manage)]
    public Task<WorkplacePeriodDto> Activity(Guid id, Guid activityId, UpdateWorkplaceActivityRequest r, CancellationToken ct) => mediator.Send(new UpdateWorkplaceActivityCommand(new WorkplacePeriodId(id), new WorkplaceActivityId(activityId), r.Status, r.Comment), ct);
    [HttpPut("periods/{id:guid}/documents/{itemId:guid}"), HasPermission(PedagoraPilotPermissionCodes.Internships.Manage)]
    public Task<WorkplacePeriodDto> Document(Guid id, Guid itemId, UpdateWorkplaceDocumentRequest r, CancellationToken ct) => mediator.Send(new UpdateWorkplaceDocumentCommand(new WorkplacePeriodId(id), new WorkplaceDocumentChecklistItemId(itemId), r.Status, r.DocumentId), ct);
    [HttpPost("periods/{id:guid}/evaluations"), HasPermission(PedagoraPilotPermissionCodes.Internships.Manage)]
    public Task<WorkplacePeriodDto> Evaluation(Guid id, RecordWorkplaceEvaluationRequest r, CancellationToken ct) => mediator.Send(new RecordWorkplaceEvaluationCommand(new WorkplacePeriodId(id), r.Kind, r.EvaluatorDisplayName, r.EvaluatedAtUtc, r.Summary, r.Strengths, r.ImprovementAreas, r.Validated), ct);
}
