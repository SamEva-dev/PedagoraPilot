using PedagoraPilot.Application.Abstractions.Security;

namespace PedagoraPilot.Api.Middleware;

/// <summary>Reject authenticated API and hub calls without a trustworthy tenant scope.</summary>
public sealed class TenantIdentityMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ICurrentUser currentUser)
    {
        // This read-only, [Authorize] endpoint is required *before* a user can
        // select a tenant. It checks the subject against product memberships
        // itself and NEVER establishes or grants a business context.
        var isContextDiscovery = string.Equals(
            context.Request.Path.Value,
            "/api/v1/me/organization-labels",
            StringComparison.OrdinalIgnoreCase);
        if (currentUser.IsAuthenticated && !isContextDiscovery &&
            (context.Request.Path.StartsWithSegments("/api/v1") ||
             context.Request.Path.StartsWithSegments("/hubs/notifications")))
            _ = TenantScope.Organization(currentUser);

        await next(context);
    }
}
