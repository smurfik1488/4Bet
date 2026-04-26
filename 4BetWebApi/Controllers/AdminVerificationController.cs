using _4Bet.Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _4BetWebApi.Controllers;

[ApiController]
[Route("api/admin/verifications")]
[Authorize(Roles = "Admin")]
public class AdminVerificationController : ControllerBase
{
    private readonly IAdminVerificationService _adminVerificationService;

    public AdminVerificationController(IAdminVerificationService adminVerificationService)
    {
        _adminVerificationService = adminVerificationService;
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingRequests()
    {
        var requests = await _adminVerificationService.GetPendingRequestsAsync();
        return Ok(requests);
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> ApproveRequest(Guid id)
    {
        var result = await _adminVerificationService.ApproveRequestAsync(id);

        return result switch
        {
            "SUCCESS" => Ok(new { message = "User verification approved successfully." }),
            "NOT_FOUND" => NotFound(new { message = "Verification request not found." }),
            "ALREADY_PROCESSED" => BadRequest(new { message = "This request has already been processed." }),
            _ => StatusCode(500, new { message = "Internal server error." })
        };
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> RejectRequest(Guid id)
    {
        var result = await _adminVerificationService.RejectRequestAsync(id);

        return result switch
        {
            "SUCCESS" => Ok(new { message = "User verification rejected." }),
            "NOT_FOUND" => NotFound(new { message = "Verification request not found." }),
            "ALREADY_PROCESSED" => BadRequest(new { message = "This request has already been processed." }),
            _ => StatusCode(500, new { message = "Internal server error." })
        };
    }
}