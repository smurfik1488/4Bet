namespace _4Bet.Application.IServices;
using DTOs;

public interface IAuthService
{
    public Task<string> LoginAsync(UserLoginDto dto);

    public Task<string> RegisterAsync(UserRegistrationDto dto);
    public Task<bool> CancelPendingRegistrationAsync(string email);
    Task<UserProfileDto?> GetProfileAsync(Guid userId);
    Task<UserProfileDto?> UpdateProfileAsync(Guid userId, UpdateProfileDto dto);
    Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
    Task<UserProfileDto?> UpdateAvatarAsync(Guid userId, UpdateAvatarDto dto);
    Task<bool> SkipDocumentVerificationAsync(Guid userId);
}