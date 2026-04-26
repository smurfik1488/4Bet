using _4Bet.Application.DTOs;

namespace _4Bet.Application.IServices;

public interface IBetService
{
    Task<BetDto> PlaceBetAsync(Guid userId, PlaceBetRequestDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BetDto>> GetMyBetsAsync(Guid userId);
    Task<BetDto?> GetMyBetByIdAsync(Guid userId, Guid betId);
    Task SettleByLiveEventsAsync(IEnumerable<string> eventExternalIds, CancellationToken cancellationToken = default);
}
