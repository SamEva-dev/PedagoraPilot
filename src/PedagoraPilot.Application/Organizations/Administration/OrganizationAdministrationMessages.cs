using PedagoraPilot.Application.Abstractions.Messaging;
using PedagoraPilot.Contracts.Administration;

namespace PedagoraPilot.Application.Organizations.Administration;

public sealed record GetOrganizationAdministrationQuery(Guid OrganizationId) : IQuery<OrganizationAdministrationDto>;

public sealed record UpdateOrganizationAdministrationCommand(Guid OrganizationId, UpdateOrganizationAdministrationRequest Request)
    : ICommand<OrganizationAdministrationDto>;
