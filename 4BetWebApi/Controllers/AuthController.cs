using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using _4Bet.Application.DTOs;
using _4Bet.Application.IServices;
using Microsoft.AspNetCore.Authorization;
using _4Bet.Infrastructure.IRepositories;

namespace _4BetWebApi.Controllers;

[ApiController]
[Route("api/[controller]")] // Шлях буде: api/auth
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IVerificationService _verificationService;
    private readonly IVerificationRepository _verificationRepository;
    public AuthController(IAuthService authService, IVerificationService verificationService, IVerificationRepository verificationRepository)
    {
        _authService = authService;
        _verificationService = verificationService;
        _verificationRepository = verificationRepository;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegistrationDto registerDto)
    {
        try
        {
            var token = await _authService.RegisterAsync(registerDto);
            return Ok(new
            {
                Message = "Register success",
                Token = token
            });
        }
        catch (Exception ex) when (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new { message = ex.Message });
        }
    }
    
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // 1. Pass the user's email and the code they typed to the VerificationService
        var isVerified = await _verificationService.VerifyCodeAsync(dto.Email, dto.Code);

        // 2. Return the appropriate HTTP response to React
        if (isVerified)
        {
            return Ok(new { message = "Email verified successfully! You can now log in." });
        }

        return BadRequest(new { message = "Invalid or expired verification code." });
    }
    [HttpPost("resend-code")]
    public async Task<IActionResult> ResendCode([FromBody] ResendCodeDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // We call the service. Notice we always return OK even if the email doesn't exist.
        // This is a standard security practice so hackers can't use this endpoint to guess registered emails.
        await _verificationService.ResendCodeAsync(dto.Email);

        return Ok(new { message = "If an unverified account with that email exists, a new code has been sent." });
    }
    
    [HttpPost("cancel-registration")]
    public async Task<IActionResult> CancelRegistration([FromBody] ResendCodeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
        {
            return BadRequest(new { message = "Email is required." });
        }

        var removed = await _authService.CancelPendingRegistrationAsync(dto.Email);
        if (!removed)
        {
            return Ok(new { message = "No pending registration found for this email." });
        }

        return Ok(new { message = "Pending registration deleted." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginDto loginDto)
    {
        try 
        {
            var token = await _authService.LoginAsync(loginDto);
            return Ok(new { Token = token });
        }
        catch (UnauthorizedAccessException ex)
        {
            // Якщо пароль невірний — повертаємо 401
            return Unauthorized(new { message = ex.Message });
        }
    }
    [Authorize]
    [HttpPost("verify-id")]
    public async Task<IActionResult> VerifyId(IFormFile file)
    {
        if (file == null) return BadRequest(new { message = "No file uploaded" });

        // Assuming you get UserId from JWT Token
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null) return Unauthorized();
    
        var userId = Guid.Parse(userIdClaim);

        using var stream = file.OpenReadStream();
        var result = await _verificationService.VerifyAgeAsync(stream, file.FileName, userId);

        return result switch
        {
            "SUCCESS" => Ok(new { 
                status = "Verified", 
                message = "Identity and age verified successfully." 
            }),
            "TOO_YOUNG" => BadRequest(new { 
                status = "Rejected", 
                message = "Access denied. You must be at least 21 years old." 
            }),
            "PENDING_REVIEW" => Ok(new { 
                status = "Pending", 
                message = "Document quality is low. Verification is pending manual review by an administrator." 
            }),
            "DATA_NOT_FOUND" => BadRequest(new { 
                status = "Error", 
                message = "Could not extract birth date from the provided document. Please try again with a clearer photo." 
            }),
            _ => StatusCode(500, new { message = "An internal error occurred during verification." })
        };
    }

    [Authorize]
    [HttpPost("skip-document-verification")]
    public async Task<IActionResult> SkipDocumentVerification()
    {
        var userId = GetUserId();
        var updated = await _authService.SkipDocumentVerificationAsync(userId);
        if (!updated)
        {
            return NotFound(new { message = "User not found." });
        }

        return Ok(new
        {
            status = "Verified",
            message = "Document verification has been skipped temporarily."
        });
    }

    [Authorize]
    [HttpGet("profile")]
    public async Task<ActionResult<UserProfileDto>> GetProfile()
    {
        var userId = GetUserId();
        var profile = await _authService.GetProfileAsync(userId);
        return profile is null ? NotFound() : Ok(profile);
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<ActionResult<UserProfileDto>> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = GetUserId();
        var profile = await _authService.UpdateProfileAsync(userId, dto);
        return profile is null ? NotFound() : Ok(profile);
    }

    [Authorize]
    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var userId = GetUserId();
            await _authService.ChangePasswordAsync(userId, dto);
            return Ok(new { message = "Password updated successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPut("avatar")]
    public async Task<ActionResult<UserProfileDto>> UpdateAvatar([FromBody] UpdateAvatarDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = GetUserId();
        var profile = await _authService.UpdateAvatarAsync(userId, dto);
        return profile is null ? NotFound() : Ok(profile);
    }

    [Authorize]
    [HttpGet("verification-status")]
    public async Task<IActionResult> GetVerificationStatus()
    {
        var userId = GetUserId();
        var requests = await _verificationRepository.GetByUserIdAsync(userId);
        var latest = requests.FirstOrDefault();

        if (latest == null)
        {
            return Ok(new
            {
                status = "NotRequested",
                message = "Verification has not been requested yet.",
                action = "none"
            });
        }

        if (latest.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new
            {
                status = "Approved",
                message = "Your documents were approved. You can continue to the dashboard.",
                action = "dashboard"
            });
        }

        if (latest.Status.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new
            {
                status = "Rejected",
                message = "Your documents were rejected. Please return to the start screen and upload a clearer document.",
                action = "start"
            });
        }

        return Ok(new
        {
            status = "Pending",
            message = "Your documents are under review by an administrator.",
            action = "none"
        });
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