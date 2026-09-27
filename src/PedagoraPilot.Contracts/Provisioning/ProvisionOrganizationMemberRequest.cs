namespace PedagoraPilot.Contracts.Provisioning;

public sealed record ProvisionOrganizationMemberRequest
{
    public Guid OrganizationId { get; init; }
    public Guid AuthGateUserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();
    public bool Active { get; init; } = true;
}

public sealed record ProvisionOrganizationMemberResponse(Guid MemberId, Guid AuthGateUserId, string Status);
