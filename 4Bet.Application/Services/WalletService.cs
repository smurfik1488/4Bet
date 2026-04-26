using _4Bet.Application.DTOs;
using _4Bet.Application.IServices;
using _4Bet.Infrastructure.Data;
using _4Bet.Infrastructure.Domain;
using _4Bet.Infrastructure.IRepositories;

namespace _4Bet.Application.Services;

public class WalletService(IWalletRepository walletRepository, FourBetDbContext dbContext) : IWalletService
{
    public async Task<WalletBalanceDto?> GetBalanceAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var wallet = await walletRepository.GetByUserIdAsync(userId);
        return wallet is null ? null : new WalletBalanceDto { Balance = wallet.Balance };
    }

    public async Task<WalletBalanceDto?> TopUpAsync(Guid userId, decimal amount, CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Top-up amount must be greater than zero.");
        }

        var wallet = await walletRepository.GetByUserIdAsync(userId);
        if (wallet is null)
        {
            return null;
        }

        wallet.Balance += amount;
        wallet.LastUpdated = DateTime.UtcNow;
        wallet.UpdatedAt = DateTime.UtcNow;

        dbContext.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            WalletId = wallet.Id,
            Type = TransactionType.Deposit,
            Amount = amount,
            Description = "Balance top-up (stub)",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await walletRepository.SaveChangesAsync(cancellationToken);
        return new WalletBalanceDto { Balance = wallet.Balance };
    }

    public async Task<WalletBalanceDto?> WithdrawAsync(Guid userId, decimal amount, CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Withdraw amount must be greater than zero.");
        }

        var wallet = await walletRepository.GetByUserIdAsync(userId);
        if (wallet is null)
        {
            return null;
        }

        if (wallet.Balance < amount)
        {
            throw new InvalidOperationException("Insufficient balance.");
        }

        wallet.Balance -= amount;
        wallet.LastUpdated = DateTime.UtcNow;
        wallet.UpdatedAt = DateTime.UtcNow;

        dbContext.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            WalletId = wallet.Id,
            Type = TransactionType.Withdrawal,
            Amount = -amount,
            Description = "Balance withdrawal (stub)",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await walletRepository.SaveChangesAsync(cancellationToken);
        return new WalletBalanceDto { Balance = wallet.Balance };
    }
}
