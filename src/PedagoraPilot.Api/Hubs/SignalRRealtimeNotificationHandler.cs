using System.Text.Json;
using DomainRelay.Abstractions;
using Microsoft.AspNetCore.SignalR;
using PedagoraPilot.Application.Common.Realtime;

namespace PedagoraPilot.Api.Hubs;
public sealed class SignalRRealtimeNotificationHandler : INotificationHandler<RealtimeDomainEventNotification>
{
    private readonly IHubContext<NotificationsHub> _hubContext;
    public SignalRRealtimeNotificationHandler(IHubContext<NotificationsHub> hubContext) => _hubContext = hubContext;
    public async Task Handle(RealtimeDomainEventNotification notification, CancellationToken ct)
    {
        using var payload = JsonDocument.Parse(notification.PayloadJson);
        await _hubContext.Clients.Group(NotificationsHub.GroupName(notification.OrganizationId)).SendAsync("domainEvent", new { notification.EventId, notification.TypeKey, notification.OccurredOnUtc, Payload = payload.RootElement.Clone() }, ct);
    }
}
