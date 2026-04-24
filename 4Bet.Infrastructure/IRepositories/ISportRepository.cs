using _4Bet.Infrastructure.Domain;

namespace _4Bet.Infrastructure.IRepositories;

public interface ISportRepository
{
    Task UpsertEventsAsync(IEnumerable<SportEvent> events);
    Task<IEnumerable<SportEvent>> GetActiveEventsAsync();
    Task<SportEvent?> GetByIdAsync(Guid id); // Assuming BaseEntity uses Guid
    Task AddAsync(SportEvent sportEvent);
    Task UpdateAsync(SportEvent sportEvent);
    Task DeleteAsync(SportEvent sportEvent);
}