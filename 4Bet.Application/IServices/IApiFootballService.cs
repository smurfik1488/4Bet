using _4Bet.Application.DTOs;

namespace _4Bet.Application.IServices;

public interface IApiFootballService
{
    Task<List<ApiFootballFixtureItem>?> GetLiveFixturesAsync(CancellationToken cancellationToken = default);
    Task<List<ApiFootballFixtureItem>?> GetUpcomingFixturesAsync(int count = 30, CancellationToken cancellationToken = default);
    Task<ApiFootballTeamInfo?> SearchTeamByNameAsync(string teamName, string? expectedCountry = null, CancellationToken cancellationToken = default);
}