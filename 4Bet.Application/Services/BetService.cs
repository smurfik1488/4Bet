using _4Bet.Application.DTOs;
using _4Bet.Application.IServices;
using _4Bet.Infrastructure.Data;
using _4Bet.Infrastructure.Domain;
using _4Bet.Infrastructure.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace _4Bet.Application.Services;

public class BetService(
    IBetRepository betRepository,
    IWalletRepository walletRepository,
    ISportRepository sportRepository,
    ISportNotificationService notificationService,
    FourBetDbContext dbContext) : IBetService
{
    public async Task<BetDto> PlaceBetAsync(Guid userId, PlaceBetRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.Legs.Count == 0)
        {
            throw new InvalidOperationException("Bet must contain at least one leg.");
        }

        var wallet = await walletRepository.GetByUserIdAsync(userId);
        if (wallet is null)
        {
            throw new InvalidOperationException("Wallet not found.");
        }

        if (wallet.Balance < request.Stake)
        {
            throw new InvalidOperationException("Insufficient balance.");
        }

        var requestedIds = request.Legs.Select(l => l.EventExternalId).Distinct().ToList();
        var events = (await sportRepository.GetByExternalIdsAsync(requestedIds)).ToDictionary(e => e.ExternalId);
        if (events.Count != requestedIds.Count)
        {
            throw new InvalidOperationException("Some events are no longer available.");
        }

        var bet = new Bet
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Stake = request.Stake,
            Status = BetStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var leg in request.Legs)
        {
            var ev = events[leg.EventExternalId];
            var currentOdds = GetOddsBySelection(ev, leg.Selection);
            if (currentOdds <= 1.0)
            {
                throw new InvalidOperationException($"Event {leg.EventExternalId} does not have valid odds.");
            }

            if (Math.Abs(currentOdds - leg.RequestedOdds) > 0.001d)
            {
                throw new InvalidOperationException($"Odds changed for event {leg.EventExternalId}. Please refresh slip.");
            }

            bet.Legs.Add(new BetLeg
            {
                Id = Guid.NewGuid(),
                BetId = bet.Id,
                SportEventId = ev.Id,
                Selection = leg.Selection,
                LockedOdds = currentOdds,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        bet.CombinedOdds = bet.Legs.Aggregate(1.0d, (acc, leg) => acc * leg.LockedOdds);
        bet.PotentialPayout = Math.Round(request.Stake * (decimal)bet.CombinedOdds, 2);

        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        wallet.Balance -= request.Stake;
        wallet.LastUpdated = DateTime.UtcNow;
        wallet.UpdatedAt = DateTime.UtcNow;
        dbContext.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            WalletId = wallet.Id,
            Type = TransactionType.BetPlaced,
            Amount = -request.Stake,
            Description = $"Bet placed: {bet.Id}",
            ReferenceId = bet.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await betRepository.AddAsync(bet);
        await betRepository.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        var dto = MapToDto(bet);
        await notificationService.BroadcastBetAcceptedAsync(userId, dto);
        return dto;
    }

    public async Task<IReadOnlyList<BetDto>> GetMyBetsAsync(Guid userId)
    {
        var bets = await betRepository.GetByUserAsync(userId);
        return bets.Select(MapToDto).ToList();
    }

    public async Task<BetDto?> GetMyBetByIdAsync(Guid userId, Guid betId)
    {
        var bet = await betRepository.GetByIdAsync(betId, userId);
        return bet is null ? null : MapToDto(bet);
    }

    public async Task SettleByLiveEventsAsync(IEnumerable<string> eventExternalIds, CancellationToken cancellationToken = default)
    {
        var eventIds = eventExternalIds.Distinct().ToList();
        if (eventIds.Count == 0)
        {
            return;
        }

        var pending = await betRepository.GetPendingByEventExternalIdsAsync(eventIds);
        if (pending.Count == 0)
        {
            return;
        }

        var walletsCache = new Dictionary<Guid, Wallet>();
        var changedBets = new List<Bet>();
        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        foreach (var bet in pending)
        {
            if (bet.Legs.Any(l => !IsSettledStatus(l.SportEvent.MatchStatus)))
            {
                continue;
            }

            var hasRefundLeg = bet.Legs.Any(l => l.SportEvent.MatchStatus == "PST" || l.SportEvent.MatchStatus == "CANC");
            if (hasRefundLeg)
            {
                bet.Status = BetStatus.Refunded;
                bet.SettledPayout = bet.Stake;
            }
            else
            {
                var wonAll = bet.Legs.All(IsWinningLeg);
                bet.Status = wonAll ? BetStatus.Won : BetStatus.Lost;
                bet.SettledPayout = wonAll ? bet.PotentialPayout : 0m;
            }

            bet.SettledAt = DateTime.UtcNow;
            bet.UpdatedAt = DateTime.UtcNow;
            changedBets.Add(bet);

            if (!walletsCache.TryGetValue(bet.UserId, out var wallet))
            {
                wallet = await walletRepository.GetByUserIdAsync(bet.UserId)
                    ?? throw new InvalidOperationException("Wallet not found for settlement.");
                walletsCache[bet.UserId] = wallet;
            }

            if (bet.SettledPayout > 0)
            {
                wallet.Balance += bet.SettledPayout.Value;
                wallet.LastUpdated = DateTime.UtcNow;
                wallet.UpdatedAt = DateTime.UtcNow;
                dbContext.Transactions.Add(new Transaction
                {
                    Id = Guid.NewGuid(),
                    WalletId = wallet.Id,
                    Type = bet.Status == BetStatus.Refunded ? TransactionType.BetRefunded : TransactionType.BetWon,
                    Amount = bet.SettledPayout.Value,
                    Description = $"Bet settled: {bet.Id}, status: {bet.Status}",
                    ReferenceId = bet.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        if (changedBets.Count == 0)
        {
            return;
        }

        await betRepository.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        foreach (var bet in changedBets)
        {
            await notificationService.BroadcastBetSettledAsync(bet.UserId, new BetLifecycleUpdateDto
            {
                BetId = bet.Id,
                EventType = "BetSettled",
                Status = bet.Status.ToString(),
                SettledPayout = bet.SettledPayout
            });
        }
    }

    private static bool IsSettledStatus(string status)
        => status is "FT" or "AET" or "PEN" or "CANC" or "PST";

    private static bool IsWinningLeg(BetLeg leg)
    {
        var home = leg.SportEvent.HomeScore ?? 0;
        var away = leg.SportEvent.AwayScore ?? 0;
        return leg.Selection switch
        {
            BetSelection.HomeWin => home > away,
            BetSelection.Draw => home == away,
            BetSelection.AwayWin => away > home,
            _ => false
        };
    }

    private static double GetOddsBySelection(SportEvent ev, BetSelection selection) => selection switch
    {
        BetSelection.HomeWin => ev.HomeWinOdds,
        BetSelection.Draw => ev.DrawOdds,
        BetSelection.AwayWin => ev.AwayWinOdds,
        _ => 0d
    };

    private static BetDto MapToDto(Bet bet)
    {
        return new BetDto
        {
            Id = bet.Id,
            Stake = bet.Stake,
            CombinedOdds = bet.CombinedOdds,
            PotentialPayout = bet.PotentialPayout,
            Status = bet.Status,
            SettledPayout = bet.SettledPayout,
            CreatedAt = bet.CreatedAt,
            SettledAt = bet.SettledAt,
            Legs = bet.Legs.Select(l => new BetLegDto
            {
                EventExternalId = l.SportEvent.ExternalId,
                HomeTeam = l.SportEvent.HomeTeam,
                AwayTeam = l.SportEvent.AwayTeam,
                Selection = l.Selection,
                LockedOdds = l.LockedOdds
            }).ToList()
        };
    }
}
