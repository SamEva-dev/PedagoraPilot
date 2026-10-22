using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using PedagoraPilot.Api.Middleware;
using PedagoraPilot.Application.Common.Errors;
using Xunit;

namespace PedagoraPilot.Api.UnitTests;

/// <summary>
/// HTTP serialization contract of the exception middleware. These tests do not
/// replace authentication, PostgreSQL or cross-tenant integration tests.
/// </summary>
public sealed class ExceptionHandlingHttpContractRegressionTests
{
    [Theory]
    [InlineData(403, "AUTH_FORBIDDEN")]
    [InlineData(409, "LEARNER_ALREADY_ENROLLED")]
    [InlineData(409, "CERTIFICATION_JURY_ALREADY_ASSIGNED")]
    public async Task Application_errors_return_expected_http_status_and_public_code(int status, string code)
    {
        Exception exception = status switch
        {
            403 => new ForbiddenApplicationException(code),
            409 => new ConflictApplicationException(code),
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };

        using var body = new MemoryStream();
        var context = new DefaultHttpContext();
        context.Response.Body = body;
        var middleware = new ExceptionHandlingMiddleware(
            _ => Task.FromException(exception),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(status, context.Response.StatusCode);
        body.Position = 0;
        using var json = await JsonDocument.ParseAsync(body);
        var root = json.RootElement;
        Assert.Equal(code, root.GetProperty("code").GetString());
        Assert.Equal(status, root.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));
        Assert.DoesNotContain("PostgresException", root.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StackTrace", root.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Unexpected_exception_returns_generic_500_without_internal_exception_message()
    {
        using var body = new MemoryStream();
        var context = new DefaultHttpContext();
        context.Response.Body = body;
        const string secretMarker = "internal-secret-sql-detail";
        var middleware = new ExceptionHandlingMiddleware(
            _ => Task.FromException(new InvalidOperationException(secretMarker)),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(500, context.Response.StatusCode);
        body.Position = 0;
        using var json = await JsonDocument.ParseAsync(body);
        Assert.Equal(ErrorKeys.Unexpected, json.RootElement.GetProperty("code").GetString());
        Assert.DoesNotContain(secretMarker, json.RootElement.ToString(), StringComparison.Ordinal);
    }
}
