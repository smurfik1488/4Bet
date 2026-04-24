using AutoMapper;
using _4Bet.Application.DTOs;
using _4Bet.Application.IServices;
using _4Bet.Infrastructure.IRepositories;

namespace _4Bet.Application.Services;

public class SportService(ISportRepository sportRepository, IMapper mapper) : ISportService
{
    public async Task<IEnumerable<SportEventDto>> GetActiveEventsAsync()
    {
        // GetActiveEventsAsync is already implemented in your SportRepository!
        var events = await sportRepository.GetActiveEventsAsync();
        
        // Map the database entities to DTOs for the frontend
        return mapper.Map<IEnumerable<SportEventDto>>(events);
    }
}