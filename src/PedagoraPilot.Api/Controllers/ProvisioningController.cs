using DomainRelay.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PedagoraPilot.Api.Authorization;
using PedagoraPilot.Application.Organizations.Provision;
using PedagoraPilot.Application.Access.Members;
using PedagoraPilot.Application.Organizations.Queries;
using PedagoraPilot.Contracts.Provisioning;

namespace PedagoraPilot.Api.Controllers;
[ApiController]
[Route("api/provisioning/organizations")]
[Authorize(Policy = AuthorizationPolicies.AuthGateProvisioning)]
public sealed class ProvisioningController : ControllerBase
{
    private readonly IMediator _mediator;
    public ProvisioningController(IMediator mediator) => _mediator = mediator;
    [HttpPost]
    [ProducesResponseType(typeof(ProvisionOrganizationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProvisionOrganizationResponse>> Provision([FromBody] ProvisionOrganizationRequest request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new ProvisionOrganizationCommand(request.ExternalUserId, request.LegalName, request.CountryCode, request.Owner.FirstName, request.Owner.LastName, request.Owner.Email, request.Owner.Phone), cancellationToken);
        return Ok(response);
    }

    [HttpPost("{organizationId:guid}/members")]
    [ProducesResponseType(typeof(ProvisionOrganizationMemberResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProvisionOrganizationMemberResponse>> ProvisionMember(
        Guid organizationId,
        [FromBody] ProvisionOrganizationMemberRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationId != request.OrganizationId) return BadRequest(new { error = "organization_mismatch" });
        var response = await _mediator.Send(new SyncOrganizationMemberCommand(
            request.OrganizationId, request.AuthGateUserId, request.Email, request.FirstName, request.LastName, request.Roles, request.Active), cancellationToken);
        return Ok(response);
    }

    [HttpGet("{organizationId:guid}")]
    [ProducesResponseType(typeof(OrganizationVerificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrganizationVerificationResponse>> Verify(Guid organizationId, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetOrganizationVerificationQuery(organizationId), cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }
}
