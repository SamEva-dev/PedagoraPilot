using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using PedagoraPilot.Api.Middleware;
using Xunit;

namespace PedagoraPilot.Api.UnitTests;
public sealed class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task Middleware_registers_required_security_headers()
    {
        var context = new DefaultHttpContext();
        var responseFeature = new StartingResponseFeature();
        context.Features.Set<IHttpResponseFeature>(responseFeature);
        var middleware = new SecurityHeadersMiddleware(async ctx =>
        {
            ctx.Response.StatusCode = 204;
            await responseFeature.StartAsync();
        });
        await middleware.InvokeAsync(context);
        context.Response.Headers["X-Content-Type-Options"].ToString().Should().Be("nosniff");
        context.Response.Headers["X-Frame-Options"].ToString().Should().Be("DENY");
        context.Response.Headers["Referrer-Policy"].ToString().Should().Be("strict-origin-when-cross-origin");
        context.Response.Headers["Content-Security-Policy"].ToString().Should().Contain("frame-ancestors 'none'");
    }

    private sealed class StartingResponseFeature : HttpResponseFeature
    {
        private readonly Stack<(Func<object, Task> Callback, object State)> callbacks = new();
        public override void OnStarting(Func<object, Task> callback, object state) => callbacks.Push((callback, state));
        public async Task StartAsync()
        {
            while (callbacks.TryPop(out var callback))
                await callback.Callback(callback.State);
        }
    }
}
