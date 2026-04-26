namespace _4Bet.Infrastructure.Domain;
using System.ComponentModel.DataAnnotations;

public class Wallet : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    [Range(0, double.MaxValue, ErrorMessage = "Balance cannot be negative")]
    public decimal Balance { get; set; } = 0m;
    
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}