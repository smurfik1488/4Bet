using _4Bet.Infrastructure.Domain; 

namespace _4Bet.Application.IServices;

public interface IAdminVerificationService
{
    Task<IEnumerable<VerificationRequest>> GetPendingRequestsAsync();
    Task<string> ApproveRequestAsync(Guid requestId);
    Task<string> RejectRequestAsync(Guid requestId);
}