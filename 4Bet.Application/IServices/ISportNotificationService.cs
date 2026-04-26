using _4Bet.Application.DTOs;

namespace _4Bet.Application.IServices;

public interface ISportNotificationService
{
    Task BroadcastMatchStateUpdatedAsync(IEnumerable<SportEventDto> updatedEvents);
    Task BroadcastOddsUpdatedAsync(IEnumerable<OddsUpdateDto> updatedOdds);
    Task BroadcastBetAcceptedAsync(Guid userId, BetDto bet);
    Task BroadcastBetSettledAsync(Guid userId, BetLifecycleUpdateDto update);
}