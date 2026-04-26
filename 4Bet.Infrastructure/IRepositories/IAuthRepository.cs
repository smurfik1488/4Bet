namespace _4Bet.Infrastructure.IRepositories;
using Domain;

public interface IAuthRepository
{
    public Task<User?> GetByEmailAsync(string email);

    public Task<User?> GetByIdAsync(Guid id);

    public Task AddAsync(User user);

    public Task<bool> ExistsAsync(string email);

    public Task UpdateAsync(User user);
    public Task<bool> RemovePendingByEmailAsync(string email);
}