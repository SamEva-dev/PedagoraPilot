using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PedagoraPilot.Application.Abstractions.Security;

namespace PedagoraPilot.Api.Controllers;
[ApiController]
[Route("api/v1/me")]
[Authorize]
public sealed class MeController : ControllerBase
{
    [HttpGet]
    public IActionResult GetCurrentUser([FromServices] ICurrentUser currentUser) => Ok(new { currentUser.UserId, currentUser.OrganizationId, currentUser.Application, Roles = currentUser.Roles.Order(StringComparer.OrdinalIgnoreCase), Permissions = currentUser.Permissions.Order(StringComparer.OrdinalIgnoreCase) });
}
