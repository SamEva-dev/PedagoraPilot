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
    [HttpPut("{siteId:guid}"), HasPermission(PedagoraPilotPermissionCodes.Sites.Manage)]
    public async Task<ActionResult<WorkspaceSiteDto>> Update(Guid organizationId, Guid siteId, [FromBody] UpdateTrainingSiteRequest request, CancellationToken ct)
        => Ok(await mediator.Send(new UpdateTrainingSiteCommand(organizationId, siteId, request.Code, request.Name,
            request.City, request.Address, request.PostalCode, request.Phone, request.Email, request.Manager,
            request.Status), ct));

    [HttpPost, HasPermission(PedagoraPilotPermissionCodes.Sites.Manage)]
    public async Task<ActionResult<WorkspaceSiteDto>> Create(Guid organizationId, [FromBody] CreateTrainingSiteRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(new CreateTrainingSiteCommand(organizationId, request.Code, request.Name,
            request.City, request.Address, request.PostalCode, request.Phone, request.Email, request.Manager,
            request.Status, request.ExternalKey), ct);
        return Created($"/api/v1/organizations/{organizationId}/sites/{result.Id}", result);
    }
}
