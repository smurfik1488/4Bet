namespace _4Bet.Application.IServices;
using DTOs;

public interface IAuthService
{
    public Task<string> LoginAsync(UserLoginDto dto);

    public Task<string> RegisterAsync(UserRegistrationDto dto);
}