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

    public async Task<User?> GetByIdAsync(int id)
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
}