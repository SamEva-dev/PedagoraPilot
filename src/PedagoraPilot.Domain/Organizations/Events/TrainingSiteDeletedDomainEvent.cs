using PedagoraPilot.Domain.Common;

namespace PedagoraPilot.Domain.Organizations.Events;

public sealed record TrainingSiteDeletedDomainEvent(Guid SiteId, Guid OrganizationId) : DomainEvent;
