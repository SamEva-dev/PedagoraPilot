namespace PedagoraPilot.Contracts.Administration;

public sealed record OrganizationVerificationStatusRequest(string Status, string Comment);

public sealed record UpdateOrganizationCommercialStateRequest(
    string Plan,
    string Status,
    DateTime? TrialEndsAtUtc,
    string BillingCustomerReference);
