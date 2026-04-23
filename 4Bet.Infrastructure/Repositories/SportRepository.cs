using _4Bet.Infrastructure.Data;
using _4Bet.Infrastructure.Domain;
using _4Bet.Infrastructure.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace _4Bet.Infrastructure.Repositories;

public class SportRepository(FourBetDbContext context) : ISportRepository
{
    public async Task UpsertEventsAsync(IEnumerable<SportEvent> events)
    {
        foreach (var ev in events)
        {
            var existing = await context.Set<SportEvent>()
                .FirstOrDefaultAsync(e => e.ExternalId == ev.ExternalId);

            if (existing != null)
            {
                existing.HomeWinOdds = ev.HomeWinOdds;
                existing.AwayWinOdds = ev.AwayWinOdds;
                existing.DrawOdds = ev.DrawOdds;
                existing.LastUpdated = DateTime.UtcNow;
            }
            else
            {
                await context.Set<SportEvent>().AddAsync(ev);
            }
        }
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<SportEvent>> GetActiveEventsAsync() 
        => await context.Set<SportEvent>().Where(e => e.EventDate > DateTime.UtcNow).ToListAsync();
}