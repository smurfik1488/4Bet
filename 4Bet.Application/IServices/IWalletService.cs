using _4Bet.Application.DTOs;

namespace _4Bet.Application.IServices;

public interface IWalletService
{
    Task<WalletBalanceDto?> GetBalanceAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<WalletBalanceDto?> TopUpAsync(Guid userId, decimal amount, CancellationToken cancellationToken = default);
    Task<WalletBalanceDto?> WithdrawAsync(Guid userId, decimal amount, CancellationToken cancellationToken = default);
}
