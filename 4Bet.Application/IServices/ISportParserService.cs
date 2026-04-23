using _4Bet.Application.DTOs.External;

namespace _4Bet.Application.IServices;

public interface ISportParserService
{
    public Task<List<OddsApiResponse>?> GetFootballOddsAsync(CancellationToken cancellationToken = default);
}