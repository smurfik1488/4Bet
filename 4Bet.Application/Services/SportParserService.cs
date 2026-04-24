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
    public async Task<List<OddsApiResponse>?> GetFootballOddsAsync(string sportKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = configuration["ExternalAPIs:TheOddsApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                logger.LogError("API key for The Odds API not found in appsettings.json.");
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
}