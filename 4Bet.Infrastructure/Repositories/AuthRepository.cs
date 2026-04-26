using _4Bet.Infrastructure.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace _4Bet.Infrastructure.Repositories;
using Data;
using Domain;
public class AuthRepository : IAuthRepository
{
    private readonly FourBetDbContext _context;

    public AuthRepository(FourBetDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
    }

    public async Task<bool> ExistsAsync(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email);
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await Task.CompletedTask; 
    }

    public async Task<bool> RemovePendingByEmailAsync(string email)
    {
        var normalizedEmail = email.Trim();
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail && !u.IsDeleted);
        if (user == null || user.IsEmailVerified)
        {
            return false;
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }
}