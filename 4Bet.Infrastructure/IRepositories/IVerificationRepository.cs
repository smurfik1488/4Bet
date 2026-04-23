using _4Bet.Infrastructure.Data;
using _4Bet.Infrastructure.Domain;

namespace _4Bet.Infrastructure.IRepositories;

public interface IVerificationRepository
{

    public Task<VerificationRequest?> GetByIdAsync(Guid id);
    public Task<IEnumerable<VerificationRequest>> GetPendingRequestsAsync();

    public Task<IEnumerable<VerificationRequest>> GetByUserIdAsync(Guid userId);

    public Task AddAsync(VerificationRequest request);
    public Task UpdateAsync(VerificationRequest request);
}