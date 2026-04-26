using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace _4BetWebApi.Hubs;

public class MatchHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? Context.User?.FindFirst("nameid")?.Value;
        if (Guid.TryParse(userId, out var parsedUserId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(parsedUserId));
        }

        await base.OnConnectedAsync();
    }

    public Task SubscribeEvent(string externalId)
        => Groups.AddToGroupAsync(Context.ConnectionId, EventGroup(externalId));

    public Task UnsubscribeEvent(string externalId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, EventGroup(externalId));

    public static string EventGroup(string externalId) => $"event:{externalId}";
    public static string UserGroup(Guid userId) => $"user:{userId}";
}