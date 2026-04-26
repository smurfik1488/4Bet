using _4Bet.Application.IServices;
using _4Bet.Infrastructure.IRepositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace _4Bet.Application.Services;

public class BetSettlementWorker(
    IServiceProvider serviceProvider,
    ILogger<BetSettlementWorker> logger) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(3);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Bet Settlement Worker started.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var sportRepo = scope.ServiceProvider.GetRequiredService<ISportRepository>();
                var betService = scope.ServiceProvider.GetRequiredService<IBetService>();
                var finishedEventIds = await sportRepo.GetFinishedEventExternalIdsAsync();
                await betService.SettleByLiveEventsAsync(finishedEventIds, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in BetSettlementWorker");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
