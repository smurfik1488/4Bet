using _4Bet.Application.DTOs;
using _4Bet.Application.DTOs.External;
using _4Bet.Application.IServices;
using _4Bet.Infrastructure.Data;
using _4Bet.Infrastructure.Domain;
using Microsoft.EntityFrameworkCore;

namespace _4Bet.Application.Services;

public class DataSeedService(
    FourBetDbContext dbContext,
    ISportParserService sportParserService,
    IAuditLogService auditLogService) : IDataSeedService
{
    public async Task<SeedResultDto> SeedHybridAsync(CancellationToken cancellationToken = default)
    {
        var result = new SeedResultDto();
        var rng = new Random();

        var seedEventsAdded = await EnsureSportEventsAsync(rng, cancellationToken);
        result.SportEventsAdded += seedEventsAdded;

        var usersAdded = await EnsureUsersAsync(cancellationToken);
        result.UsersAdded += usersAdded;

        var betsResult = await EnsureBetsAsync(rng, cancellationToken);
        result.BetsAdded += betsResult.BetsAdded;
        result.BetLegsAdded += betsResult.BetLegsAdded;
        result.TransactionsAdded += betsResult.TransactionsAdded;

        await auditLogService.LogAsync(
            action: "DataSeedCompleted",
            entityType: "Seed",
            entityId: null,
            userId: null,
            summary: "Hybrid data seeding completed.",
            payload: result,
            cancellationToken: cancellationToken);

        return result;
    }

    private async Task<int> EnsureSportEventsAsync(Random rng, CancellationToken cancellationToken)
    {
        const int targetCount = 1000;
        var existingCount = await dbContext.SportEvents.CountAsync(cancellationToken);
        if (existingCount >= targetCount)
        {
            return 0;
        }

        var toAdd = new List<SportEvent>();

        var apiBatch = await sportParserService.GetFootballOddsAsync("soccer_epl", cancellationToken);
        if (apiBatch is { Count: > 0 })
        {
            foreach (var dto in apiBatch.Take(150))
            {
                var outcomes = dto.Bookmakers?
                    .SelectMany(b => b.Markets)
                    .Where(m => m.Key == "h2h")
                    .SelectMany(m => m.Outcomes)
                    .ToList() ?? new List<Outcome>();

                var homeTeam = dto.HomeTeam ?? "Home";
                var awayTeam = dto.AwayTeam ?? "Away";
                var homeOdds = outcomes.FirstOrDefault(o => o.Name == homeTeam)?.Price ?? 1.8;
                var drawOdds = outcomes.FirstOrDefault(o => o.Name?.Equals("Draw", StringComparison.OrdinalIgnoreCase) == true)?.Price ?? 3.2;
                var awayOdds = outcomes.FirstOrDefault(o => o.Name == awayTeam)?.Price ?? 2.1;

                toAdd.Add(new SportEvent
                {
                    Id = Guid.NewGuid(),
                    ExternalId = $"seed-api-{dto.Id}",
                    HomeTeam = homeTeam,
                    AwayTeam = awayTeam,
                    EventDate = dto.CommenceTime.ToUniversalTime(),
                    SportKey = dto.SportKey ?? "soccer_epl",
                    HomeWinOdds = homeOdds,
                    DrawOdds = drawOdds,
                    AwayWinOdds = awayOdds,
                    MatchStatus = "NS",
                    LastUpdated = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        var missing = targetCount - existingCount - toAdd.Count;
        for (var i = 0; i < missing; i++)
        {
            var home = $"SeedHome{i + 1}";
            var away = $"SeedAway{i + 1}";
            toAdd.Add(new SportEvent
            {
                Id = Guid.NewGuid(),
                ExternalId = $"seed-generated-{i + 1}",
                HomeTeam = home,
                AwayTeam = away,
                EventDate = DateTime.UtcNow.AddDays(rng.Next(-30, 30)).AddMinutes(rng.Next(0, 1440)),
                SportKey = i % 2 == 0 ? "soccer_epl" : "soccer_uefa_champs_league",
                HomeWinOdds = Math.Round(1.3 + rng.NextDouble() * 3.5, 2),
                DrawOdds = Math.Round(2.0 + rng.NextDouble() * 3.5, 2),
                AwayWinOdds = Math.Round(1.3 + rng.NextDouble() * 3.5, 2),
                HomeScore = rng.Next(0, 5),
                AwayScore = rng.Next(0, 5),
                MatchStatus = rng.Next(0, 4) switch
                {
                    0 => "NS",
                    1 => "1H",
                    2 => "2H",
                    _ => "FT"
                },
                MatchMinute = rng.Next(0, 91),
                LastUpdated = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        var existingExternalIds = await dbContext.SportEvents
            .Select(e => e.ExternalId)
            .ToHashSetAsync(cancellationToken);

        var filtered = toAdd
            .Where(e => !existingExternalIds.Contains(e.ExternalId))
            .ToList();

        if (filtered.Count == 0)
        {
            return 0;
        }

        await dbContext.SportEvents.AddRangeAsync(filtered, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return filtered.Count;
    }

    private async Task<int> EnsureUsersAsync(CancellationToken cancellationToken)
    {
        const int targetUsers = 100;
        var existingUsers = await dbContext.Users.CountAsync(cancellationToken);
        if (existingUsers >= targetUsers)
        {
            return 0;
        }

        var missing = targetUsers - existingUsers;
        var usersToAdd = new List<User>(missing);
        var walletsToAdd = new List<Wallet>(missing);

        var startIndex = existingUsers + 1;
        for (var i = 0; i < missing; i++)
        {
            var id = Guid.NewGuid();
            usersToAdd.Add(new User
            {
                Id = id,
                Email = $"seed_user_{startIndex + i}@example.com",
                PasswordHash = "SEED_HASH",
                FirstName = "Seed",
                LastName = $"User{startIndex + i}",
                Birthday = DateTime.UtcNow.AddYears(-20).Date,
                IsBdVerified = true,
                IsEmailVerified = true,
                Role = UserRole.User,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            });

            walletsToAdd.Add(new Wallet
            {
                Id = Guid.NewGuid(),
                UserId = id,
                Balance = 1000m,
                LastUpdated = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            });
        }

        await dbContext.Users.AddRangeAsync(usersToAdd, cancellationToken);
        await dbContext.Wallets.AddRangeAsync(walletsToAdd, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return usersToAdd.Count;
    }

    private async Task<(int BetsAdded, int BetLegsAdded, int TransactionsAdded)> EnsureBetsAsync(Random rng, CancellationToken cancellationToken)
    {
        const int targetBets = 1500;
        var existingBets = await dbContext.Bets.CountAsync(cancellationToken);
        if (existingBets >= targetBets)
        {
            return (0, 0, 0);
        }

        var users = await dbContext.Users
            .Where(u => !u.IsDeleted && u.Role == UserRole.User)
            .Take(120)
            .ToListAsync(cancellationToken);
        var wallets = await dbContext.Wallets
            .Where(w => users.Select(u => u.Id).Contains(w.UserId))
            .ToDictionaryAsync(w => w.UserId, cancellationToken);
        var events = await dbContext.SportEvents
            .OrderBy(e => e.EventDate)
            .Take(1200)
            .ToListAsync(cancellationToken);

        if (users.Count == 0 || events.Count < 3)
        {
            return (0, 0, 0);
        }

        var missing = targetBets - existingBets;
        var bets = new List<Bet>(missing);
        var legs = new List<BetLeg>(missing * 2);
        var transactions = new List<Transaction>(missing * 2);

        for (var i = 0; i < missing; i++)
        {
            var user = users[rng.Next(users.Count)];
            if (!wallets.TryGetValue(user.Id, out var wallet))
            {
                continue;
            }

            var stake = Math.Round((decimal)(rng.NextDouble() * 45 + 5), 2);
            if (wallet.Balance < stake)
            {
                wallet.Balance += 200m;
            }

            var selectedEvents = events.OrderBy(_ => rng.Next()).Take(2).ToList();
            var leg1Odds = Math.Round(1.2 + rng.NextDouble() * 3, 2);
            var leg2Odds = Math.Round(1.2 + rng.NextDouble() * 3, 2);
            var combined = Math.Round(leg1Odds * leg2Odds, 4);
            var potentialPayout = Math.Round(stake * (decimal)combined, 2);
            var status = rng.Next(0, 3) switch
            {
                0 => BetStatus.Won,
                1 => BetStatus.Lost,
                _ => BetStatus.Pending
            };
            var settledPayout = status == BetStatus.Won ? potentialPayout : 0m;

            var betId = Guid.NewGuid();
            var createdAt = DateTime.UtcNow.AddDays(-rng.Next(0, 120)).AddMinutes(rng.Next(0, 1440));

            var bet = new Bet
            {
                Id = betId,
                UserId = user.Id,
                Stake = stake,
                CombinedOdds = (double)combined,
                PotentialPayout = potentialPayout,
                Status = status,
                SettledPayout = status == BetStatus.Pending ? null : settledPayout,
                SettledAt = status == BetStatus.Pending ? null : createdAt.AddHours(rng.Next(2, 36)),
                CreatedAt = createdAt,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
            bets.Add(bet);

            legs.Add(new BetLeg
            {
                Id = Guid.NewGuid(),
                BetId = betId,
                SportEventId = selectedEvents[0].Id,
                Selection = (BetSelection)rng.Next(0, 3),
                LockedOdds = leg1Odds,
                CreatedAt = createdAt,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            });
            legs.Add(new BetLeg
            {
                Id = Guid.NewGuid(),
                BetId = betId,
                SportEventId = selectedEvents[1].Id,
                Selection = (BetSelection)rng.Next(0, 3),
                LockedOdds = leg2Odds,
                CreatedAt = createdAt,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            });

            wallet.Balance -= stake;
            transactions.Add(new Transaction
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                Type = TransactionType.BetPlaced,
                Amount = -stake,
                Description = $"Seed bet placed: {betId}",
                ReferenceId = betId,
                CreatedAt = createdAt,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            });

            if (status == BetStatus.Won)
            {
                wallet.Balance += settledPayout;
                transactions.Add(new Transaction
                {
                    Id = Guid.NewGuid(),
                    WalletId = wallet.Id,
                    Type = TransactionType.BetWon,
                    Amount = settledPayout,
                    Description = $"Seed bet won: {betId}",
                    ReferenceId = betId,
                    CreatedAt = bet.SettledAt ?? createdAt.AddHours(3),
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                });
            }

            wallet.LastUpdated = DateTime.UtcNow;
            wallet.UpdatedAt = DateTime.UtcNow;
        }

        await dbContext.Bets.AddRangeAsync(bets, cancellationToken);
        await dbContext.BetLegs.AddRangeAsync(legs, cancellationToken);
        await dbContext.Transactions.AddRangeAsync(transactions, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (bets.Count, legs.Count, transactions.Count);
    }
}
