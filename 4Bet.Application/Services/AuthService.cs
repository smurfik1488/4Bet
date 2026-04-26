
using _4Bet.Application.Mappings;
using _4Bet.Infrastructure.Data;
using _4Bet.Infrastructure.Domain;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace _4Bet.Application.Services;
using IServices;
using DTOs;
using Infrastructure.IRepositories;
public class AuthService : IAuthService
{
    private readonly IAuthRepository _authRepository;
    private readonly ITokenService _tokenService;
    private readonly IMapper _mapper;
    private readonly FourBetDbContext _context;
    private readonly IVerificationService _verificationService;
    private readonly IEmailVerificationRepository _emailVerificationRepository;
    
    public AuthService(
        IAuthRepository authRepository,
        ITokenService tokenService,
        IMapper mapper,
        FourBetDbContext context,
        IVerificationService verificationService,
        IEmailVerificationRepository emailVerificationRepository)
    {
        _authRepository = authRepository;
        _tokenService = tokenService;
        _mapper = mapper;
        _context = context;
        _verificationService = verificationService;
        _emailVerificationRepository = emailVerificationRepository;
   
    }

    public async Task<string> LoginAsync(UserLoginDto dto)
    {
        // Ми не знаємо, як репозиторій шукає юзера (SQL, NoSQL чи In-memory)
        var user = await _authRepository.GetByEmailAsync(dto.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials");

        if (!user.IsEmailVerified)
            throw new UnauthorizedAccessException("Email is not verified. Please confirm your code first.");

        return _tokenService.CreateToken(user);
    }
    public async Task<string> RegisterAsync(UserRegistrationDto dto)
    {
        // 1. Перевіряємо, чи такий Email вже є (через репозиторій)
        if (await _authRepository.ExistsAsync(dto.Email))
        {
            throw new Exception("User with this email already exists");
        }
        // 3. Створюємо сутність
        var user = _mapper.Map<User>(dto);

        // 4. Додаємо через репозиторій
        await _authRepository.AddAsync(user);
        
        await _context.SaveChangesAsync();
        await _verificationService.GenerateAndSendCodeAsync(user);

        // 6. Повертаємо токен
        return _tokenService.CreateToken(user);
    }

    public async Task<bool> CancelPendingRegistrationAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var normalizedEmail = email.Trim();
        var user = await _authRepository.GetByEmailAsync(normalizedEmail);
        if (user == null || user.IsEmailVerified)
        {
            return false;
        }

        await _emailVerificationRepository.InvalidateOldCodesAsync(user.Id);
        return await _authRepository.RemovePendingByEmailAsync(normalizedEmail);
    }

    public async Task<UserProfileDto?> GetProfileAsync(Guid userId)
    {
        var user = await _authRepository.GetByIdAsync(userId);
        if (user == null || user.IsDeleted)
        {
            return null;
        }

        return new UserProfileDto
        {
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            AvatarDataUrl = user.AvatarDataUrl,
            IsBdVerified = user.IsBdVerified
        };
    }

    public async Task<UserProfileDto?> UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
    {
        var user = await _authRepository.GetByIdAsync(userId);
        if (user == null || user.IsDeleted)
        {
            return null;
        }

        user.FirstName = dto.FirstName.Trim();
        user.LastName = dto.LastName.Trim();
        user.UpdatedAt = DateTime.UtcNow;
        await _authRepository.UpdateAsync(user);
        await _context.SaveChangesAsync();

        return new UserProfileDto
        {
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            AvatarDataUrl = user.AvatarDataUrl,
            IsBdVerified = user.IsBdVerified
        };
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        var user = await _authRepository.GetByIdAsync(userId);
        if (user == null || user.IsDeleted)
        {
            throw new InvalidOperationException("User not found.");
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
        {
            throw new InvalidOperationException("Current password is invalid.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _authRepository.UpdateAsync(user);
        await _context.SaveChangesAsync();
    }

    public async Task<UserProfileDto?> UpdateAvatarAsync(Guid userId, UpdateAvatarDto dto)
    {
        var user = await _authRepository.GetByIdAsync(userId);
        if (user == null || user.IsDeleted)
        {
            return null;
        }

        user.AvatarDataUrl = dto.AvatarDataUrl.Trim();
        user.UpdatedAt = DateTime.UtcNow;
        await _authRepository.UpdateAsync(user);
        await _context.SaveChangesAsync();

        return new UserProfileDto
        {
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            AvatarDataUrl = user.AvatarDataUrl,
            IsBdVerified = user.IsBdVerified
        };
    }

    public async Task<bool> SkipDocumentVerificationAsync(Guid userId)
    {
        var user = await _authRepository.GetByIdAsync(userId);
        if (user == null || user.IsDeleted)
        {
            return false;
        }

        user.IsBdVerified = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _authRepository.UpdateAsync(user);
        await _context.SaveChangesAsync();
        return true;
    }
}