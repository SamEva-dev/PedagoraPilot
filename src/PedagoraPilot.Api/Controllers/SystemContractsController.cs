using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PedagoraPilot.Api.Controllers;
[ApiController]
[Route("api/v1/system/contracts")]
public sealed class SystemContractsController(IConfiguration configuration, IHostEnvironment environment) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get() => Ok(new { product = configuration["Application:ProductName"] ?? "Pedagora Pilot", applicationCode = configuration["Application:ApplicationCode"] ?? "pedagora-pilot", apiVersion = "v1", realtimeHubPath = configuration["Realtime:HubPath"] ?? "/hubs/notifications", demoEnabled = !environment.IsProduction() && configuration.GetValue<bool>("Demo:Enabled"), auth = new { provider = "AuthGate", clientId = configuration["AuthGate:ClientId"] }, invariants = new { stronglyTypedIdentifiers = true, noTrackingReadsByDefault = true, idempotency = configuration.GetValue<bool>("Idempotency:Enabled"), outbox = true, signalR = configuration.GetValue<bool>("Realtime:Enabled") } });
}
