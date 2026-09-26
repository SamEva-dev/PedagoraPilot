using DomainRelay.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PedagoraPilot.Api.Authorization;
using PedagoraPilot.Application.Workforce;
using PedagoraPilot.Contracts.Workforce;
using PedagoraPilot.Domain.Identifiers;
using PedagoraPilot.Security.Contracts;

namespace PedagoraPilot.Api.Controllers;
[ApiController]
[Route("api/v1/workforce/remote-work")]
[Authorize]
public sealed class WorkforceController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [HasPermission(PedagoraPilotPermissionCodes.RemoteWork.View)]
    public async Task<ActionResult<IReadOnlyCollection<RemoteWorkRequestDto>>> List([FromQuery] Guid? siteId, [FromQuery] bool mineOnly = false, CancellationToken ct = default) => Ok(await mediator.Send(new GetRemoteWorkRequestsQuery(siteId, mineOnly), ct));
    [HttpGet("policy")]
    [HasPermission(PedagoraPilotPermissionCodes.RemoteWork.View)]
    public async Task<ActionResult<RemoteWorkPolicyDto>> Policy(CancellationToken ct)
        => Ok(await mediator.Send(new GetRemoteWorkPolicyQuery(), ct));
    [HttpPost]
    [HasPermission(PedagoraPilotPermissionCodes.RemoteWork.View)]
    public async Task<ActionResult<RemoteWorkRequestDto>> Create([FromBody] CreateRemoteWorkRequest r, CancellationToken ct) => Ok(await mediator.Send(new CreateRemoteWorkCommand(r.SiteId, r.Date, r.Period, r.StartTime, r.EndTime, r.Comment, r.Activities), ct));
    [HttpPut("{requestId:guid}/decision")]
    [HasPermission(PedagoraPilotPermissionCodes.RemoteWork.Manage)]
    public async Task<ActionResult<RemoteWorkRequestDto>> Decide(Guid requestId, [FromBody] RecordRemoteWorkDecisionRequest r, CancellationToken ct) => Ok(await mediator.Send(new DecideRemoteWorkCommand(new RemoteWorkRequestId(requestId), r.Approved), ct));
    [HttpPut("{requestId:guid}/activities/{activityId:guid}")]
    [HasPermission(PedagoraPilotPermissionCodes.RemoteWork.View)]
    public async Task<ActionResult<RemoteWorkRequestDto>> Activity(Guid requestId, Guid activityId, [FromBody] UpdateRemoteWorkActivityRequest r, CancellationToken ct) => Ok(await mediator.Send(new UpdateRemoteWorkActivityCommand(new RemoteWorkRequestId(requestId), new RemoteWorkActivityId(activityId), r.Status), ct));
}
