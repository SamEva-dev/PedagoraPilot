using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using PedagoraPilot.Api.Authorization;
using PedagoraPilot.Application.Abstractions.Security;

namespace PedagoraPilot.Api.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddPedagoraAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AuthGateOptions>(
            configuration.GetSection(AuthGateOptions.SectionName));

        services.AddHttpContextAccessor();

        var authGate =
            configuration.GetSection(AuthGateOptions.SectionName)
                .Get<AuthGateOptions>()
            ?? new AuthGateOptions();

        if (string.IsNullOrWhiteSpace(authGate.BaseUrl) ||
            string.IsNullOrWhiteSpace(authGate.Issuer) ||
            string.IsNullOrWhiteSpace(authGate.Audience))
        {
            throw new InvalidOperationException(
                "AuthGate BaseUrl, Issuer and Audience are required.");
        }

        services.AddHttpClient("AuthGateJwks", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        services.AddSingleton<AuthGateSigningKeyProvider>();

        // DI-backed named-options configuration.
        // This keeps the signing-key provider directly captured by the JwtBearer options
        // through dependency injection.
        services.AddSingleton<
            IConfigureOptions<JwtBearerOptions>,
            AuthGateJwtBearerOptionsConfigurator>();

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                    JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme =
                    JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer();

        // OIDC is an opt-in, identity-only scheme. Legacy JWT remains the default.
        // No organization, role, or business permission is derived from the OIDC token.
        if (configuration.GetValue<bool>("OidcApi:Enabled"))
        {
            var issuer = configuration["OidcApi:Issuer"];
            var audience = configuration["OidcApi:Audience"];
            if (!Uri.TryCreate(issuer, UriKind.Absolute, out var issuerUri) ||
                issuerUri.Scheme != Uri.UriSchemeHttps ||
                string.IsNullOrWhiteSpace(audience))
                throw new InvalidOperationException("OidcApi Issuer (HTTPS) and Audience are required.");

            services.AddAuthentication()
                .AddJwtBearer("AuthGateOidc", options =>
                {
                    options.Authority = issuerUri.GetLeftPart(UriPartial.Authority);
                    options.RequireHttpsMetadata = true;
                    options.MapInboundClaims = false;
                    options.IncludeErrorDetails = true;
                    options.SaveToken = false;
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var hubPath = configuration["Realtime:HubPath"] ?? "/hubs/notifications";
                            if (context.HttpContext.Request.Path.StartsWithSegments(hubPath, StringComparison.OrdinalIgnoreCase))
                            {
                                var token = context.Request.Query["access_token"].ToString();
                                if (!string.IsNullOrWhiteSpace(token)) context.Token = token;
                            }
                            return Task.CompletedTask;
                        }
                    };
                    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = issuerUri.AbsoluteUri,
                        ValidateAudience = true,
                        ValidAudience = audience,
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = true,
                        RequireSignedTokens = true,
                        RequireExpirationTime = true,
                        ClockSkew = TimeSpan.FromSeconds(30),
                        NameClaimType = "sub"
                    };
                });
        }

        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                AuthorizationPolicies.AuthGateProvisioning,
                policy =>
                {
                    policy.RequireAuthenticatedUser();

                    policy.RequireAssertion(context =>
                    {
                        var expectedScope = authGate.ProvisioningScope;

                        return context.User
                            .FindAll(AuthGateClaimNames.Scope)
                            .SelectMany(claim =>
                                claim.Value.Split(
                                    ' ',
                                    StringSplitOptions.RemoveEmptyEntries |
                                    StringSplitOptions.TrimEntries))
                            .Contains(
                                expectedScope,
                                StringComparer.OrdinalIgnoreCase);
                    });
                });
        });

        return services;
    }
}
