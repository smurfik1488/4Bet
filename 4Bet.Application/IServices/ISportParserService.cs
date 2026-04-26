using _4Bet.Application.DTOs.External;

namespace _4Bet.Application.IServices;

public interface ISportParserService
{
    public Task<List<OddsApiResponse>?> GetFootballOddsAsync(string sportKey, CancellationToken cancellationToken = default);
    public Task<List<OddsScoreResponse>?> GetFootballScoresAsync(string sportKey, CancellationToken cancellationToken = default);
}