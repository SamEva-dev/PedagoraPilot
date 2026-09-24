namespace PedagoraPilot.Contracts.Provisioning;
public sealed record ProvisionOrganizationRequest
{
    public Guid ExternalUserId { get; init; }
    public string LegalName { get; init; } = string.Empty;
    public string? CountryCode { get; init; }
    public ProvisionOrganizationOwner Owner { get; init; } = new();
}

public sealed record ProvisionOrganizationOwner
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
}
