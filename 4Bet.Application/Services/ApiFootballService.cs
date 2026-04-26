using System.Net.Http.Json;
using _4Bet.Application.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using _4Bet.Application.DTOs.External;
using _4Bet.Application.IServices;

namespace _4Bet.Infrastructure.ExternalServices;

public class ApiFootballService(
    HttpClient httpClient, 
    IConfiguration configuration, 
    ILogger<ApiFootballService> logger) : IApiFootballService
{
    private string? _apiKey;

    private bool EnsureAuthHeader()
    {
        _apiKey ??= configuration["ExternalAPIs:ApiFootballKey"];
        if (string.IsNullOrEmpty(_apiKey))
        {
            logger.LogError("API key for API-Football not found in appsettings.json.");
            return false;
        }

        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("x-apisports-key", _apiKey);
        return true;
    }

    public async Task<List<ApiFootballFixtureItem>?> GetLiveFixturesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!EnsureAuthHeader())
            {
                return null;
            }

            // Звертаємося до ендпоінту, який повертає тільки лайв-матчі
            var response = await httpClient.GetFromJsonAsync<ApiFootballLiveResponse>("fixtures?live=all", cancellationToken);
            
            return response?.Response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching live matches from API-Football");
            return null;
        }
    }

    public async Task<List<ApiFootballFixtureItem>?> GetUpcomingFixturesAsync(
        int count = 30,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!EnsureAuthHeader())
            {
                return null;
            }

            var safeCount = Math.Clamp(count, 1, 100);
            var response = await httpClient.GetFromJsonAsync<ApiFootballLiveResponse>(
                $"fixtures?next={safeCount}",
                cancellationToken);

            return response?.Response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching upcoming matches from API-Football");
            return null;
        }
    }

    public async Task<ApiFootballTeamInfo?> SearchTeamByNameAsync(
        string teamName,
        string? expectedCountry = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teamName))
        {
            return null;
        }

        try
        {
            if (!EnsureAuthHeader())
            {
                return null;
            }

            var encodedName = Uri.EscapeDataString(teamName.Trim());
            var response = await httpClient.GetFromJsonAsync<ApiFootballTeamSearchResponse>(
                $"teams?search={encodedName}",
                cancellationToken);
            var candidates = response?.Response?.Select(x => x.Team).ToList();
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            var normalizedQuery = NormalizeTeamName(teamName);
            var exactNameMatch = candidates
                .Where(x => NormalizeTeamName(x.Name) == normalizedQuery)
                .ToList();

            var countryFiltered = !string.IsNullOrWhiteSpace(expectedCountry)
                ? exactNameMatch
                    .Where(x => string.Equals(x.Country, expectedCountry, StringComparison.OrdinalIgnoreCase))
                    .ToList()
                : new List<ApiFootballTeamInfo>();

            if (countryFiltered.Count > 0)
            {
                return countryFiltered.First();
            }

            if (exactNameMatch.Count > 0)
            {
                return exactNameMatch.First();
            }

            return candidates.FirstOrDefault();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error searching team logo for {TeamName} via API-Football", teamName);
            return null;
        }
    }

    private static string NormalizeTeamName(string value)
    {
        var normalized = value.ToLowerInvariant().Trim();
        normalized = normalized.Replace("football club", " ");
        normalized = normalized.Replace("fc", " ");
        normalized = normalized.Replace("cf", " ");
        normalized = normalized.Replace("utd", "united");
        normalized = new string(normalized.Where(char.IsLetterOrDigit).ToArray());
        return normalized;
    }
}