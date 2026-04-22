namespace _4Bet.Application.IServices;
using _4Bet.Infrastructure.Domain;
public interface ITokenService
{
    string CreateToken(User user);
}