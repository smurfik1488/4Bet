using _4Bet.Application.DTOs;
using _4Bet.Application.IServices;
using _4Bet.Application.Realtime;
using _4BetWebApi.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace _4BetWebApi.Services;

public class SignalRNotificationService(IHubContext<MatchHub> hubContext) : ISportNotificationService
{
    public async Task BroadcastMatchStateUpdatedAsync(IEnumerable<SportEventDto> updatedEvents)
    {
        var events = updatedEvents.ToList();
        await hubContext.Clients.All.SendAsync(SignalREventNames.MatchStateUpdated, events);

        foreach (var ev in events)
        {
            await hubContext.Clients.Group(MatchHub.EventGroup(ev.ExternalId))
                .SendAsync(SignalREventNames.MatchStateUpdated, new[] { ev });
        }
    }

    public async Task BroadcastOddsUpdatedAsync(IEnumerable<OddsUpdateDto> updatedOdds)
    {
        var odds = updatedOdds.ToList();
        await hubContext.Clients.All.SendAsync(SignalREventNames.OddsUpdated, odds);

        foreach (var update in odds)
        {
            await hubContext.Clients.Group(MatchHub.EventGroup(update.ExternalId))
                .SendAsync(SignalREventNames.OddsUpdated, new[] { update });
        }
    }

    public async Task BroadcastBetAcceptedAsync(Guid userId, BetDto bet)
    {
        await hubContext.Clients.Group(MatchHub.UserGroup(userId)).SendAsync(SignalREventNames.BetAccepted, bet);
    }

    public async Task BroadcastBetSettledAsync(Guid userId, BetLifecycleUpdateDto update)
    {
        await hubContext.Clients.Group(MatchHub.UserGroup(userId)).SendAsync(SignalREventNames.BetSettled, update);
    }
}