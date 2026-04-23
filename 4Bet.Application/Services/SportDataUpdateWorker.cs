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
    private readonly TimeSpan _updateInterval = TimeSpan.FromMinutes(90);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Воркер оновлення спортивних даних запущено.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Оскільки BackgroundService є Singleton, ми створюємо Scope, 
                // щоб дістати Scoped та Transient сервіси (репозиторій та парсер)
                using var scope = serviceProvider.CreateScope();
                
                // Використовуємо ІНТЕРФЕЙСИ
                var parser = scope.ServiceProvider.GetRequiredService<ISportParserService>();
                var repo = scope.ServiceProvider.GetRequiredService<ISportRepository>();

                logger.LogInformation("Початок синхронізації з The Odds API...");
                
                var apiData = await parser.GetFootballOddsAsync(stoppingToken);
                
                if (apiData != null && apiData.Any())
                {
                    var validEvents = apiData
                        .Where(dto => dto.CommenceTime > DateTime.UtcNow) // Тільки майбутні події
                        .Select(MapToDomain)
                        .Where(ev => ev != null)
                        .Cast<SportEvent>()
                        .ToList();

                    if (validEvents.Any())
                    {
                        await repo.UpsertEventsAsync(validEvents);
                        logger.LogInformation("Успішно оновлено {Count} спортивних подій.", validEvents.Count);
                    }
                }
                else
                {
                    logger.LogWarning("API повернуло порожній результат або сталася помилка.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Критична помилка всередині циклу воркера.");
            }

            // Засинаємо до наступного циклу
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
            return null; // Відкидаємо биті дані
        }
    }
}