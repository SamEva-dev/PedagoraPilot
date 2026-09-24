using PedagoraPilot.Application.Abstractions.Messaging;
using PedagoraPilot.Contracts.Provisioning;

namespace PedagoraPilot.Application.Organizations.Queries;
public sealed record GetOrganizationVerificationQuery(Guid OrganizationId) : IQuery<OrganizationVerificationResponse?>;
