using _4Bet.Application.DTOs;

namespace _4Bet.Application.IServices;

public interface IBetAnalyticsService
{
    Task<BetAnalyticsDto> GetUserAnalyticsAsync(Guid userId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
}
