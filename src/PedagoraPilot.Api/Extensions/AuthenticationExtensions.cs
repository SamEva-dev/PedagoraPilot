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
        // and removes the previous InstancePropertyBag workaround.
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
