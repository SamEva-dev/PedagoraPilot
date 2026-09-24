using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PedagoraPilot.Application.Common.Errors;
using PedagoraPilot.Contracts.Common;
using PedagoraPilot.Infrastructure.Persistence.Idempotency;

namespace PedagoraPilot.Api.Middleware;
public sealed class IdempotencyMiddleware
{
    public const string HeaderName = "Idempotency-Key";
    private static readonly HashSet<string> UnsafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "POST",
        "PUT",
        "PATCH",
        "DELETE"
    };
    private readonly RequestDelegate _next;
    public IdempotencyMiddleware(RequestDelegate next) => _next = next;
    public async Task InvokeAsync(HttpContext context, IdempotencyStore store, IOptions<IdempotencyOptions> optionsAccessor)
    {
        var options = optionsAccessor.Value;
        if (!options.Enabled || !ShouldApply(context))
        {
            await _next(context);
            return;
        }

        var key = context.Request.Headers[HeaderName].ToString().Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            if (!options.RequireForUnsafeMethods)
            {
                await _next(context);
                return;
            }

            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, ErrorKeys.IdempotencyKeyRequired);
            return;
        }

        if (key.Length > 200)
        {
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, ErrorKeys.IdempotencyKeyConflict);
            return;
        }

        context.Request.EnableBuffering();
        var requestHash = await ComputeRequestHashAsync(context.Request, context.RequestAborted);
        var scope = BuildScope(context);
        var existing = await store.FindAsync(key, scope, isTracking: false, context.RequestAborted);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
            {
                await WriteErrorAsync(context, StatusCodes.Status409Conflict, ErrorKeys.IdempotencyKeyConflict);
                return;
            }

            if (existing.Status == "Completed" && existing.ResponseStatusCode.HasValue)
            {
                context.Response.StatusCode = existing.ResponseStatusCode.Value;
                if (!string.IsNullOrWhiteSpace(existing.ResponseContentType))
                    context.Response.ContentType = existing.ResponseContentType;
                context.Response.Headers["Idempotency-Replayed"] = "true";
                if (!string.IsNullOrEmpty(existing.ResponseBody))
                    await context.Response.WriteAsync(existing.ResponseBody, context.RequestAborted);
                return;
            }

            await WriteErrorAsync(context, StatusCodes.Status409Conflict, ErrorKeys.IdempotencyKeyConflict);
            return;
        }

        var record = new IdempotencyRequest
        {
            Id = Guid.NewGuid(),
            Key = key,
            Scope = scope,
            RequestHash = requestHash,
            Status = "Processing",
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(Math.Max(1, options.RetentionHours))
        };
        try
        {
            await store.AddProcessingAsync(record, context.RequestAborted);
        }
        catch (DbUpdateException)
        {
            // Concurrent request won the unique key race. Retry through the same semantics.
            var raced = await store.FindAsync(key, scope, isTracking: false, context.RequestAborted);
            if (raced?.Status == "Completed" && raced.RequestHash == requestHash && raced.ResponseStatusCode.HasValue)
            {
                context.Response.StatusCode = raced.ResponseStatusCode.Value;
                context.Response.ContentType = raced.ResponseContentType;
                context.Response.Headers["Idempotency-Replayed"] = "true";
                if (!string.IsNullOrEmpty(raced.ResponseBody))
                    await context.Response.WriteAsync(raced.ResponseBody, context.RequestAborted);
                return;
            }

            await WriteErrorAsync(context, StatusCodes.Status409Conflict, ErrorKeys.IdempotencyKeyConflict);
            return;
        }

        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;
        try
        {
            await _next(context);
            buffer.Position = 0;
            string responseBody;
            if (buffer.Length <= options.MaxResponseBytes)
            {
                using var reader = new StreamReader(buffer, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                responseBody = await reader.ReadToEndAsync(context.RequestAborted);
            }
            else
            {
                responseBody = string.Empty;
            }

            buffer.Position = 0;
            await buffer.CopyToAsync(originalBody, context.RequestAborted);
            if (context.Response.StatusCode < 500)
            {
                await store.CompleteAsync(record.Id, context.Response.StatusCode, context.Response.ContentType, responseBody, context.RequestAborted);
            }
            else
            {
                await store.RemoveAsync(record.Id, context.RequestAborted);
            }
        }
        catch
        {
            await store.RemoveAsync(record.Id, CancellationToken.None);
            throw;
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    private static bool ShouldApply(HttpContext context) => UnsafeMethods.Contains(context.Request.Method) && context.Request.Path.StartsWithSegments("/api") && !context.Request.Path.StartsWithSegments("/api/webhooks");
    private static string BuildScope(HttpContext context)
    {
        var subject = context.User.FindFirst("sub")?.Value ?? "anonymous";
        var organization = context.User.FindFirst("organization_id")?.Value ?? context.User.FindFirst("org_id")?.Value ?? "none";
        return $"{subject}|{organization}|{context.Request.Method}|{context.Request.Path}";
    }

    private static async Task<string> ComputeRequestHashAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        request.Body.Position = 0;
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(request.Body, cancellationToken);
        request.Body.Position = 0;
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static Task WriteErrorAsync(HttpContext context, int statusCode, string code)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        return context.Response.WriteAsJsonAsync(new ApiErrorResponse(code, statusCode, context.TraceIdentifier));
    }
}
