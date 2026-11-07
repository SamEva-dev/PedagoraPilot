using DomainRelay.Exceptions;
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

    [Theory]
    [InlineData(409, "ACCESS_MEMBER_ALREADY_EXISTS")]
    [InlineData(409, "ACCESS_CANNOT_INVITE_SELF")]
    [InlineData(409, "SITE_CODE_ALREADY_EXISTS")]
    [InlineData(403, "AUTH_FORBIDDEN")]
    [InlineData(404, "LEARNER_NOT_FOUND")]
    public async Task DomainRelay_wrapped_application_errors_retain_status_and_code(int status, string code)
    {
        Exception underlying = status switch
        {
            403 => new ForbiddenApplicationException(code),
            404 => new NotFoundApplicationException(code),
            409 => new ConflictApplicationException(code),
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };
        using var body = new MemoryStream();
        var context = new DefaultHttpContext();
        context.Response.Body = body;
        var wrapped = new DomainRelayException("Mediator failed", underlying);
        var middleware = new ExceptionHandlingMiddleware(
            _ => Task.FromException(wrapped), NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);
        Assert.Equal(status, context.Response.StatusCode);
        body.Position = 0;
        using var json = await JsonDocument.ParseAsync(body);
        Assert.Equal(code, json.RootElement.GetProperty("code").GetString());
        Assert.Equal(status, json.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task DomainRelay_wrapped_unknown_error_is_not_exposed()
    {
        using var body = new MemoryStream();
        var context = new DefaultHttpContext();
        context.Response.Body = body;
        var middleware = new ExceptionHandlingMiddleware(
            _ => Task.FromException(new DomainRelayException("wrapper", new InvalidOperationException("private data"))),
            NullLogger<ExceptionHandlingMiddleware>.Instance);
        await middleware.InvokeAsync(context);
        Assert.Equal(500, context.Response.StatusCode);
        body.Position = 0;
        using var json = await JsonDocument.ParseAsync(body);
        Assert.Equal(ErrorKeys.Unexpected, json.RootElement.GetProperty("code").GetString());
        Assert.DoesNotContain("private data", json.RootElement.ToString(), StringComparison.Ordinal);
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
