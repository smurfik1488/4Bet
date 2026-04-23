namespace _4Bet.Application.IServices;

public interface IVerificationService
{
    public Task<string> VerifyAgeAsync(Stream fileStream, string fileName, Guid userId);

}