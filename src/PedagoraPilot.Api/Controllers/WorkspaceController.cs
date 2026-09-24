using DomainRelay.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PedagoraPilot.Application.Organizations.Workspace;
using PedagoraPilot.Contracts.Workspace;

namespace PedagoraPilot.Api.Controllers;
[ApiController, Route("api/v1/me/workspace"), Authorize]
public sealed class WorkspaceController(IMediator mediator) : ControllerBase
{
    [HttpGet, ProducesResponseType(typeof(WorkspaceBootstrapDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WorkspaceBootstrapDto>> Get(CancellationToken ct) => Ok(await mediator.Send(new GetWorkspaceBootstrapQuery(), ct));
}
