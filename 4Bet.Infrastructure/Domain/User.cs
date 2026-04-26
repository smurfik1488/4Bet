using System.ComponentModel.DataAnnotations;

namespace _4Bet.Infrastructure.Domain;

public class User : BaseEntity
{
    [Required]
    [EmailAddress]
    public string? Email { get; set; }
    [Required]
    public string? PasswordHash { get; set; }
    [Required]
    [RegularExpression("^[a-zA-Zа-яА-ЯіІїЇєЄґҐ' ]+$",ErrorMessage = "First name must contain only letters")]
    public string? FirstName { get; set; }
    [Required]
    [RegularExpression("^[a-zA-Zа-яА-ЯіІїЇєЄґҐ' ]+$",ErrorMessage = "Last name must contain only letters")]
    public string? LastName { get; set; }
    [Required]
    [DataType(DataType.Date)]
    public DateTime Birthday { get; set; }
    [Required]
    public bool IsBdVerified { get; set; } = false;
    [Required]
    public bool IsEmailVerified { get; set; } = false;
    public ICollection<VerificationRequest> VerificationRequests { get; set; } = new List<VerificationRequest>();
    [Required]
    public UserRole Role { get; set; }
    [Required]
    public Wallet Wallet { get; set; } = null!;
    public ICollection<Bet> Bets { get; set; } = new List<Bet>();
    public string? AvatarDataUrl { get; set; }
}