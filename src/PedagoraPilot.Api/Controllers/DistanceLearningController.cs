using DomainRelay.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PedagoraPilot.Api.Authorization;
using PedagoraPilot.Application.DistanceLearning;
using PedagoraPilot.Contracts.DistanceLearning;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Security.Contracts;

namespace PedagoraPilot.Api.Controllers;
[ApiController]
[Route("api/v1/distance-learning")]
[Authorize]
public sealed class DistanceLearningController(IMediator mediator) : ControllerBase
{
    [HttpGet("live-sessions")]
    [HasPermission(PedagoraPilotPermissionCodes.DistanceLearning.View)]
    public async Task<ActionResult<IReadOnlyCollection<DistanceLearningSessionDto>>> LiveSessions([FromQuery] Guid? cohortId, CancellationToken ct) => Ok(await mediator.Send(new GetDistanceLearningSessionsQuery(cohortId.HasValue ? new CohortId(cohortId.Value) : null), ct));
    [HttpPost("live-sessions")]
    [HasPermission(PedagoraPilotPermissionCodes.DistanceLearning.Manage)]
    public async Task<ActionResult<DistanceLearningSessionDto>> Create([FromBody] CreateDistanceLearningSessionRequest r, CancellationToken ct) => Ok(await mediator.Send(new CreateDistanceLearningSessionCommand(r.SiteId, r.ProgramId, new CohortId(r.CohortId), r.Title, r.TrainerDisplayName, r.TrainerEmail, r.StartsAtUtc, r.EndsAtUtc, r.Platform, r.JoinUrl, r.Objectives), ct));
    [HttpPost("live-sessions/{sessionId:guid}/participants")]
    [HasPermission(PedagoraPilotPermissionCodes.DistanceLearning.Manage)]
    public async Task<ActionResult<DistanceLearningSessionDto>> AddParticipant(Guid sessionId, [FromBody] AddDistanceParticipantRequest r, CancellationToken ct) => Ok(await mediator.Send(new AddDistanceParticipantCommand(new DistanceLearningSessionId(sessionId), new EnrollmentId(r.EnrollmentId), r.DisplayName), ct));
    [HttpPut("live-sessions/{sessionId:guid}/status")]
    [HasPermission(PedagoraPilotPermissionCodes.DistanceLearning.Manage)]
    public async Task<ActionResult<DistanceLearningSessionDto>> Status(Guid sessionId, [FromBody] ChangeDistanceSessionStatusRequest r, CancellationToken ct) => Ok(await mediator.Send(new ChangeDistanceSessionStatusCommand(new DistanceLearningSessionId(sessionId), r.Status), ct));
    [HttpPut("live-sessions/{sessionId:guid}/participants/{participantId:guid}/attendance")]
    [HasPermission(PedagoraPilotPermissionCodes.DistanceLearning.Manage)]
    public async Task<ActionResult<DistanceLearningSessionDto>> Attendance(Guid sessionId, Guid participantId, [FromBody] UpdateDistanceAttendanceRequest r, CancellationToken ct) => Ok(await mediator.Send(new UpdateDistanceAttendanceCommand(new DistanceLearningSessionId(sessionId), new DistanceParticipantId(participantId), r.Attendance, r.ConnectedAtUtc, r.DisconnectedAtUtc, r.ConnectedMinutes, r.ParticipationPercent, r.CompletedActivities, r.ActivityCount), ct));
    [HttpGet("async-modules")]
    [HasPermission(PedagoraPilotPermissionCodes.DistanceLearning.View)]
    public async Task<ActionResult<IReadOnlyCollection<AsyncLearningModuleDto>>> Modules([FromQuery] Guid? cohortId, CancellationToken ct) => Ok(await mediator.Send(new GetAsyncLearningModulesQuery(cohortId.HasValue ? new CohortId(cohortId.Value) : null), ct));
    [HttpPost("async-modules")]
    [HasPermission(PedagoraPilotPermissionCodes.DistanceLearning.Manage)]
    public async Task<ActionResult<AsyncLearningModuleDto>> CreateModule([FromBody] CreateAsyncLearningModuleRequest r, CancellationToken ct) => Ok(await mediator.Send(new CreateAsyncLearningModuleCommand(r.SiteId, r.ProgramId, new CohortId(r.CohortId), r.Title, r.Description, r.EstimatedMinutes, r.DueDate, r.TrainerDisplayName, r.ExpectedStudents, r.Steps), ct));
    [HttpPut("async-modules/{moduleId:guid}/progress")]
    [HasPermission(PedagoraPilotPermissionCodes.DistanceLearning.Manage)]
    public async Task<ActionResult<AsyncLearningModuleDto>> Progress(Guid moduleId, [FromBody] UpdateAsyncModuleProgressRequest r, CancellationToken ct) => Ok(await mediator.Send(new UpdateAsyncModuleProgressCommand(new AsyncLearningModuleId(moduleId), r.ProgressPercent, r.CompletedStudents, r.AverageScore), ct));
}
