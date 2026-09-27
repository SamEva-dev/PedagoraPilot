using PedagoraPilot.Application.Abstractions.Messaging;
using PedagoraPilot.Contracts.Administration;

namespace PedagoraPilot.Application.Organizations.Administration;

public sealed record GetOrganizationAdministrationQuery(Guid OrganizationId) : IQuery<OrganizationAdministrationDto>;

public sealed record UpdateOrganizationAdministrationCommand(Guid OrganizationId, UpdateOrganizationAdministrationRequest Request)
    : ICommand<OrganizationAdministrationDto>;

public sealed record SubmitOrganizationVerificationCommand(Guid OrganizationId) : ICommand<OrganizationAdministrationDto>;
public sealed record CompleteOrganizationOnboardingCommand(Guid OrganizationId) : ICommand<OrganizationAdministrationDto>;
public sealed record SetOrganizationVerificationStatusCommand(Guid OrganizationId, OrganizationVerificationStatusRequest Request) : ICommand<OrganizationAdministrationDto>;
public sealed record UpdateOrganizationCommercialStateCommand(Guid OrganizationId, UpdateOrganizationCommercialStateRequest Request) : ICommand<OrganizationAdministrationDto>;
