using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace PedagoraPilot.Api.Authorization;

/// <summary>
/// Configures the Pedagora JWT bearer scheme with a DI-backed AuthGate signing-key provider.
/// The AllowHttpForDevelopment setting is read directly from IConfiguration because
/// AuthGateOptions does not expose that property.
/// </summary>
public sealed class AuthGateJwtBearerOptionsConfigurator(
    AuthGateSigningKeyProvider signingKeys,
    IOptions<AuthGateOptions> authGateOptions,
    IConfiguration configuration,
    ILogger<AuthGateJwtBearerOptionsConfigurator> logger)
    : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly AuthGateOptions _authGate = authGateOptions.Value;

    public void Configure(JwtBearerOptions options) =>
        Configure(JwtBearerDefaults.AuthenticationScheme, options);

    public void Configure(string? name, JwtBearerOptions options)
    {
        if (!string.Equals(name, JwtBearerDefaults.AuthenticationScheme, StringComparison.Ordinal))
            return;

        options.RequireHttpsMetadata =
            !configuration.GetValue("AuthGate:AllowHttpForDevelopment", false);

        options.SaveToken = false;
        options.IncludeErrorDetails = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeyResolver = (_, _, kid, _) => signingKeys.Resolve(kid),

            ValidateIssuer = true,
            ValidIssuer = _authGate.Issuer,

            ValidateAudience = true,
            ValidAudience = _authGate.Audience,

            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.FromSeconds(30),

            NameClaimType = "email",
            RoleClaimType = ClaimTypes.Role,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256]
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = async context =>
            {
                if (string.IsNullOrWhiteSpace(context.Token) &&
                    context.Request.Path.StartsWithSegments("/hubs/notifications"))
                {
                    context.Token = context.Request.Query["access_token"];
                }

                var rawToken = context.Token;

                if (string.IsNullOrWhiteSpace(rawToken))
                {
                    var authorization = context.Request.Headers.Authorization.ToString();
                    if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        rawToken = authorization["Bearer ".Length..].Trim();
                }

                if (string.IsNullOrWhiteSpace(rawToken))
                    return;

                string? kid = null;
                try
                {
                    kid = new JwtSecurityTokenHandler()
                        .ReadJwtToken(rawToken)
                        .Header.Kid;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(
                        ex,
                        "Unable to read JWT header before AuthGate key resolution. Path={Path}",
                        context.Request.Path);
                }

                if (!signingKeys.HasKeys ||
                    (!string.IsNullOrWhiteSpace(kid) && !signingKeys.ContainsKey(kid)))
                {
                    await signingKeys.RefreshAsync(context.HttpContext.RequestAborted)
                        .ConfigureAwait(false);
                }
            },

            OnAuthenticationFailed = async context =>
            {
                logger.LogError(
                    context.Exception,
                    "JWT authentication failed. Path={Path} ExceptionType={ExceptionType} Message={Message}",
                    context.Request.Path,
                    context.Exception.GetType().FullName,
                    context.Exception.Message);

                if (context.Exception is SecurityTokenSignatureKeyNotFoundException)
                {
                    try
                    {
                        await signingKeys.RefreshAsync(context.HttpContext.RequestAborted)
                            .ConfigureAwait(false);
                    }
                    catch (Exception refreshException)
                    {
                        logger.LogWarning(
                            refreshException,
                            "Unable to refresh AuthGate JWKS after signature key miss.");
                    }
                }
            },

            OnChallenge = context =>
            {
                logger.LogWarning(
                    "JWT challenge. Path={Path} Error={Error} Description={Description}",
                    context.Request.Path,
                    context.Error,
                    context.ErrorDescription);

                return Task.CompletedTask;
            },

            OnTokenValidated = context =>
            {
                var principal = context.Principal;

                logger.LogDebug(
                    "JWT validated. Path={Path} Subject={Subject} Scope={Scope}",
                    context.Request.Path,
                    principal?.FindFirst("sub")?.Value,
                    principal?.FindFirst("scope")?.Value);

                return Task.CompletedTask;
            }
        };
    }
}
