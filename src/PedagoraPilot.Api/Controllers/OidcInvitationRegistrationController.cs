using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PedagoraPilot.Application.Access.Invitations;

namespace PedagoraPilot.Api.Controllers;

/// <summary>
/// Authenticated invitation acceptance for an existing AuthGate identity.
/// Does not accept identity fields, a password or an application role from the caller.
/// The invitation processor remains the authority for local membership creation.
/// </summary>
[ApiController]
[Route("api/v1/oidc/registration/invitation")]
[Authorize(AuthenticationSchemes = "AuthGateOidc")]
public sealed class OidcInvitationRegistrationController(
    IConfiguration configuration,
    IAccessInvitationProcessor invitations) : ControllerBase
{
    public sealed record AcceptRequest(string InvitationToken);
    private sealed record VerifiedIdentity(Guid Subject, string Audience, string Email);

    [HttpPost("accept")]
    public async Task<IActionResult> Accept([FromBody] AcceptRequest request, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        if (string.IsNullOrWhiteSpace(request.InvitationToken))
            return BadRequest(new { code = "ACCESS_INVITATION_TOKEN_REQUIRED" });

        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var subject) || subject == Guid.Empty)
            return Unauthorized();

        // No caller-provided email is trusted. Ask AuthGate for the verified email
        // using this API's signed, audience-bound OIDC access token.
        var identity = await ReadVerifiedIdentityAsync(subject, cancellationToken);
        if (identity is null)
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { code = "AUTHGATE_IDENTITY_VERIFICATION_UNAVAILABLE" });

        var preparation = await invitations.PrepareAsync(request.InvitationToken, cancellationToken);
        if (!preparation.ExistingActiveIdentity)
            return Conflict(new { code = "ACCESS_INVITATION_REQUIRES_FULL_REGISTRATION" });
        if (!string.Equals(preparation.Email.Trim(), identity.Email.Trim(), StringComparison.OrdinalIgnoreCase))
            return Forbid();

        try
        {
            var accepted = await invitations.AcceptAsync(request.InvitationToken, null, identity.Email, cancellationToken, subject);
            // Defensive check: the invitation must have been applied to the authenticated
            // AuthGate identity, never to another account sharing application metadata.
            if (accepted.UserId != subject)
                throw new InvalidOperationException("ACCESS_INVITATION_SUBJECT_MISMATCH");
            return Ok(new { accepted.UserId, accepted.Email, status = "Accepted", nextStep = "Bootstrap" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    private async Task<VerifiedIdentity?> ReadVerifiedIdentityAsync(Guid expectedSubject, CancellationToken ct)
    {
        var issuer = configuration["OidcApi:Issuer"];
        if (!Uri.TryCreate(issuer, UriKind.Absolute, out var issuerUri)
            || issuerUri.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrWhiteSpace(issuerUri.UserInfo)
            || !string.IsNullOrEmpty(issuerUri.Query)
            || !string.IsNullOrEmpty(issuerUri.Fragment))
            return null;

        var rawBearer = Request.Headers.Authorization.ToString();
        if (!rawBearer.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;
        var token = rawBearer[7..].Trim();
        if (token.Length is 0 or > 16384) return null;

        var origin = issuerUri.GetLeftPart(UriPartial.Authority);
        var url = origin + "/api/oidc/registration/verified-identity";
        using var handler = new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(8) };
        using var message = new HttpRequestMessage(HttpMethod.Get, url);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, ct);
        if (response.StatusCode != HttpStatusCode.OK ||
            response.Content.Headers.ContentType?.MediaType != "application/json" ||
            response.Content.Headers.ContentLength > 65536)
            return null;
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        while (true)
        {
            var read = await stream.ReadAsync(chunk.AsMemory(), ct);
            if (read == 0) break;
            if (buffer.Length + read > 65536) return null;
            buffer.Write(chunk, 0, read);
        }
        buffer.Position = 0;
        using var json = await JsonDocument.ParseAsync(buffer, cancellationToken: ct);
        var root = json.RootElement;
        if (!root.TryGetProperty("subject", out var subjectNode) ||
            !Guid.TryParse(subjectNode.ToString(), out var returnedSubject) ||
            returnedSubject != expectedSubject ||
            !root.TryGetProperty("audience", out var audienceNode) ||
            audienceNode.GetString() != configuration["OidcApi:Audience"] ||
            !root.TryGetProperty("email", out var emailNode) ||
            emailNode.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(emailNode.GetString()))
            return null;
        return new VerifiedIdentity(returnedSubject, audienceNode.GetString()!, emailNode.GetString()!);
    }
}
