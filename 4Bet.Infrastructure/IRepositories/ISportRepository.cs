using _4Bet.Infrastructure.Domain;

namespace _4Bet.Infrastructure.IRepositories;

public interface ISportRepository
{
    Task UpsertEventsAsync(IEnumerable<SportEvent> events);
    Task<IEnumerable<SportEvent>> GetActiveEventsAsync();
    
    // ДОДАНО: Метод для отримання всіх матчів
    Task<IEnumerable<SportEvent>> GetAllEventsAsync(); 
    Task<SportEvent?> GetByExternalIdAsync(string externalId);
    Task<IEnumerable<SportEvent>> GetByExternalIdsAsync(IEnumerable<string> externalIds);
    
    Task<SportEvent?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<string>> GetFinishedEventExternalIdsAsync();
    Task UpsertTeamIdentitiesAsync(IEnumerable<TeamIdentity> teamIdentities);
    Task<IReadOnlyList<TeamIdentity>> GetTeamIdentitiesByNormalizedNamesAsync(IEnumerable<string> normalizedNames);
    Task AddAsync(SportEvent sportEvent);
    Task UpdateAsync(SportEvent sportEvent);
    Task DeleteAsync(SportEvent sportEvent);
}