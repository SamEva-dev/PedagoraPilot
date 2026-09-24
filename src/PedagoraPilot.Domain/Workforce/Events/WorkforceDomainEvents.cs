using PedagoraPilot.Domain.Common;
using PedagoraPilot.Domain.Identifiers;

namespace PedagoraPilot.Domain.Workforce.Events;
public sealed record RemoteWorkRequestedDomainEvent(RemoteWorkRequestId RequestId, Guid OrganizationId, Guid SiteId, Guid AuthGateUserId, string? RecipientEmail) : DomainEvent;
public sealed record RemoteWorkDecisionRecordedDomainEvent(RemoteWorkRequestId RequestId, Guid OrganizationId, RemoteWorkRequestStatus Status, string? RecipientEmail) : DomainEvent;
public sealed record RemoteWorkActivityUpdatedDomainEvent(RemoteWorkRequestId RequestId, Guid OrganizationId, RemoteWorkActivityId ActivityId, RemoteWorkActivityStatus Status) : DomainEvent;
