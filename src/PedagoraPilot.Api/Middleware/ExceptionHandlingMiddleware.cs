using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Contracts.Common;
using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Api.Middleware;
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException exception)
        {
            var errors = exception.Errors.GroupBy(failure => failure.PropertyName).ToDictionary(group => group.Key, group => group.Select(failure => string.IsNullOrWhiteSpace(failure.ErrorCode) ? ErrorKeys.ValidationFailed : failure.ErrorCode).Distinct(StringComparer.Ordinal).ToArray());
            await WriteAsync(context, StatusCodes.Status400BadRequest, ErrorKeys.ValidationFailed, errors: errors);
        }
        catch (ApplicationExceptionBase exception)
        {
            await WriteAsync(context, exception.StatusCode, exception.ErrorKey, exception.Parameters);
        }
        catch (DomainException exception)
        {
            await WriteAsync(context, StatusCodes.Status400BadRequest, exception.ErrorKey);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            _logger.LogWarning(exception, "Optimistic concurrency conflict.");
            await WriteAsync(context, StatusCodes.Status409Conflict, ErrorKeys.ConcurrencyConflict);
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning(exception, "Unauthorized operation.");
            await WriteAsync(context, StatusCodes.Status403Forbidden, ErrorKeys.Forbidden);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled request exception. TraceId={TraceId}", context.TraceIdentifier);
            await WriteAsync(context, StatusCodes.Status500InternalServerError, ErrorKeys.Unexpected);
        }
    }

    private static async Task WriteAsync(HttpContext context, int statusCode, string code, IReadOnlyDictionary<string, object?>? parameters = null, IReadOnlyDictionary<string, string[]>? errors = null)
    {
        if (context.Response.HasStarted)
            throw new InvalidOperationException("Cannot write an API error after the response has started.");
        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        var response = new ApiErrorResponse(code, statusCode, context.TraceIdentifier, parameters, errors);
        await context.Response.WriteAsJsonAsync(response);
    }
}
