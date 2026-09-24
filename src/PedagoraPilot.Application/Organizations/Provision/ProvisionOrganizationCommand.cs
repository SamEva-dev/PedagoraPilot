using PedagoraPilot.Application.Abstractions.Messaging;
using PedagoraPilot.Contracts.Provisioning;

namespace PedagoraPilot.Application.Organizations.Provision;
public sealed record ProvisionOrganizationCommand(Guid ExternalUserId, string LegalName, string? CountryCode, string FirstName, string LastName, string Email, string? Phone) : ICommand<ProvisionOrganizationResponse>;
