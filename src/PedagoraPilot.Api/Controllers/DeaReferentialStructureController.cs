using System.Text.Json;
using DomainRelay.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PedagoraPilot.Api.Authorization;
using PedagoraPilot.Application.Catalog.Referentials;
using PedagoraPilot.Security.Contracts;

namespace PedagoraPilot.Api.Controllers;
[ApiController, Authorize]
public sealed class DeaReferentialStructureController(IMediator mediator) : ControllerBase
{
    [HttpGet("api/v1/referential-versions/{versionId:guid}/dea-structure")]
    [HasPermission(PedagoraPilotPermissionCodes.Referentials.View)]
    public async Task<ActionResult<DeaStructureResult>> Get(Guid versionId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetDeaStructureQuery(versionId), ct);
        return result is null ? NotFound() : Ok(result);
    }
    [HttpPut("api/v1/referential-versions/{versionId:guid}/dea-structure")]
    [HasPermission(PedagoraPilotPermissionCodes.Referentials.Manage)]
    public Task<DeaStructureResult> Put(Guid versionId, [FromBody] JsonElement content, CancellationToken ct)
        => mediator.Send(new SaveDeaStructureDraftCommand(versionId, content), ct);
}
