using DomainRelay.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PedagoraPilot.Api.Authorization;
using PedagoraPilot.Application.Organizations.Access;
using PedagoraPilot.Security.Contracts;

namespace PedagoraPilot.Api.Controllers;

[ApiController]
[Route("api/v1/access/scope-validation")]
[Authorize]
public sealed class AccessScopeController(IMediator mediator) : ControllerBase
{
    public sealed record ValidateScopeRequest(
        string Scope,
        Guid? SiteId,
        Guid? ProgramId,
        Guid? CohortId,
        Guid? ExamSessionId);

    [HttpPost]
    [HasPermission(PedagoraPilotPermissionCodes.Access.Manage)]
    public async Task<IActionResult> Validate(ValidateScopeRequest request, CancellationToken ct)
    {
        var valid = await mediator.Send(new ValidateAccessScopeAssignmentQuery(
            request.Scope,
            request.SiteId,
            request.ProgramId,
            request.CohortId,
            request.ExamSessionId), ct);

        return valid ? Ok(new { valid = true }) : BadRequest(new { valid = false, error = "invalid_scope_assignment_target" });
    }
}
