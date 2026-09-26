using DomainRelay.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PedagoraPilot.Api.Authorization;
using PedagoraPilot.Application.Catalog.Referentials;
using PedagoraPilot.Contracts.Catalog;
using PedagoraPilot.Security.Contracts;

namespace PedagoraPilot.Api.Controllers;
[ApiController, Authorize]
public sealed class ReferentialsController(IMediator mediator) : ControllerBase
{
    [HttpGet("api/v1/referentials"), HasPermission(PedagoraPilotPermissionCodes.Referentials.View)]
    public Task<IReadOnlyCollection<ReferentialVersionDto>> Get([FromQuery] Guid? programId, CancellationToken ct) => mediator.Send(new GetReferentialsQuery(programId), ct);
    [HttpGet("api/v1/referential-versions/{versionId:guid}"), HasPermission(PedagoraPilotPermissionCodes.Referentials.View)]
    public Task<ReferentialVersionDetailsDto> GetDetails(Guid versionId, CancellationToken ct) => mediator.Send(new GetReferentialVersionDetailsQuery(versionId), ct);
    [HttpPost("api/v1/referentials/{referentialId:guid}/versions"), HasPermission(PedagoraPilotPermissionCodes.Referentials.Manage)]
    public async Task<ActionResult<ReferentialVersionDto>> CreateVersion(Guid referentialId, CreateReferentialVersionRequest r, CancellationToken ct)
    {
        var x = await mediator.Send(new CreateReferentialVersionCommand(referentialId, r.Version, r.CertificationCode, r.EffectiveFrom, r.TotalHours, r.SheetCount, r.RequiredDocumentCount, r.EnabledModules, r.NotesKey, r.ExternalKey, r.Publish), ct);
        return Created($"/api/v1/referential-versions/{x.Id}", x);
    }

    [HttpPost("api/v1/referential-versions/{versionId:guid}/publish"), HasPermission(PedagoraPilotPermissionCodes.Referentials.Publish)]
    public Task<ReferentialVersionDto> Publish(Guid versionId, CancellationToken ct) => mediator.Send(new PublishReferentialVersionCommand(versionId), ct);
}
