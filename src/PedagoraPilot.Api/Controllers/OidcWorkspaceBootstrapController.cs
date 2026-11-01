using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Security.Contracts;

namespace PedagoraPilot.Api.Controllers;

/// <summary>
/// Read-only OIDC product context. An identity token cannot create roles, permissions or a session.
/// Memberships and role profiles are read afresh from the PedagoraPilot database.
/// </summary>
[ApiController]
[Route("api/v1/oidc/workspaces")]
[Authorize(AuthenticationSchemes = "AuthGateOidc")]
public sealed class OidcWorkspaceBootstrapController(
    IConfiguration configuration,
    IOrganizationMemberRepository members,
    IOrganizationRepository organizations,
    IHttpClientFactory httpClients,
    ILogger<OidcWorkspaceBootstrapController> logger) : ControllerBase
{
    [HttpGet]
    [HttpGet("/api/v1/session/bootstrap")]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("OidcApi:Enabled")) return NotFound();
        if (!TrySubject(out var subject)) return Unauthorized();

        var ids = await members.ListActiveOrganizationIdsByAuthGateUserAsync(subject, cancellationToken);
        var workspaces = new List<object>();
        var blockedOrganizations = 0;
        var missingRoles = 0;
        var unmappedRoles = 0;
        foreach (var id in ids)
        {
            var org = await organizations.GetByIdAsync(id, false, cancellationToken);
            if (org is null || !org.IsLoginAllowed)
            {
                blockedOrganizations++;
                continue;
            }
            var roles = await members.GetRoleCodesAsync(id, subject, cancellationToken);
            if (roles.Count == 0)
            {
                missingRoles++;
                continue;
            }
            if (!HasOnlyCatalogRoles(roles))
            {
                unmappedRoles++;
                continue;
            }
            var defaults = GetRoleDefaults(roles);

            workspaces.Add(new
            {
                id,
                organizationId = id,
                label = org.ShortName,
                roles = roles.Order(StringComparer.OrdinalIgnoreCase).ToArray(),
                // Role matrix only: not an effective grant and never consumed by API policies.
                roleDefaultPermissions = defaults,
                active = true
            });
        }

        // Do not guess grants or silently provision a role when the AuthGate
        // identity is valid but no PedagoraPilot business membership is usable.
        // Structured counts distinguish missing local membership from disabled
        // organizations / missing role profiles without exposing details to the UI.
        if (workspaces.Count == 0)
        {
            logger.LogWarning(
                "OIDC bootstrap no_access: Subject={Subject} ActiveMembershipOrganizations={ActiveMembershipOrganizations} BlockedOrganizations={BlockedOrganizations} MissingRoleProfiles={MissingRoleProfiles} UnmappedRoleProfiles={UnmappedRoleProfiles}",
                subject, ids.Count, blockedOrganizations, missingRoles, unmappedRoles);
        }

        Response.Headers.CacheControl = "no-store";
        return Ok(new
        {
            subject,
            state = workspaces.Count == 0 ? "no_access" : "select_workspace",
            workspaces,
            selectedWorkspaceId = (Guid?)null,
            permissions = Array.Empty<string>(),
            permissionsResolved = false,
            // Discovery returns identity/workspace choices only, not a business session.
            businessSessionEstablished = false,
            version = 1
        });
    }

    [HttpGet("{organizationId:guid}/bootstrap")]
    public Task<IActionResult> Bootstrap(Guid organizationId, CancellationToken ct) => VerifyContext(organizationId, ct);

    [HttpPost("/api/v1/session/context")]
    public Task<IActionResult> Select([FromBody] WorkspaceSelectionRequest request, CancellationToken ct) =>
        VerifyContext(request.WorkspaceId, ct);

    private async Task<IActionResult> VerifyContext(Guid organizationId, CancellationToken ct)
    {
        if (!configuration.GetValue<bool>("OidcApi:Enabled")) return NotFound();
        if (!TrySubject(out var subject)) return Unauthorized();
        if (organizationId == Guid.Empty) return BadRequest(new { code = "ORGANIZATION_REQUIRED" });

        // The selected context is never trusted: recheck active membership and organization.
        var member = await members.GetByAuthGateUserAsync(organizationId, subject, false, ct);
        if (member is null || !member.IsActive)
            return NotFound(new { code = "WORKSPACE_NOT_AVAILABLE" });
        var org = await organizations.GetByIdAsync(organizationId, false, ct);
        if (org is null || !org.IsLoginAllowed)
            return NotFound(new { code = "WORKSPACE_NOT_AVAILABLE" });
        var roles = await members.GetRoleCodesAsync(organizationId, subject, ct);
        if (roles.Count == 0)
            return StatusCode(StatusCodes.Status403Forbidden, new { code = "PRODUCT_ROLE_REQUIRED" });
        // Unknown role names must not silently acquire permissions.
        if (!HasOnlyCatalogRoles(roles))
            return StatusCode(StatusCodes.Status403Forbidden, new { code = "PRODUCT_ROLE_UNMAPPED" });
        var defaults = GetRoleDefaults(roles);
        var resolved = await OidcEffectivePermissionGateway.ResolveAsync(
            HttpContext, configuration, httpClients, subject, "pedagora-pilot", organizationId, ct);
        if (resolved.Denied)
            return StatusCode(StatusCodes.Status403Forbidden, new { code = "AUTHGATE_MEMBERSHIP_INACTIVE" });
        if (!resolved.Resolved)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { code = "PERMISSION_AUTHORITY_UNAVAILABLE" });
        // Product-role ceilings + explicitly configured AuthGate user grants.
        // A revocation in AuthGate cannot be bypassed by Itech.Common defaults.
        var authorized = resolved.Permissions
            .Intersect(defaults.Concat(resolved.ExplicitAllowOverrides), StringComparer.OrdinalIgnoreCase)
            .Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray();

        Response.Headers.CacheControl = "no-store";
        return Ok(new
        {
            subject,
            state = configuration.GetValue<bool>("OidcApi:BusinessEnforcementEnabled")
                ? "ready" : "context_verified",
            selectedWorkspaceId = organizationId,
            organizationId,
            roles = roles.Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            roleDefaultPermissions = defaults,
            permissions = authorized,
            permissionsResolved = true,
            businessSessionEstablished = configuration.GetValue<bool>("OidcApi:BusinessEnforcementEnabled"),
            version = 1
        });
    }

    private static bool HasOnlyCatalogRoles(IEnumerable<string> roles) =>
        roles.All(role => PedagoraPilotRoleCodes.All.Contains(role, StringComparer.OrdinalIgnoreCase));

    private static string[] GetRoleDefaults(IEnumerable<string> roles) =>
        roles.SelectMany(PedagoraPilotRolePermissionDefaults.GetPermissions)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private bool TrySubject(out Guid subject) =>
        Guid.TryParse(User.FindFirst("sub")?.Value, out subject) && subject != Guid.Empty;
}

public sealed record WorkspaceSelectionRequest(Guid WorkspaceId);
