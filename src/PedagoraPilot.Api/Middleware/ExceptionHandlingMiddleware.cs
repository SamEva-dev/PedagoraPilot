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
        // The first attendance write may race against another transaction which
        // inserts the same session sheet. Preserve the database's unique index as
        // final arbiter, and expose a retryable conflict rather than a generic 500.
        catch (DbUpdateException exception) when (
            exception.InnerException is Npgsql.PostgresException pg
            && pg.SqlState == Npgsql.PostgresErrorCodes.UniqueViolation
            && ((pg.TableName?.Equals("attendance_sheets", StringComparison.OrdinalIgnoreCase) ?? false)
                || (pg.ConstraintName?.Contains("attendance_sheets", StringComparison.OrdinalIgnoreCase) ?? false)))
        {
            _logger.LogWarning(exception, "Concurrent attendance sheet initialization.");
            await WriteAsync(context, StatusCodes.Status409Conflict, ErrorKeys.ConcurrencyConflict);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is Npgsql.PostgresException pg
            && pg.SqlState == Npgsql.PostgresErrorCodes.UniqueViolation
            && string.Equals(pg.ConstraintName, "ux_signature_source_revision", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(exception, "Concurrent signature draft creation.");
            await WriteAsync(context, StatusCodes.Status409Conflict, ErrorKeys.SignatureDuplicateSource);
        }
        // Database unique indexes are the final arbiter when two HTTP requests
        // pass the application-level duplicate check concurrently.
        catch (DbUpdateException exception) when (
            exception.InnerException is Npgsql.PostgresException pg
            && pg.SqlState == Npgsql.PostgresErrorCodes.UniqueViolation
            && EnrollmentJuryUniqueConflict.TryResolve(pg.ConstraintName, out _))
        {
            var pgs = (Npgsql.PostgresException)exception.InnerException!;
            EnrollmentJuryUniqueConflict.TryResolve(pgs.ConstraintName, out var errorKey);
            _logger.LogWarning(exception, "Concurrent jury assignment or enrollment rejected by unique index.");
            await WriteAsync(context, StatusCodes.Status409Conflict, errorKey);
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
