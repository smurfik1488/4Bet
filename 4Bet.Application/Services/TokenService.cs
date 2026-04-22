using _4Bet.Application.IServices;

namespace _4Bet.Application.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using _4Bet.Infrastructure.Domain;

public class TokenService : ITokenService
{
    private readonly SymmetricSecurityKey _key;
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
        // Беремо наш секретний ключ і перетворюємо його на байтовий масив
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtSettings:Key"]));
    }

    public string CreateToken(User user)
    {
        // 1. Створюємо Claims (корисне навантаження токена)
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.NameId, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName),
            // Кастомний claim для віку
            new Claim("is_verified", user.IsBdVerified.ToString().ToLower()),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        // 2. Створюємо підпис (Credentials)
        var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256Signature);

        // 3. Описуємо сам токен
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.Now.AddMinutes(Convert.ToDouble(_config["JwtSettings:DurationInMinutes"])),
            SigningCredentials = creds,
            Issuer = _config["JwtSettings:Issuer"],
            Audience = _config["JwtSettings:Audience"]
        };

        // 4. Генеруємо та повертаємо рядок
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}