using _4Bet.Infrastructure.Data;
using _4Bet.Infrastructure.Domain;
using _4Bet.Infrastructure.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace _4Bet.Infrastructure.Repositories;

public class WalletRepository(FourBetDbContext context) : IWalletRepository
{
    public async Task<Wallet?> GetByUserIdAsync(Guid userId)
    {
        return await context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == userId);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
