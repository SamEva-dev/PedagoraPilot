using DomainRelay.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PedagoraPilot.Api.Authorization;
using PedagoraPilot.Application.Organizations.Sites;
using PedagoraPilot.Contracts.Workspace;
using PedagoraPilot.Security.Contracts;

namespace PedagoraPilot.Api.Controllers;
[ApiController, Route("api/v1/organizations/{organizationId:guid}/sites"), Authorize]
public sealed class SitesController(IMediator mediator) : ControllerBase
{
    [HttpPost, HasPermission(PedagoraPilotPermissionCodes.Sites.Manage), ProducesResponseType(typeof(WorkspaceSiteDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<WorkspaceSiteDto>> Create(Guid organizationId, [FromBody] CreateTrainingSiteRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateTrainingSiteCommand(organizationId, request.Code, request.Name, request.City, request.ExternalKey), ct);
        return Created($"/api/v1/organizations/{organizationId}/sites/{result.Id}", result);
    }
}
