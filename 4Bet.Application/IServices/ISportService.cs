using _4Bet.Application.DTOs;

namespace _4Bet.Application.IServices;

public interface ISportService
{
    Task<IEnumerable<SportEventDto>> GetActiveEventsAsync();
}