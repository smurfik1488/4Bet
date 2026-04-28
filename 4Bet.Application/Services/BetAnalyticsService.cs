using _4Bet.Application.DTOs;
using _4Bet.Application.IServices;
using _4Bet.Infrastructure.Data;
using _4Bet.Infrastructure.Domain;
using Microsoft.EntityFrameworkCore;

namespace _4Bet.Application.Services;

public class BetAnalyticsService(FourBetDbContext dbContext) : IBetAnalyticsService
{
    public async Task<BetAnalyticsDto> GetUserAnalyticsAsync(Guid userId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        var normalizedFrom = DateTime.SpecifyKind(fromUtc, DateTimeKind.Utc);
        var normalizedTo = DateTime.SpecifyKind(toUtc, DateTimeKind.Utc);
        if (normalizedFrom > normalizedTo)
        {
            (normalizedFrom, normalizedTo) = (normalizedTo, normalizedFrom);
        }

        normalizedTo = normalizedTo.Date.AddDays(1).AddTicks(-1);

        var query = dbContext.Bets
            .AsNoTracking()
            .Where(b => b.UserId == userId && b.CreatedAt >= normalizedFrom && b.CreatedAt <= normalizedTo);

        var rows = await query
            .Select(b => new
            {
                Day = b.CreatedAt.Date,
                Stake = b.Stake,
                Payout = b.SettledPayout ?? 0m,
                Status = b.Status
            })
            .ToListAsync(cancellationToken);

        var grouped = rows
            .GroupBy(x => x.Day)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    BetsCount = g.Count(),
                    WonCount = g.Count(x => x.Status == BetStatus.Won),
                    LostCount = g.Count(x => x.Status == BetStatus.Lost),
                    StakeSum = g.Sum(x => x.Stake),
                    PayoutSum = g.Sum(x => x.Payout)
                });

        var points = new List<BetAnalyticsPointDto>();
        var dayCursor = normalizedFrom.Date;
        var dayEnd = normalizedTo.Date;
        while (dayCursor <= dayEnd)
        {
            if (grouped.TryGetValue(dayCursor, out var dayData))
            {
                points.Add(new BetAnalyticsPointDto
                {
                    DayUtc = DateTime.SpecifyKind(dayCursor, DateTimeKind.Utc),
                    BetsCount = dayData.BetsCount,
                    WonCount = dayData.WonCount,
                    LostCount = dayData.LostCount,
                    StakeSum = dayData.StakeSum,
                    PayoutSum = dayData.PayoutSum,
                    Net = dayData.PayoutSum - dayData.StakeSum
                });
            }
            else
            {
                points.Add(new BetAnalyticsPointDto
                {
                    DayUtc = DateTime.SpecifyKind(dayCursor, DateTimeKind.Utc),
                    BetsCount = 0,
                    WonCount = 0,
                    LostCount = 0,
                    StakeSum = 0,
                    PayoutSum = 0,
                    Net = 0
                });
            }

            dayCursor = dayCursor.AddDays(1);
        }

        var totalBets = rows.Count;
        var totalStake = rows.Sum(x => x.Stake);
        var totalPayout = rows.Sum(x => x.Payout);
        var wonBets = rows.Count(x => x.Status == BetStatus.Won);

        return new BetAnalyticsDto
        {
            FromUtc = normalizedFrom,
            ToUtc = normalizedTo,
            TotalBets = totalBets,
            TotalStake = totalStake,
            TotalPayout = totalPayout,
            Net = totalPayout - totalStake,
            WinRatePercent = totalBets == 0 ? 0 : Math.Round((double)wonBets / totalBets * 100, 1),
            Points = points
        };
    }
}
