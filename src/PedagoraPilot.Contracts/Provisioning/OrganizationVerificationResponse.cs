namespace PedagoraPilot.Contracts.Provisioning;
public sealed record OrganizationVerificationResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}
