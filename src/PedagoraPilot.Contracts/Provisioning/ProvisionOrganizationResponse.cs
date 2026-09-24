namespace PedagoraPilot.Contracts.Provisioning;
public sealed record ProvisionOrganizationResponse
{
    public Guid OrganizationId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}
