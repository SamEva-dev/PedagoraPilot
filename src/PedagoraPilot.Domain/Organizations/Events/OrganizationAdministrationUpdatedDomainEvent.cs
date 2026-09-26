using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Organizations.Events;

public sealed record OrganizationAdministrationUpdatedDomainEvent(Guid OrganizationId) : DomainEvent;
