using Microsoft.AspNetCore.Mvc;
using _4Bet.Application.DTOs;
using _4Bet.Application.IServices;

namespace _4BetWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SportController(ISportService sportService) : ControllerBase
{
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<SportEventDto>>> GetActiveEvents()
    {
        var activeEvents = await sportService.GetActiveEventsAsync();
        
        if (!activeEvents.Any())
        {
            return NoContent(); // Returns 204 if there are no events in the database yet
        }

        return Ok(activeEvents); // Returns 200 with the JSON array
    }
}