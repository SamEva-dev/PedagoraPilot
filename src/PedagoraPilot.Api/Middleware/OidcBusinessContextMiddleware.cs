using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using PedagoraPilot.Api.Controllers;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Security.Contracts;

namespace PedagoraPilot.Api.Middleware;

/// <summary>
/// Authenticates an OIDC access token using the dedicated audience-bound scheme,
/// then constructs tenant/permission claims ONLY from validated server-side data.
/// Legacy Bearer tokens are left untouched. Off by default until rollout.
/// </summary>
public sealed class OidcBusinessContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        IConfiguration configuration,
        IOrganizationMemberRepository members,
        IOrganizationRepository organizations,
        IHttpClientFactory clients)
    {
        // SignalR WebSocket/SSE clients may send their bearer token as `access_token`
        // in the query string. Accept it ONLY for the known hub endpoints.
        var isHub = context.Request.Path.StartsWithSegments(
            configuration["Realtime:HubPath"] ?? "/hubs/notifications", StringComparison.OrdinalIgnoreCase);
        var isApi = context.Request.Path.StartsWithSegments("/api");
        var hasBearerHeader = context.Request.Headers.Authorization.ToString()
            .StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);
        var hasHubQueryToken = isHub &&
            !string.IsNullOrWhiteSpace(context.Request.Query["access_token"].ToString());

        if (!configuration.GetValue<bool>("OidcApi:Enabled") ||
            !configuration.GetValue<bool>("OidcApi:BusinessEnforcementEnabled") ||
            (!isApi && !isHub) ||
            (isApi && (context.Request.Path.StartsWithSegments("/api/v1/session") ||
                       context.Request.Path.StartsWithSegments("/api/v1/oidc"))) ||
            context.User.Identity?.IsAuthenticated == true ||
            (!hasBearerHeader && !hasHubQueryToken))
        {
            await next(context);
            return;
        }

        // Never choose silently between two different credentials on a hub call.
        if (isHub && hasBearerHeader && hasHubQueryToken &&
            !string.Equals(context.Request.Headers.Authorization.ToString()["Bearer ".Length..].Trim(),
                context.Request.Query["access_token"].ToString(), StringComparison.Ordinal))
        {
            await Deny(context, StatusCodes.Status403Forbidden, "OIDC_CONFLICTING_CREDENTIALS");
            return;
        }

        // Token authentication remains bound to this application's OIDC audience.
        var verified = await context.AuthenticateAsync("AuthGateOidc");
        if (!verified.Succeeded || verified.Principal is null)
        {
            await next(context); // Normal framework 401/403, no OIDC fallback privileges.
            return;
        }

        var requestedWorkspace = isHub
            ? context.Request.Query["workspaceId"].ToString()
            : context.Request.Headers["X-Workspace-Id"].ToString();
        var headerWorkspace = context.Request.Headers["X-Workspace-Id"].ToString();
        if (isHub && !string.IsNullOrWhiteSpace(headerWorkspace) &&
            !string.Equals(headerWorkspace, requestedWorkspace, StringComparison.OrdinalIgnoreCase))
        {
            await Deny(context, StatusCodes.Status403Forbidden, "OIDC_WORKSPACE_MISMATCH");
            return;
        }

        if (!Guid.TryParse(verified.Principal.FindFirst("sub")?.Value, out var subject) ||
            subject == Guid.Empty ||
            !Guid.TryParse(requestedWorkspace, out var organizationId) ||
            organizationId == Guid.Empty ||
            HasConflictingRouteWorkspace(context, organizationId))
        {
            await Deny(context, StatusCodes.Status403Forbidden, "OIDC_WORKSPACE_REQUIRED");
            return;
        }

        // Tenant and membership come from Pedagora's database, not from OIDC claims/header.
        var membership = await members.GetByAuthGateUserAsync(
            organizationId, subject, false, context.RequestAborted);
        var organization = await organizations.GetByIdAsync(
            organizationId, false, context.RequestAborted);
        if (membership is null || !membership.IsActive ||
            organization is null || !organization.IsLoginAllowed)
        {
            await Deny(context, StatusCodes.Status403Forbidden, "OIDC_MEMBERSHIP_INACTIVE");
            return;
        }
        var roles = await members.GetRoleCodesAsync(organizationId, subject, context.RequestAborted);
        if (roles.Count == 0 || !roles.All(r =>
                PedagoraPilotRoleCodes.All.Contains(r, StringComparer.OrdinalIgnoreCase)))
        {
            await Deny(context, StatusCodes.Status403Forbidden, "OIDC_PRODUCT_ROLE_UNMAPPED");
            return;
        }

        var authority = await OidcEffectivePermissionGateway.ResolveAsync(
            context, configuration, clients, subject, "pedagora-pilot", organizationId,
            context.RequestAborted);
        if (!authority.Resolved)
        {
            await Deny(context,
                authority.Denied ? StatusCodes.Status403Forbidden : StatusCodes.Status503ServiceUnavailable,
                authority.Denied ? "OIDC_AUTHGATE_ACCESS_REVOKED" : "OIDC_PERMISSION_AUTHORITY_UNAVAILABLE");
            return;
        }

        var defaults = roles.SelectMany(PedagoraPilotRolePermissionDefaults.GetPermissions)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var explicitAllows = authority.ExplicitAllowOverrides.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var grants = authority.Permissions
            .Where(p => defaults.Contains(p) || explicitAllows.Contains(p))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        // Do not inject platform-admin roles or platform_admin flag, since legacy
        // HasPermission has a role-based superuser bypass. Verified permissions
        // are the only grants. Route-role policies may deny (safe migration gap).
        var claims = new List<Claim>
        {
            new("sub", subject.ToString("D")),
            new("org_id", organizationId.ToString("D")),
            new("organization_id", organizationId.ToString("D")),
            new("app", "pedagora-pilot")
        };
        claims.AddRange(grants.Select(p => new Claim("permissions", p)));
        claims.AddRange(authority.ContextualScopes.Select(p => new Claim("permissions", p)));
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "AuthGateOidcBusiness", "sub", "role"));
        await next(context);
    }

    private static bool HasConflictingRouteWorkspace(HttpContext context, Guid selected)
    {
        foreach (var key in new[] { "organizationId", "orgId", "tenantId" })
        {
            if (context.Request.RouteValues.TryGetValue(key, out var value))
            {
                if (!Guid.TryParse(value?.ToString(), out var routeId) || routeId != selected)
                    return true;
            }
        }
        return false;
    }

    private static Task Deny(HttpContext context, int status, string code)
    {
        context.Response.StatusCode = status;
        context.Response.Headers.CacheControl = "no-store";
        return context.Response.WriteAsJsonAsync(new { code });
    }
}
