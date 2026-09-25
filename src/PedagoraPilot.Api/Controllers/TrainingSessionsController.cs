using DomainRelay.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PedagoraPilot.Api.Authorization;
using PedagoraPilot.Application.Abstractions.Security;
using PedagoraPilot.Application.Training.Learners;
using PedagoraPilot.Application.Training.Delivery;
using PedagoraPilot.Contracts.Training;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Security.Contracts;

namespace PedagoraPilot.Api.Controllers;
[ApiController, Route("api/v1/training-sessions"), Authorize]
public sealed class TrainingSessionsController(IMediator mediator, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet, HasPermission(PedagoraPilotPermissionCodes.Sessions.View)]
    public Task<IReadOnlyCollection<TrainingSessionDto>> Get([FromQuery] Guid? cohortId, [FromQuery] DateTimeOffset? fromUtc, [FromQuery] DateTimeOffset? toUtc, [FromQuery] string? type, [FromQuery] string? status, CancellationToken ct) => mediator.Send(new GetTrainingSessionsQuery(cohortId.HasValue ? new CohortId(cohortId.Value) : null, fromUtc, toUtc, type, status), ct);
    [HttpGet("{id:guid}"), HasPermission(PedagoraPilotPermissionCodes.Sessions.View)]
    public Task<TrainingSessionDto> GetById(Guid id, CancellationToken ct) => mediator.Send(new GetTrainingSessionQuery(new TrainingSessionId(id)), ct);
    [HttpPost, HasPermission(PedagoraPilotPermissionCodes.Sessions.Manage)]
    public async Task<ActionResult<TrainingSessionDto>> Create(CreateTrainingSessionRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateTrainingSessionCommand(new CohortId(request.CohortId), request.Type, request.Modality, request.Title, request.StartsAtUtc, request.EndsAtUtc, request.TimeZoneId, request.TrainerAuthGateUserId, request.TrainerDisplayName, request.Location, request.Objective, request.Supports, request.Comments, request.AudienceMode, request.ParticipantEnrollmentIds, request.ExternalKey), ct);
        return Created($"/api/v1/training-sessions/{result.Id}", result);
    }

    [HttpPut("{id:guid}"), HasPermission(PedagoraPilotPermissionCodes.Sessions.Manage)]
    public Task<TrainingSessionDto> Update(Guid id, UpdateTrainingSessionRequest request, CancellationToken ct) => mediator.Send(new UpdateTrainingSessionCommand(new TrainingSessionId(id), request.Type, request.Modality, request.Title, request.StartsAtUtc, request.EndsAtUtc, request.TimeZoneId, request.TrainerAuthGateUserId, request.TrainerDisplayName, request.Location, request.Objective, request.Supports, request.Comments, request.AudienceMode, request.ParticipantEnrollmentIds), ct);
    [HttpPost("{id:guid}/cancel"), HasPermission(PedagoraPilotPermissionCodes.Sessions.Manage)]
    public Task<TrainingSessionDto> Cancel(Guid id, CancellationToken ct) => mediator.Send(new CancelTrainingSessionCommand(new TrainingSessionId(id)), ct);
    [HttpPost("{id:guid}/complete"), HasPermission(PedagoraPilotPermissionCodes.Sessions.Manage)]
    public Task<TrainingSessionDto> Complete(Guid id, CancellationToken ct) => mediator.Send(new CompleteTrainingSessionCommand(new TrainingSessionId(id)), ct);
    [HttpGet("{id:guid}/attendance")]
    public async Task<ActionResult<AttendanceSheetDto>> Attendance(Guid id, CancellationToken ct)
    {
        if (!currentUser.HasPermission(PedagoraPilotPermissionCodes.Attendance.View)
            && !(LearnerSelfAccess.Applies(currentUser) && currentUser.HasPermission(PedagoraPilotPermissionCodes.Sessions.View)))
            return Forbid();
        return Ok(await mediator.Send(new GetAttendanceSheetQuery(new TrainingSessionId(id)), ct));
    }
    [HttpPut("{id:guid}/attendance"), HasPermission(PedagoraPilotPermissionCodes.Attendance.Manage)]
    public Task<AttendanceSheetDto> SaveAttendance(Guid id, SaveAttendanceRequest request, CancellationToken ct) => mediator.Send(new SaveAttendanceCommand(new TrainingSessionId(id), request.Entries), ct);
}
