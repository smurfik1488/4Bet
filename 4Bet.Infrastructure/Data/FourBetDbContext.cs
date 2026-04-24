using _4Bet.Infrastructure.Domain;
using Microsoft.EntityFrameworkCore;

namespace _4Bet.Infrastructure.Data;

public class FourBetDbContext : DbContext {
    public FourBetDbContext(DbContextOptions<FourBetDbContext> options) : base(options) { }
    public DbSet<User> Users { get; set; }
    public DbSet<VerificationRequest> VerificationRequests { get; set; }
    
    public DbSet<SportEvent> SportEvents { get; set; }
    
    public DbSet<EmailVerificationRequest> EmailVerificationRequests { get; set; }
    public DbSet<Wallet> Wallets { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<VerificationRequest>()
            .HasOne(r => r.User)           // Запит має одного юзера
            .WithMany(u => u.VerificationRequests) // Юзер має багато запитів
            .HasForeignKey(r => r.UserId)  // Зовнішній ключ
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SportEvent>(entity =>
        {
            entity.HasIndex(e => e.ExternalId).IsUnique();
        });
        modelBuilder.Entity<User>()
            .HasOne(u => u.Wallet)
            .WithOne(w => w.User)
            .HasForeignKey<Wallet>(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    
    }
    
}