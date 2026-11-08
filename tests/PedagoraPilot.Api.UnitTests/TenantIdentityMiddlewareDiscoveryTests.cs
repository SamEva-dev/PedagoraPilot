using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PedagoraPilot.Api.Authorization;
using PedagoraPilot.Api.Middleware;
using PedagoraPilot.Application.Common.Errors;
using Xunit;

namespace PedagoraPilot.Api.UnitTests;

public sealed class TenantIdentityMiddlewareDiscoveryTests
{
    [Theory]
    [InlineData("/api/v1/me/organization-labels")]
    [InlineData("/API/V1/ME/ORGANIZATION-LABELS")]
    public async Task Authenticated_identity_can_discover_organizations_before_selecting_context(string path)
    {
        var context = NewContext(path);
        var called = false;
        var middleware = new TenantIdentityMiddleware(_ => { called = true; return Task.CompletedTask; });
        await middleware.InvokeAsync(context, new HttpCurrentUser(new HttpContextAccessor { HttpContext = context }));
        Assert.True(called);
    }

    [Theory]
    [InlineData("/api/v1/workspace")]
    [InlineData("/api/v1/me/organization-labels/other")]
    [InlineData("/hubs/notifications")]
    public async Task Business_routes_still_require_a_selected_organization(string path)
    {
        var context = NewContext(path);
        var middleware = new TenantIdentityMiddleware(_ => Task.CompletedTask);
        await Assert.ThrowsAsync<ForbiddenApplicationException>(() =>
            middleware.InvokeAsync(context, new HttpCurrentUser(new HttpContextAccessor { HttpContext = context })));
    }

    private static DefaultHttpContext NewContext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim("sub", Guid.NewGuid().ToString("D")) }, "AuthGateBearer"));
        return context;
    }
}
