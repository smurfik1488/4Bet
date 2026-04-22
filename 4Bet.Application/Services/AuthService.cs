
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
    
    public AuthService(IAuthRepository authRepository, ITokenService tokenService, IMapper mapper, FourBetDbContext context)
    {
        _authRepository = authRepository;
        _tokenService = tokenService;
        _mapper = mapper;
        _context = context;
    }

    public async Task<string> LoginAsync(UserLoginDto dto)
    {
        // Ми не знаємо, як репозиторій шукає юзера (SQL, NoSQL чи In-memory)
        var user = await _authRepository.GetByEmailAsync(dto.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials");

        return _tokenService.CreateToken(user);
    }
    public async Task<string> RegisterAsync(UserRegistrationDto dto)
    {
        // 1. Перевіряємо, чи такий Email вже є (через репозиторій)
        if (await _authRepository.ExistsAsync(dto.Email))
        {
            throw new Exception("User with this email already exists");
        }

        // 2. Хешуємо пароль
        // var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        // 3. Створюємо сутність
        var user = _mapper.Map<User>(dto);

        // 4. Додаємо через репозиторій
        await _authRepository.AddAsync(user);
        
        await _context.SaveChangesAsync();

        // 6. Повертаємо токен
        return _tokenService.CreateToken(user);
    }
}