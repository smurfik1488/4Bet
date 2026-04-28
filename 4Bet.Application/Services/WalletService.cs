using _4Bet.Application.DTOs;
using _4Bet.Application.IServices;
using _4Bet.Infrastructure.Data;
using _4Bet.Infrastructure.Domain;
using _4Bet.Infrastructure.IRepositories;

namespace _4Bet.Application.Services;

public class WalletService(
    IWalletRepository walletRepository,
    IAuthRepository authRepository,
    IBusinessRulesService businessRules,
    IAuditLogService auditLogService,
    FourBetDbContext dbContext) : IWalletService
{
    public async Task<WalletBalanceDto?> GetBalanceAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var wallet = await walletRepository.GetByUserIdAsync(userId);
        return wallet is null ? null : new WalletBalanceDto { Balance = wallet.Balance };
    }

    public async Task<WalletBalanceDto?> TopUpAsync(Guid userId, decimal amount, CancellationToken cancellationToken = default)
    {
        var user = await authRepository.GetByIdAsync(userId);
        if (user == null || user.IsDeleted)
        {
            return null;
        }

        businessRules.EnsureVerified(user, "depositing funds");
        businessRules.EnsurePositiveAmount(amount, "Top-up");

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
        await auditLogService.LogAsync(
            action: "WalletTopUp",
            entityType: "Wallet",
            entityId: wallet.Id,
            userId: userId,
            summary: "Wallet topped up.",
            payload: new { amount, wallet.Balance },
            cancellationToken: cancellationToken);
        return new WalletBalanceDto { Balance = wallet.Balance };
    }

    public async Task<WalletBalanceDto?> WithdrawAsync(Guid userId, decimal amount, CancellationToken cancellationToken = default)
    {
        var user = await authRepository.GetByIdAsync(userId);
        if (user == null || user.IsDeleted)
        {
            return null;
        }

        businessRules.EnsureVerified(user, "withdrawing funds");
        businessRules.EnsurePositiveAmount(amount, "Withdraw");

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
        await auditLogService.LogAsync(
            action: "WalletWithdraw",
            entityType: "Wallet",
            entityId: wallet.Id,
            userId: userId,
            summary: "Wallet withdrawal completed.",
            payload: new { amount, wallet.Balance },
            cancellationToken: cancellationToken);
        return new WalletBalanceDto { Balance = wallet.Balance };
    }
}
