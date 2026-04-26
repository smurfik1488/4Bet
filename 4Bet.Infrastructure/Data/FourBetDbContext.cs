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
    public DbSet<Bet> Bets { get; set; }
    public DbSet<BetLeg> BetLegs { get; set; }
    public DbSet<TeamIdentity> TeamIdentities { get; set; }
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

        modelBuilder.Entity<User>()
            .HasMany(u => u.Bets)
            .WithOne(b => b.User)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Bet>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.Status });
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<BetLeg>(entity =>
        {
            entity.HasIndex(e => new { e.BetId, e.SportEventId }).IsUnique();
            entity.HasIndex(e => e.SportEventId);
            entity.HasOne(e => e.SportEvent)
                .WithMany(s => s.BetLegs)
                .HasForeignKey(e => e.SportEventId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TeamIdentity>(entity =>
        {
            entity.HasIndex(e => new { e.Provider, e.ProviderTeamId }).IsUnique();
            entity.HasIndex(e => e.TeamNameNormalized);
        });
    }
    
}