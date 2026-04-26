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
                // Оновлюємо старі поля (коефіцієнти)
                existing.HomeWinOdds = ev.HomeWinOdds;
                existing.AwayWinOdds = ev.AwayWinOdds;
                existing.DrawOdds = ev.DrawOdds;
                
                // ДОДАНО: Оновлюємо нові поля (лайв-рахунок)
                existing.HomeScore = ev.HomeScore;
                existing.AwayScore = ev.AwayScore;
                existing.MatchStatus = ev.MatchStatus;
                existing.MatchMinute = ev.MatchMinute;
                
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
    {
        var now = DateTime.UtcNow;
        var recentLiveFallbackStart = now.AddHours(-4);
        var upcomingWindowEnd = now.AddDays(7);
        var liveStatuses = new[]
        {
            "1H", "HT", "2H", "ET", "BT", "P", "LIVE", "INT", "INPLAY", "IN_PLAY", "PLAYING"
        };

        return await context.Set<SportEvent>()
            .Where(e =>
                (e.EventDate >= recentLiveFallbackStart && e.EventDate <= upcomingWindowEnd &&
                 e.MatchStatus != null &&
                 e.MatchStatus != "FT" &&
                 e.MatchStatus != "AET" &&
                 e.MatchStatus != "PEN" &&
                 e.MatchStatus != "CANC" &&
                 e.MatchStatus != "PST") ||
                (e.EventDate >= now && e.EventDate <= upcomingWindowEnd) ||
                (e.MatchStatus != null && (
                    liveStatuses.Contains(e.MatchStatus.Trim().ToUpper()) ||
                    EF.Functions.Like(e.MatchStatus.ToUpper(), "%LIVE%") ||
                    EF.Functions.Like(e.MatchStatus.ToUpper(), "%PLAY%"))))
            .OrderBy(e => e.EventDate)
            .ToListAsync();
    }
    
    // ДОДАНО: Реалізація методу для отримання всіх подій
    public async Task<IEnumerable<SportEvent>> GetAllEventsAsync()
    {
        return await context.SportEvents.ToListAsync();
    }

    public async Task<SportEvent?> GetByExternalIdAsync(string externalId)
    {
        return await context.SportEvents.FirstOrDefaultAsync(e => e.ExternalId == externalId);
    }

    public async Task<IEnumerable<SportEvent>> GetByExternalIdsAsync(IEnumerable<string> externalIds)
    {
        var ids = externalIds.ToList();
        if (ids.Count == 0)
        {
            return Array.Empty<SportEvent>();
        }

        return await context.SportEvents.Where(e => ids.Contains(e.ExternalId)).ToListAsync();
    }

    public async Task<SportEvent?> GetByIdAsync(Guid id)
    {
        return await context.SportEvents.FindAsync(id);
    }

    public async Task<IReadOnlyList<string>> GetFinishedEventExternalIdsAsync()
    {
        return await context.SportEvents
            .Where(e => e.MatchStatus == "FT" || e.MatchStatus == "AET" || e.MatchStatus == "PEN" || e.MatchStatus == "CANC")
            .Select(e => e.ExternalId)
            .ToListAsync();
    }

    public async Task UpsertTeamIdentitiesAsync(IEnumerable<TeamIdentity> teamIdentities)
    {
        foreach (var incoming in teamIdentities)
        {
            var existing = await context.TeamIdentities.FirstOrDefaultAsync(x =>
                x.Provider == incoming.Provider && x.ProviderTeamId == incoming.ProviderTeamId);

            if (existing == null)
            {
                await context.TeamIdentities.AddAsync(incoming);
                continue;
            }

            existing.TeamName = incoming.TeamName;
            existing.TeamNameNormalized = incoming.TeamNameNormalized;
            existing.LogoUrl = incoming.LogoUrl;
            existing.LastSeenAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<TeamIdentity>> GetTeamIdentitiesByNormalizedNamesAsync(IEnumerable<string> normalizedNames)
    {
        var names = normalizedNames
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();
        if (names.Count == 0)
        {
            return Array.Empty<TeamIdentity>();
        }

        return await context.TeamIdentities
            .Where(x => names.Contains(x.TeamNameNormalized))
            .ToListAsync();
    }

    public async Task AddAsync(SportEvent sportEvent)
    {
        await context.SportEvents.AddAsync(sportEvent);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(SportEvent sportEvent)
    {
        context.SportEvents.Update(sportEvent);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(SportEvent sportEvent)
    {
        context.SportEvents.Remove(sportEvent);
        await context.SaveChangesAsync();
    }
}