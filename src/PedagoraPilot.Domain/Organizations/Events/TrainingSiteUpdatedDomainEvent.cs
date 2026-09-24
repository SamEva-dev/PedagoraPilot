using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Organizations.Events;
public sealed record TrainingSiteUpdatedDomainEvent(Guid SiteId, Guid OrganizationId, string Code, string Name) : DomainEvent;
