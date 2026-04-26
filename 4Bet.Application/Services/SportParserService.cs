// 4Bet.Infrastructure/ExternalServices/SportParserService.cs
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using _4Bet.Application.DTOs.External;
using _4Bet.Application.IServices;

namespace _4Bet.Infrastructure.ExternalServices;

public class SportParserService(
    HttpClient httpClient, 
    IConfiguration configuration, 
    ILogger<SportParserService> logger) : ISportParserService
{
    private string? GetApiKey()
    {
        var apiKey = configuration["ExternalAPIs:TheOddsApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            logger.LogError("API key for The Odds API not found in appsettings.json.");
            return null;
        }

        return apiKey;
    }

    public async Task<List<OddsApiResponse>?> GetFootballOddsAsync(string sportKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = GetApiKey();
            if (string.IsNullOrEmpty(apiKey))
            {
                return null;
            }

            // The URL now dynamically accepts the league key
            var url = $"v4/sports/{sportKey}/odds/?apiKey={apiKey}&regions=eu&markets=h2h";
            
            return await httpClient.GetFromJsonAsync<List<OddsApiResponse>>(url, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Network error when requesting The Odds API for sport: {SportKey}", sportKey);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error parsing data for sport: {SportKey}", sportKey);
            return null;
        }
    }

    public async Task<List<OddsScoreResponse>?> GetFootballScoresAsync(string sportKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = GetApiKey();
            if (string.IsNullOrEmpty(apiKey))
            {
                return null;
            }

            // daysFrom=1 includes games from yesterday/today for a lightweight live-ish score fallback.
            var url = $"v4/sports/{sportKey}/scores/?apiKey={apiKey}&daysFrom=1";
            return await httpClient.GetFromJsonAsync<List<OddsScoreResponse>>(url, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Network error when requesting score feed from The Odds API for sport: {SportKey}", sportKey);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unexpected error parsing score feed for sport: {SportKey}", sportKey);
            return null;
        }
    }
}