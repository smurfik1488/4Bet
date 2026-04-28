using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using _4Bet.Application.DTOs;
using _4Bet.Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _4BetWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BetController(IBetService betService, IBetAnalyticsService betAnalyticsService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<BetDto>> PlaceBet([FromBody] PlaceBetRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = GetUserId();
            var bet = await betService.PlaceBetAsync(userId, request, cancellationToken);
            return Ok(bet);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<BetDto>>> GetMyBets()
    {
        var userId = GetUserId();
        var bets = await betService.GetMyBetsAsync(userId);
        return Ok(bets);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BetDto>> GetById(Guid id)
    {
        var userId = GetUserId();
        var bet = await betService.GetMyBetByIdAsync(userId, id);
        return bet is null ? NotFound() : Ok(bet);
    }

    [HttpGet("analytics/mine")]
    public async Task<ActionResult<BetAnalyticsDto>> GetMyAnalytics([FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken cancellationToken)
    {
        if (from == default || to == default)
        {
            return BadRequest(new { message = "Query params 'from' and 'to' are required (ISO date)." });
        }

        var maxRangeDays = 366;
        if (Math.Abs((to - from).TotalDays) > maxRangeDays)
        {
            return BadRequest(new { message = $"Date range cannot exceed {maxRangeDays} days." });
        }

        var userId = GetUserId();
        var analytics = await betAnalyticsService.GetUserAnalyticsAsync(userId, from.ToUniversalTime(), to.ToUniversalTime(), cancellationToken);
        return Ok(analytics);
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue(JwtRegisteredClaimNames.NameId)
                    ?? User.FindFirstValue("nameid");
        return Guid.TryParse(claim, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Invalid token user id.");
    }
}
