using _4Bet.Infrastructure.Data;
using _4Bet.Infrastructure.Domain;
using _4Bet.Infrastructure.IRepositories;

namespace _4Bet.Infrastructure.Repositories;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class VerificationRepository : IVerificationRepository
{
    private readonly FourBetDbContext _context;

    public VerificationRepository(FourBetDbContext context)
    {
        _context = context;
    }

    public async Task<VerificationRequest?> GetByIdAsync(Guid id)
    {
        return await _context.VerificationRequests
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<VerificationRequest>> GetPendingRequestsAsync()
    {
        // Повертаємо всі запити, які чекають на перевірку, відсортовані від найстаріших
        return await _context.VerificationRequests
            .Include(r => r.User)
            .Where(r => r.Status == "Pending")
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<VerificationRequest>> GetByUserIdAsync(Guid userId)
    {
        // Може знадобитися, щоб показати юзеру історію його заявок
        return await _context.VerificationRequests
            .Include(r => r.User)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(VerificationRequest request)
    {
        await _context.VerificationRequests.AddAsync(request);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(VerificationRequest request)
    {
        _context.VerificationRequests.Update(request);
        await _context.SaveChangesAsync();
    }
}