using AutoMapper;
using _4Bet.Application.DTOs;
using _4Bet.Application.IServices;
using _4Bet.Infrastructure.Domain;
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
    
    public async Task<SportEventDto> AddEventAsync(ManageSportEventDto dto)
    {
        var newEvent = new SportEvent
        {
            ExternalId = dto.ExternalId,
            HomeTeam = dto.HomeTeam,
            AwayTeam = dto.AwayTeam,
            EventDate = dto.EventDate,
            SportKey = dto.SportKey,
            HomeWinOdds = dto.HomeWinOdds,
            DrawOdds = dto.DrawOdds,
            AwayWinOdds = dto.AwayWinOdds,
            LastUpdated = DateTime.UtcNow
        };

        await sportRepository.AddAsync(newEvent);
        return mapper.Map<SportEventDto>(newEvent);
    }

    public async Task UpdateEventAsync(Guid id, ManageSportEventDto dto)
    {
        var existingEvent = await sportRepository.GetByIdAsync(id) 
                            ?? throw new KeyNotFoundException("Event not found.");

        existingEvent.ExternalId = dto.ExternalId;
        existingEvent.HomeTeam = dto.HomeTeam;
        existingEvent.AwayTeam = dto.AwayTeam;
        existingEvent.EventDate = dto.EventDate;
        existingEvent.SportKey = dto.SportKey;
        existingEvent.HomeWinOdds = dto.HomeWinOdds;
        existingEvent.DrawOdds = dto.DrawOdds;
        existingEvent.AwayWinOdds = dto.AwayWinOdds;
        existingEvent.LastUpdated = DateTime.UtcNow;

        await sportRepository.UpdateAsync(existingEvent);
    }

    public async Task DeleteEventAsync(Guid id)
    {
        var existingEvent = await sportRepository.GetByIdAsync(id) 
                            ?? throw new KeyNotFoundException("Event not found.");

        await sportRepository.DeleteAsync(existingEvent);
    }
}