using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Organizations.Events;
public sealed record TrainingSiteCreatedDomainEvent(Guid SiteId, Guid OrganizationId, string Code, string Name) : DomainEvent;
