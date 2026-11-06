using DomainRelay.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PedagoraPilot.Api.Authorization;
using PedagoraPilot.Application.Workplace;
using PedagoraPilot.Contracts.Workplace;
using PedagoraPilot.Security.Contracts;

namespace PedagoraPilot.Api.Controllers;

[ApiController, Authorize, Route("api/v1/workplace/tutor")]
public sealed class WorkplaceTutorController(IMediator mediator) : ControllerBase
{
    [HttpGet("periods")]
    public Task<IReadOnlyCollection<WorkplacePeriodDto>> Assigned(CancellationToken ct) =>
        mediator.Send(new GetAssignedTutorPeriodsQuery(), ct);

    [HttpPut("periods/{id:guid}/hours")]
    public Task<WorkplacePeriodDto> Hours(Guid id, TutorHoursRequest r, CancellationToken ct) =>
        mediator.Send(new UpdateTutorHoursCommand(id, r.CompletedHours, r.Observation), ct);

    [HttpPut("periods/{id:guid}/activities/{activityId:guid}")]
    public Task<WorkplacePeriodDto> Activity(Guid id, Guid activityId, TutorActivityRequest r, CancellationToken ct) =>
        mediator.Send(new UpdateTutorActivityCommand(id, activityId, r.Status, r.Comment), ct);

    [HttpPost("periods/{id:guid}/evaluations")]
    public Task<WorkplacePeriodDto> Evaluate(Guid id, TutorEvaluationRequest r, CancellationToken ct) =>
        mediator.Send(new SubmitTutorEvaluationCommand(id, r.Summary, r.Strengths, r.ImprovementAreas, r.Validated), ct);
}
public sealed record TutorHoursRequest(decimal CompletedHours, string? Observation);
public sealed record TutorActivityRequest(string Status, string? Comment);
public sealed record TutorEvaluationRequest(string Summary, string? Strengths, string? ImprovementAreas, bool? Validated);
