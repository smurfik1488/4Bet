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
public class WalletController(IWalletService walletService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<WalletBalanceDto>> GetBalance(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var balance = await walletService.GetBalanceAsync(userId, cancellationToken);
        return balance is null ? NotFound() : Ok(balance);
    }

    [HttpPost("top-up")]
    public async Task<ActionResult<WalletBalanceDto>> TopUp([FromBody] WalletTopUpRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = GetUserId();
            var updated = await walletService.TopUpAsync(userId, request.Amount, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("withdraw")]
    public async Task<ActionResult<WalletBalanceDto>> Withdraw([FromBody] WalletTopUpRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = GetUserId();
            var updated = await walletService.WithdrawAsync(userId, request.Amount, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
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
