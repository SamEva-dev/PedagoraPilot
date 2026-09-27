using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using PedagoraPilot.Application.Abstractions.Identity;

namespace PedagoraPilot.Infrastructure.Identity;

public sealed class AuthGateApplicationIdentityAccess(IHttpClientFactory httpClientFactory, IConfiguration configuration) : IApplicationIdentityAccess
{
    public async Task<EnsureApplicationIdentityResult> EnsureAsync(EnsureApplicationIdentityRequest request, CancellationToken cancellationToken = default)
    {
        var baseUrl = configuration["AuthGate:BaseUrl"]?.TrimEnd('/') ?? throw new InvalidOperationException("AuthGate:BaseUrl is required.");
        var apiKey = configuration["AuthGate:InternalApplicationAccessApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey)) throw new InvalidOperationException("AuthGate:InternalApplicationAccessApiKey is required.");
        using var message = new HttpRequestMessage(HttpMethod.Post, baseUrl + "/internal/application-access/ensure");
        message.Headers.TryAddWithoutValidation("X-Internal-Api-Key", apiKey);
        message.Content = JsonContent.Create(new
        {
            appId = "pedagora-pilot-web",
            request.OrganizationId,
            request.Email,
            request.FirstName,
            request.LastName,
            role = request.Role,
            initialPassword = request.InitialPassword,
            request.EmailConfirmed
        });
        var client = httpClientFactory.CreateClient("AuthGateInternalAccess");
        using var response = await client.SendAsync(message, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"AuthGate application access provisioning failed ({(int)response.StatusCode}): {body}");
        var dto = System.Text.Json.JsonSerializer.Deserialize<Response>(body, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Invalid AuthGate provisioning response.");
        return new EnsureApplicationIdentityResult(dto.UserId, dto.ExistingIdentity, dto.AccessActive);
    }
    private sealed record Response(Guid UserId, bool ExistingIdentity, bool AccessActive);

    public async Task SetRoleAsync(SetApplicationRoleRequest request, CancellationToken cancellationToken = default)
    {
        var baseUrl = configuration["AuthGate:BaseUrl"]?.TrimEnd('/') ?? throw new InvalidOperationException("AuthGate:BaseUrl is required.");
        var apiKey = configuration["AuthGate:InternalApplicationAccessApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey)) throw new InvalidOperationException("AuthGate:InternalApplicationAccessApiKey is required.");
        using var message = new HttpRequestMessage(HttpMethod.Put, baseUrl + "/internal/application-access/role");
        message.Headers.TryAddWithoutValidation("X-Internal-Api-Key", apiKey);
        message.Content = JsonContent.Create(new
        {
            appId = "pedagora-pilot-web",
            request.OrganizationId,
            request.UserId,
            role = request.Role
        });
        var client = httpClientFactory.CreateClient("AuthGateInternalAccess");
        using var response = await client.SendAsync(message, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"AuthGate application role update failed ({(int)response.StatusCode}): {body}");
    }

}
