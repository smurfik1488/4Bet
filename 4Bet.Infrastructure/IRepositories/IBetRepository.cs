using _4Bet.Infrastructure.Domain;

namespace _4Bet.Infrastructure.IRepositories;

public interface IBetRepository
{
    Task AddAsync(Bet bet);
    Task<Bet?> GetByIdAsync(Guid id, Guid userId);
    Task<IReadOnlyList<Bet>> GetByUserAsync(Guid userId);
    Task<IReadOnlyList<Bet>> GetPendingByEventExternalIdsAsync(IEnumerable<string> eventExternalIds);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
