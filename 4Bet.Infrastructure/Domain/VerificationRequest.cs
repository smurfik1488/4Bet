using System.ComponentModel.DataAnnotations;

namespace _4Bet.Infrastructure.Domain;

public class VerificationRequest : BaseEntity
{
    [Required]
    public Guid UserId { get; set; } // Хто завантажив
    [Required]
    public string DocumentUrl { get; set; } = string.Empty; // Посилання на фото в Azure
    [Required]
    public string Status { get; set; } = "Pending";
    [Required]
    public User User { get; set; }
    
}