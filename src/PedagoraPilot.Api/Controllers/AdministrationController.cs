using DomainRelay.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PedagoraPilot.Api.Authorization;
using PedagoraPilot.Application.Organizations.Administration;
using PedagoraPilot.Contracts.Administration;
using PedagoraPilot.Security.Contracts;

namespace PedagoraPilot.Api.Controllers;

[ApiController]
[Route("api/v1/organizations/{organizationId:guid}/administration")]
[Authorize]
public sealed class AdministrationController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [HasPermission(PedagoraPilotPermissionCodes.Organization.Manage)]
    public async Task<ActionResult<OrganizationAdministrationDto>> Get(Guid organizationId, CancellationToken ct)
        => Ok(await mediator.Send(new GetOrganizationAdministrationQuery(organizationId), ct));

    [HttpPut]
    [HasPermission(PedagoraPilotPermissionCodes.Organization.Manage)]
    public async Task<ActionResult<OrganizationAdministrationDto>> Update(
        Guid organizationId,
        [FromBody] UpdateOrganizationAdministrationRequest request,
        CancellationToken ct)
        => Ok(await mediator.Send(new UpdateOrganizationAdministrationCommand(organizationId, request), ct));

    [HttpPost("verification/submit")]
    [HasPermission(PedagoraPilotPermissionCodes.Organization.Manage)]
    public async Task<ActionResult<OrganizationAdministrationDto>> SubmitVerification(Guid organizationId, CancellationToken ct)
        => Ok(await mediator.Send(new SubmitOrganizationVerificationCommand(organizationId), ct));

    [HttpPut("verification")]
    [HasPermission(PedagoraPilotPermissionCodes.Organization.Manage)]
    public async Task<ActionResult<OrganizationAdministrationDto>> SetVerificationStatus(
        Guid organizationId,
        [FromBody] OrganizationVerificationStatusRequest request,
        CancellationToken ct)
        => Ok(await mediator.Send(new SetOrganizationVerificationStatusCommand(organizationId, request), ct));

    [HttpPost("onboarding/complete")]
    [HasPermission(PedagoraPilotPermissionCodes.Organization.Manage)]
    public async Task<ActionResult<OrganizationAdministrationDto>> CompleteOnboarding(Guid organizationId, CancellationToken ct)
        => Ok(await mediator.Send(new CompleteOrganizationOnboardingCommand(organizationId), ct));

    [HttpPut("commercial")]
    [HasPermission(PedagoraPilotPermissionCodes.Organization.CommercialManage)]
    public async Task<ActionResult<OrganizationAdministrationDto>> UpdateCommercialState(
        Guid organizationId,
        [FromBody] UpdateOrganizationCommercialStateRequest request,
        CancellationToken ct)
        => Ok(await mediator.Send(new UpdateOrganizationCommercialStateCommand(organizationId, request), ct));
}
