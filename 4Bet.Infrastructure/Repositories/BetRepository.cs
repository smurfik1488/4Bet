using _4Bet.Infrastructure.Data;
using _4Bet.Infrastructure.Domain;
using _4Bet.Infrastructure.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace _4Bet.Infrastructure.Repositories;

public class BetRepository(FourBetDbContext context) : IBetRepository
{
    public async Task AddAsync(Bet bet)
    {
        await context.Bets.AddAsync(bet);
    }

    public async Task<Bet?> GetByIdAsync(Guid id, Guid userId)
    {
        return await context.Bets
            .Include(b => b.Legs)
                .ThenInclude(l => l.SportEvent)
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
    }

    public async Task<IReadOnlyList<Bet>> GetByUserAsync(Guid userId)
    {
        return await context.Bets
            .Include(b => b.Legs)
                .ThenInclude(l => l.SportEvent)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Bet>> GetPendingByEventExternalIdsAsync(IEnumerable<string> eventExternalIds)
    {
        var ids = eventExternalIds.ToList();
        if (ids.Count == 0)
        {
            return Array.Empty<Bet>();
        }

        return await context.Bets
            .Include(b => b.Legs)
                .ThenInclude(l => l.SportEvent)
            .Where(b => b.Status == BetStatus.Pending && b.Legs.Any(l => ids.Contains(l.SportEvent.ExternalId)))
            .ToListAsync();
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
