using Microsoft.AspNetCore.Mvc;
using _4Bet.Application.DTOs;
using _4Bet.Application.IServices;

namespace _4BetWebApi.Controllers;

[ApiController]
[Route("api/[controller]")] // Шлях буде: api/auth
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
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
}