using _4Bet.Infrastructure.Domain;
using Microsoft.EntityFrameworkCore;

namespace _4Bet.Infrastructure.Data;

public class FourBetDbContext : DbContext {
    public FourBetDbContext(DbContextOptions<FourBetDbContext> options) : base(options) { }
    public DbSet<User> Users { get; set; }
    
}