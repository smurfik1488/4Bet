using _4Bet.Application.DTOs;

namespace _4Bet.Application.IServices;

public interface ISportService
{
    Task<IEnumerable<SportEventDto>> GetActiveEventsAsync();
    Task<SportEventDto> AddEventAsync(ManageSportEventDto dto);
    Task UpdateEventAsync(Guid id, ManageSportEventDto dto);
    Task DeleteEventAsync(Guid id);
    Task<TeamImportResultDto> ImportTeamsFromJsonAsync(Stream jsonStream, CancellationToken cancellationToken = default);
}