using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using _4Bet.Application.DTOs;
using _4Bet.Application.IServices;
using Microsoft.AspNetCore.Authorization;

namespace _4BetWebApi.Controllers;

[ApiController]
[Route("api/[controller]")] // Шлях буде: api/auth
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IVerificationService _verificationService;
    public AuthController(IAuthService authService, IVerificationService verificationService)
    {
        _authService = authService;
        _verificationService = verificationService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegistrationDto registerDto)
    {
        // Метод поверне токен після успішної реєстрації
        var token = await _authService.RegisterAsync(registerDto);
        
        return Ok(new { 
            Message = "Register success", 
            Token = token 
        });
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
}