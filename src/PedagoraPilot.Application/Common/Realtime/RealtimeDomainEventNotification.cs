using DomainRelay.Abstractions;

namespace PedagoraPilot.Application.Common.Realtime;
public sealed record RealtimeDomainEventNotification(Guid OrganizationId, Guid EventId, string TypeKey, DateTime OccurredOnUtc, string PayloadJson) : INotification;
