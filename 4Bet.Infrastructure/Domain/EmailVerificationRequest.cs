namespace _4Bet.Infrastructure.Domain;

public class EmailVerificationRequest : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Code { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}