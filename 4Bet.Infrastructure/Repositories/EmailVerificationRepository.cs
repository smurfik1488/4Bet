using _4Bet.Infrastructure.Data;
using _4Bet.Infrastructure.Domain;
using _4Bet.Infrastructure.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace _4Bet.Infrastructure.Repositories;

public class EmailVerificationRepository(FourBetDbContext dbContext) : IEmailVerificationRepository
{
    
    public async Task AddAsync(EmailVerificationRequest request)
    {
        await dbContext.EmailVerificationRequests.AddAsync(request);
        await dbContext.SaveChangesAsync();
    }



    public async Task<EmailVerificationRequest?> GetLatestCodeForUserAsync(Guid userId)
    {
        // We find the newest verification request for this specific user
        // by ordering them by their expiration date in descending order.
        return await dbContext.EmailVerificationRequests
            .Where(vr => vr.UserId == userId)
            .OrderByDescending(vr => vr.ExpiresAt)
            .FirstOrDefaultAsync();
    }

    public async Task DeleteAsync(EmailVerificationRequest request)
    {
        dbContext.EmailVerificationRequests.Remove(request);
        await dbContext.SaveChangesAsync();
    }
    
    public async Task InvalidateOldCodesAsync(Guid userId)
    {
        // Find all existing codes for this user
        var oldCodes = await dbContext.EmailVerificationRequests
            .Where(vr => vr.UserId == userId)
            .ToListAsync();

        if (oldCodes.Any())
        {
            // Delete them
            dbContext.EmailVerificationRequests.RemoveRange(oldCodes);
            await dbContext.SaveChangesAsync();
        }
    }
}