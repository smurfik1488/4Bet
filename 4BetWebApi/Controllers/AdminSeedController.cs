using _4Bet.Application.DTOs;
using _4Bet.Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _4BetWebApi.Controllers;

[ApiController]
[Route("api/admin/seed")]
[Authorize(Roles = "Admin")]
public class AdminSeedController(IDataSeedService dataSeedService) : ControllerBase
{
    [HttpPost("hybrid")]
    public async Task<ActionResult<SeedResultDto>> RunHybridSeed(CancellationToken cancellationToken)
    {
        var result = await dataSeedService.SeedHybridAsync(cancellationToken);
        return Ok(result);
    }
}
