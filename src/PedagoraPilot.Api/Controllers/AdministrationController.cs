using DomainRelay.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PedagoraPilot.Api.Authorization;
using PedagoraPilot.Application.Organizations.Administration;
using PedagoraPilot.Contracts.Administration;

namespace PedagoraPilot.Api.Controllers;

[ApiController]
[Route("api/v1/organizations/{organizationId:guid}/administration")]
[Authorize]
public sealed class AdministrationController(IMediator mediator) : ControllerBase
{
    private const string ManagePermission = "pedagora.organization.manage";

    [HttpGet]
    [HasPermission(ManagePermission)]
    public async Task<ActionResult<OrganizationAdministrationDto>> Get(Guid organizationId, CancellationToken ct)
        => Ok(await mediator.Send(new GetOrganizationAdministrationQuery(organizationId), ct));

    [HttpPut]
    [HasPermission(ManagePermission)]
    public async Task<ActionResult<OrganizationAdministrationDto>> Update(
        Guid organizationId,
        [FromBody] UpdateOrganizationAdministrationRequest request,
        CancellationToken ct)
        => Ok(await mediator.Send(new UpdateOrganizationAdministrationCommand(organizationId, request), ct));
}
