using Microsoft.AspNetCore.Mvc;
using _4Bet.Application.DTOs;
using _4Bet.Application.Services;
using _4Bet.Application.IServices;
using Microsoft.AspNetCore.Authorization;

namespace _4BetWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SportController(ISportService sportService) : ControllerBase
{
    /// <summary>
    /// Proxies team badge images from approved CDNs (same-origin for the browser).
    /// </summary>
    [HttpGet("team-logo")]
    [AllowAnonymous]
    [ResponseCache(Duration = 86_400, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetTeamLogo(
        [FromServices] IHttpClientFactory httpClientFactory,
        [FromQuery] string u,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(u) || u.Length > 4096)
        {
            return BadRequest();
        }

        var decoded = Uri.UnescapeDataString(u.Trim());
        if (decoded.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest();
        }

        if (!Uri.TryCreate(decoded, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return BadRequest();
        }

        if (!TeamLogoUrls.IsAllowedLogoHost(uri.Host))
        {
            return BadRequest();
        }

        var client = httpClientFactory.CreateClient("TeamLogoProxy");
        using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return NotFound();
        }

        var mediaType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        return File(bytes, mediaType);
    }

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

        try
        {
            var createdEvent = await sportService.AddEventAsync(dto);
            return Ok(createdEvent);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
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
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
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