using _4Bet.Infrastructure.Domain;

namespace _4Bet.Application.IServices;

public interface IVerificationService
{
    public Task<string> VerifyAgeAsync(Stream fileStream, string fileName, Guid userId);
    public Task GenerateAndSendCodeAsync(User user);
    public Task<bool> VerifyCodeAsync(string email, string code);
    Task ResendCodeAsync(string email);

}