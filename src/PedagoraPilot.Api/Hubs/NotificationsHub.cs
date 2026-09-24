using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PedagoraPilot.Api.Hubs;
[Authorize]
public sealed class NotificationsHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var rawOrganizationId = Context.User?.FindFirst("organization_id")?.Value ?? Context.User?.FindFirst("org_id")?.Value;
        if (Guid.TryParse(rawOrganizationId, out var organizationId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(organizationId));
        }

        await base.OnConnectedAsync();
    }

    public static string GroupName(Guid organizationId) => $"organization:{organizationId:D}";
}
