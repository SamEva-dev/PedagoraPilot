using DomainRelay.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PedagoraPilot.Api.Authorization;
using PedagoraPilot.Application.Catalog.Programs;
using PedagoraPilot.Contracts.Catalog;
using PedagoraPilot.Security.Contracts;

namespace PedagoraPilot.Api.Controllers;
[ApiController, Route("api/v1/programs"), Authorize]
public sealed class ProgramsController(IMediator mediator) : ControllerBase
{
    [HttpGet, HasPermission(PedagoraPilotPermissionCodes.Programs.View)]
    public Task<IReadOnlyCollection<TrainingProgramDto>> Get([FromQuery] Guid? organizationId, CancellationToken ct) => mediator.Send(new GetProgramsQuery(organizationId), ct);
    
    [HttpPost, HasPermission(PedagoraPilotPermissionCodes.Programs.Manage)]
    public async Task<ActionResult<TrainingProgramDto>> Create(CreateProgramRequest r, CancellationToken ct)
    {
        var x = await mediator.Send(new CreateProgramCommand(r.FamilyCode, r.Code, r.Name, r.DescriptionKey, r.Icon, r.DurationHours, r.Status, r.EnabledModules, r.ExternalKey), ct);
        return Created($"/api/v1/programs/{x.Id}", x);
    }

    [HttpPut("{id:guid}"), HasPermission(PedagoraPilotPermissionCodes.Programs.Manage)]
    public Task<TrainingProgramDto> Update(Guid id, UpdateProgramRequest r, CancellationToken ct) => mediator.Send(new UpdateProgramCommand(id, r.FamilyCode, r.Name, r.DescriptionKey, r.Icon, r.DurationHours, r.Status, r.EnabledModules), ct);
    
    [HttpPut("{id:guid}/sites/{siteId:guid}/offering"), HasPermission(PedagoraPilotPermissionCodes.Programs.Manage)]
    public Task<ProgramOfferingDto> Offering(Guid id, Guid siteId, SetProgramOfferingRequest r, CancellationToken ct) => mediator.Send(new SetProgramOfferingCommand(id, siteId, r.Active), ct);
}
