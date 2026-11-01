using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PedagoraPilot.Api.Controllers;

/// <summary>
/// Opt-in OIDC identity probe, deliberately not a business session.
/// Never returns roles, permissions, memberships or organization data.
/// </summary>
[ApiController]
[Route("api/v1/oidc/identity")]
public sealed class OidcIdentityProbeController(IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    [Authorize(AuthenticationSchemes = "AuthGateOidc")]
    public IActionResult Get()
    {
        if (!configuration.GetValue<bool>("OidcApi:Enabled"))
            return NotFound();

        var subject = User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(subject, out var userId))
            return Unauthorized();

        return Ok(new { authenticated = true, subject = userId });
    }
}
