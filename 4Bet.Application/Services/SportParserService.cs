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
    public async Task<List<OddsApiResponse>?> GetFootballOddsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = configuration["ExternalAPIs:TheOddsApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                logger.LogError("API ключ для The Odds API не знайдено в appsettings.json.");
                return null;
            }

            var url = $"v4/sports/soccer_uefa_champs_league/odds/?apiKey={apiKey}&regions=eu&markets=h2h";
            
            return await httpClient.GetFromJsonAsync<List<OddsApiResponse>>(url, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Мережева помилка при запиті до The Odds API.");
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Непередбачена помилка при парсингу даних.");
            return null;
        }
    }
}