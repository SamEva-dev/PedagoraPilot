using PedagoraPilot.Application.Abstractions.Security;

namespace PedagoraPilot.Api.Middleware;

/// <summary>Reject authenticated API and hub calls without a trustworthy tenant scope.</summary>
public sealed class TenantIdentityMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ICurrentUser currentUser)
    {
        if (currentUser.IsAuthenticated &&
            (context.Request.Path.StartsWithSegments("/api/v1") ||
             context.Request.Path.StartsWithSegments("/hubs/notifications")))
            _ = TenantScope.Organization(currentUser);

        await next(context);
    }
}
