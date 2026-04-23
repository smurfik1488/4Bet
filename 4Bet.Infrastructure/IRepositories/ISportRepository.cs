using _4Bet.Infrastructure.Domain;

namespace _4Bet.Infrastructure.IRepositories;

public interface ISportRepository
{
    Task UpsertEventsAsync(IEnumerable<SportEvent> events);
    Task<IEnumerable<SportEvent>> GetActiveEventsAsync();
}