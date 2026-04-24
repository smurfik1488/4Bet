using _4Bet.Infrastructure.Domain;

namespace _4Bet.Infrastructure.IRepositories;

public interface IEmailVerificationRepository
{
    Task AddAsync(EmailVerificationRequest request);
    Task<EmailVerificationRequest?> GetLatestCodeForUserAsync(Guid userId);
    Task DeleteAsync(EmailVerificationRequest request);
    Task InvalidateOldCodesAsync(Guid userId);
}