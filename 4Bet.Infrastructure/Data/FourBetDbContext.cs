using _4Bet.Infrastructure.Domain;
using Microsoft.EntityFrameworkCore;

namespace _4Bet.Infrastructure.Data;

public class FourBetDbContext : DbContext {
    public FourBetDbContext(DbContextOptions<FourBetDbContext> options) : base(options) { }
    public DbSet<User> Users { get; set; }
    public DbSet<VerificationRequest> VerificationRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VerificationRequest>()
            .HasOne(r => r.User)           // Запит має одного юзера
            .WithMany(u => u.VerificationRequests) // Юзер має багато запитів
            .HasForeignKey(r => r.UserId)  // Зовнішній ключ
            .OnDelete(DeleteBehavior.Cascade);
    
    }
    
}