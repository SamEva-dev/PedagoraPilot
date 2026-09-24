using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Infrastructure.Persistence;

namespace PedagoraPilot.Api.Controllers;
[ApiController]
[Route("api/system")]
public sealed class SystemController : ControllerBase
{
    [HttpGet("live")]
    [AllowAnonymous]
    public IActionResult Live() => Ok(new { status = "Healthy", service = "PedagoraPilot.Api" });
    [HttpGet("ready")]
    [AllowAnonymous]
    public async Task<IActionResult> Ready([FromServices] IDbContextFactory<PedagoraPilotDbContext> factory, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var ready = await db.Database.CanConnectAsync(cancellationToken);
        return ready ? Ok(new { status = "Healthy", database = "PostgreSQL" }) : StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "Unhealthy" });
    }
}
