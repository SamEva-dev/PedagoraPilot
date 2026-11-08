using Microsoft.AspNetCore.Authorization;
using System.Net;
using System.Text.Json;
using PedagoraPilot.Api.Authorization;
using PedagoraPilot.Application.Abstractions.Persistence;
using PedagoraPilot.Application.Abstractions.Security;

namespace PedagoraPilot.Api.Middleware;

/// <summary>
/// Fail-closed product-side membership check on every authenticated tenant request.
/// A previously issued JWT is not evidence that a revoked Pedagora membership is still valid.
/// No caching: suspensions/revocations in the product database apply to the next request.
/// </summary>
public sealed class ActiveWorkspaceAuthorizationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ICurrentUser current,
        IOrganizationMemberRepository members, IOrganizationRepository organizations,
        IHttpClientFactory clients, IConfiguration configuration,
        ILogger<ActiveWorkspaceAuthorizationMiddleware> logger)
    {
        var path = context.Request.Path;
        var isBusiness = path.StartsWithSegments("/api/v1", StringComparison.OrdinalIgnoreCase) ||
                         path.StartsWithSegments("/hubs/notifications", StringComparison.OrdinalIgnoreCase);
        if (!isBusiness || !current.IsAuthenticated ||
            context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null ||
            // Organization discovery and OIDC bootstrap precede tenant selection.
            string.Equals(path.Value, "/api/v1/me/organization-labels", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/api/v1/oidc", StringComparison.OrdinalIgnoreCase) ||
            (string.Equals(path.Value, "/api/v1/session/bootstrap", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(path.Value, "/api/v1/session/context", StringComparison.OrdinalIgnoreCase)))
        {
            await next(context);
            return;
        }

        // The OIDC business middleware already checks product membership and
        // live AuthGate effective access, using an audience-bound token.
        if (context.User.Identity?.AuthenticationType == "AuthGateOidcBusiness")
        {
            await next(context);
            return;
        }

        if (current.UserId is not Guid userId || userId == Guid.Empty ||
            current.OrganizationId is not Guid orgId || orgId == Guid.Empty ||
            !WorkspaceAuthorizationPolicy.RouteWorkspaceMatches(context, orgId))
        {
            await RejectAsync(context, StatusCodes.Status403Forbidden, "WORKSPACE_CONTEXT_INVALID");
            return;
        }

        try
        {
            var org = await organizations.GetByIdAsync(orgId, false, context.RequestAborted);
            var member = await members.GetByAuthGateUserAsync(orgId, userId, false, context.RequestAborted);
            // An owner can be provisioned before a member projection exists. However,
            // an explicitly inactive owner membership must NEVER be bypassed by ownership.
            if (!WorkspaceAuthorizationPolicy.CanAccess(org?.IsLoginAllowed == true,
                    org?.OwnerUserId == userId, member is not null, member?.IsActive == true))
            {
                await RejectAsync(context, StatusCodes.Status403Forbidden, "WORKSPACE_MEMBERSHIP_REVOKED");
                return;
            }
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to validate current product membership");
            await RejectAsync(context, StatusCodes.Status503ServiceUnavailable, "WORKSPACE_AUTHORIZATION_UNAVAILABLE");
            return;
        }

        // A legacy JWT can remain cryptographically valid after the identity
        // provider revokes the membership. Consult AuthGate on every request.
        var upstream = await CheckAuthGateAsync(clients, configuration, userId, orgId, context.RequestAborted);
        if (upstream != MembershipState.Active)
        {
            await RejectAsync(context,
                upstream == MembershipState.Revoked ? StatusCodes.Status403Forbidden : StatusCodes.Status503ServiceUnavailable,
                upstream == MembershipState.Revoked ? "AUTHGATE_MEMBERSHIP_REVOKED" : "AUTHGATE_MEMBERSHIP_UNAVAILABLE");
            return;
        }
        await next(context);
    }

    private enum MembershipState { Active, Revoked, Unavailable }

    private static async Task<MembershipState> CheckAuthGateAsync(IHttpClientFactory clients,
        IConfiguration configuration, Guid subject, Guid organization, CancellationToken ct)
    {
        var key = configuration["AuthGate:InternalApplicationAccessApiKey"];
        var baseUrl = configuration["AuthGate:BaseUrl"];
        var appId = configuration["AuthGate:ClientId"];
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(appId) ||
            !Uri.TryCreate(baseUrl, UriKind.Absolute, out var root) ||
            (root.Scheme != Uri.UriSchemeHttps && root.Scheme != Uri.UriSchemeHttp) ||
            !string.IsNullOrEmpty(root.UserInfo))
            return MembershipState.Unavailable;

        var endpoint = new Uri(root.ToString().TrimEnd('/') +
            "/internal/application-access/membership-state?appId=" + Uri.EscapeDataString(appId) +
            "&organizationId=" + organization.ToString("D") +
            "&userId=" + subject.ToString("D"));
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.TryAddWithoutValidation("X-Internal-Api-Key", key);
        try
        {
            using var response = await clients.CreateClient("AuthGateMembershipState")
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (response.StatusCode != HttpStatusCode.OK ||
                response.Content.Headers.ContentLength is > 4096 ||
                !string.Equals(response.Content.Headers.ContentType?.MediaType,
                    "application/json", StringComparison.OrdinalIgnoreCase))
                return MembershipState.Unavailable;

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var limited = new MemoryStream();
            var buffer = new byte[1024];
            int read;
            while ((read = await stream.ReadAsync(buffer.AsMemory(), ct)) > 0)
            {
                if (limited.Length + read > 4096) return MembershipState.Unavailable;
                limited.Write(buffer, 0, read);
            }
            using var json = JsonDocument.Parse(limited.ToArray());
            var rootJson = json.RootElement;
            if (!rootJson.TryGetProperty("appId", out var remoteApp) ||
                !string.Equals(remoteApp.GetString(), appId, StringComparison.OrdinalIgnoreCase) ||
                !rootJson.TryGetProperty("organizationId", out var remoteOrg) ||
                !Guid.TryParse(remoteOrg.GetString(), out var confirmedOrg) || confirmedOrg != organization ||
                !rootJson.TryGetProperty("userId", out var remoteUser) ||
                !Guid.TryParse(remoteUser.GetString(), out var confirmedUser) || confirmedUser != subject ||
                !rootJson.TryGetProperty("active", out var active))
                return MembershipState.Unavailable;
            return active.ValueKind switch
            {
                JsonValueKind.True => MembershipState.Active,
                JsonValueKind.False => MembershipState.Revoked,
                _ => MembershipState.Unavailable
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or IOException or InvalidOperationException)
        {
            return MembershipState.Unavailable;
        }
    }

    private static Task RejectAsync(HttpContext context, int status, string code)
    {
        context.Response.StatusCode = status;
        context.Response.Headers.CacheControl = "no-store";
        return context.Response.WriteAsJsonAsync(new { code });
    }
}

/// <summary>Pure decisions used by the middleware and security regression tests.</summary>
public static class WorkspaceAuthorizationPolicy
{
    public static bool CanAccess(bool organizationPermitsLogin, bool isOwner,
        bool membershipExists, bool membershipActive) =>
        organizationPermitsLogin && (membershipExists ? membershipActive : isOwner);

    public static bool RouteWorkspaceMatches(HttpContext context, Guid selectedOrganization)
    {
        foreach (var key in new[] { "organizationId", "orgId", "tenantId" })
        {
            if (context.Request.RouteValues.TryGetValue(key, out var raw) &&
                (!Guid.TryParse(raw?.ToString(), out var fromRoute) || fromRoute != selectedOrganization))
                return false;
        }
        return true;
    }
}
