namespace PedagoraPilot.Api.Authorization;
public sealed class AuthGateOptions
{
    public const string SectionName = "AuthGate";
    public string BaseUrl { get; set; } = "https://localhost:8081";
    public string Issuer { get; set; } = "AuthGate";
    public string Audience { get; set; } = "PedagoraPilot";
    public string ClientId { get; set; } = "pedagora-pilot-web";
    public string ApplicationCode { get; set; } = "pedagora-pilot";
    public string JwksPath { get; set; } = "/.well-known/jwks.json";
    public string ProvisioningScope { get; set; } = "pedagora-pilot.provisioning";
}
