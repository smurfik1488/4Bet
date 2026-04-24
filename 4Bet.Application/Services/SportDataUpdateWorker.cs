// 4Bet.Application/Services/SportDataUpdateWorker.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using _4Bet.Application.IServices;
using _4Bet.Application.DTOs.External;
using _4Bet.Infrastructure.Domain;
using _4Bet.Infrastructure.IRepositories;

namespace _4Bet.Application.Services;

public class SportDataUpdateWorker(
    IServiceProvider serviceProvider,
    ILogger<SportDataUpdateWorker> logger) : BackgroundService
{
    // Increased to 6 hours to stay within the 500 requests/month free limit (4 leagues * 4 times a day * 30 days = 480 requests)
    private readonly TimeSpan _updateInterval = TimeSpan.FromHours(6);

    // List of leagues to parse
    private readonly string[] _targetLeagues = 
    {
        "soccer_epl",                  // English Premier League
        "soccer_spain_la_liga",        // Spanish La Liga
        "soccer_uefa_champs_league",   // Champions League
        "soccer_uefa_europa_conference_league" // Ukrainian Premier League
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Sports Data Update Worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var parser = scope.ServiceProvider.GetRequiredService<ISportParserService>();
                var repo = scope.ServiceProvider.GetRequiredService<ISportRepository>();

                logger.LogInformation("Starting synchronization with The Odds API...");

                foreach (var league in _targetLeagues)
                {
                    logger.LogInformation("Fetching odds for: {League}", league);
                    
                    var apiData = await parser.GetFootballOddsAsync(league, stoppingToken);
                    
                    if (apiData != null && apiData.Any())
                    {
                        var validEvents = apiData
                            .Where(dto => dto.CommenceTime > DateTime.UtcNow)
                            .Select(MapToDomain)
                            .Where(ev => ev != null)
                            .Cast<SportEvent>()
                            .ToList();

                        if (validEvents.Any())
                        {
                            await repo.UpsertEventsAsync(validEvents);
                            logger.LogInformation("Successfully updated {Count} events for {League}.", validEvents.Count, league);
                        }
                    }
                    else
                    {
                        logger.LogWarning("API returned empty result for {League}. It might be out of season or the key is incorrect.", league);
                    }
                    
                    // Add a small delay between requests to avoid hitting rate limit spikes
                    await Task.Delay(2000, stoppingToken); 
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Critical error inside the worker loop.");
            }

            logger.LogInformation("Worker sleeping for {Hours} hours...", _updateInterval.TotalHours);
            await Task.Delay(_updateInterval, stoppingToken);
        }
    }

    private SportEvent? MapToDomain(OddsApiResponse dto)
    {
        try
        {
            var bookmaker = dto.Bookmakers?.FirstOrDefault();
            var market = bookmaker?.Markets?.FirstOrDefault(m => m.Key == "h2h");
            
            if (market == null) return null;

            return new SportEvent
            {
                ExternalId = dto.Id,
                HomeTeam = dto.HomeTeam,
                AwayTeam = dto.AwayTeam,
                EventDate = dto.CommenceTime,
                SportKey = dto.SportKey,
                HomeWinOdds = market.Outcomes.FirstOrDefault(o => o.Name == dto.HomeTeam)?.Price ?? 1.0,
                AwayWinOdds = market.Outcomes.FirstOrDefault(o => o.Name == dto.AwayTeam)?.Price ?? 1.0,
                DrawOdds = market.Outcomes.FirstOrDefault(o => o.Name == "Draw")?.Price ?? 1.0,
                LastUpdated = DateTime.UtcNow
            };
        }
        catch
        {
            return null;
        }
    }
}