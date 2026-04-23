using _4Bet.Application.IServices;
using _4Bet.Infrastructure.Data;
using _4Bet.Infrastructure.Domain;
using _4Bet.Infrastructure.IRepositories;
// using _4Bet.Domain.Entities;

namespace _4Bet.Application.Services;

public class AdminVerificationService : IAdminVerificationService
{
    private readonly IVerificationRepository _requestRepository;
    private readonly IAuthRepository _authRepository;
    private readonly FourBetDbContext _context;

    public AdminVerificationService(
        IVerificationRepository requestRepository, 
        IAuthRepository authRepository, 
        FourBetDbContext context)
    {
        _requestRepository = requestRepository;
        _authRepository = authRepository;
        _context = context;
    }

    public async Task<IEnumerable<VerificationRequest>> GetPendingRequestsAsync()
    {
        return await _requestRepository.GetPendingRequestsAsync();
    }

    public async Task<string> ApproveRequestAsync(Guid requestId)
    {
        var request = await _requestRepository.GetByIdAsync(requestId);
        
        if (request == null) return "NOT_FOUND";
        if (request.Status != "Pending") return "ALREADY_PROCESSED";

        // Змінюємо статус запиту
        request.Status = "Approved";

        // Знаходимо користувача і підтверджуємо йому вік
        var user = await _authRepository.GetByIdAsync(request.UserId);
        if (user != null)
        {
            user.IsBdVerified = true;
        }

        // SaveChangesAsync збереже і оновлений запит, і оновленого юзера, 
        // бо EF Core відслідковує ці об'єкти
        await _context.SaveChangesAsync(); 
        
        return "SUCCESS";
    }

    public async Task<string> RejectRequestAsync(Guid requestId)
    {
        var request = await _requestRepository.GetByIdAsync(requestId);
        
        if (request == null) return "NOT_FOUND";
        if (request.Status != "Pending") return "ALREADY_PROCESSED";

        request.Status = "Rejected";
        await _context.SaveChangesAsync();

        return "SUCCESS";
    }
}