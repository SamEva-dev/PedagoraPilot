using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Organizations.Events;
public sealed record OrganizationProvisionedDomainEvent(Guid OrganizationId, Guid OwnerUserId, string Code, string LegalName) : DomainEvent;
