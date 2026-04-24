using Microsoft.AspNetCore.Mvc;
using _4Bet.Application.DTOs;
using _4Bet.Application.IServices;
using Microsoft.AspNetCore.Authorization;

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
    
    [Authorize(Roles = "Admin,Moderator")]
    [HttpPost]
    public async Task<IActionResult> AddEvent([FromBody] ManageSportEventDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var createdEvent = await sportService.AddEventAsync(dto);
        return Ok(createdEvent);
    }

    [Authorize(Roles = "Admin,Moderator")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] ManageSportEventDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            await sportService.UpdateEventAsync(id, dto);
            return NoContent(); // 204 No Content is standard for successful PUT
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin,Moderator")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEvent(Guid id)
    {
        try
        {
            await sportService.DeleteEventAsync(id);
            return NoContent(); 
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}