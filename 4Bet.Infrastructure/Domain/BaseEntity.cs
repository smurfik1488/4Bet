using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace _4Bet.Infrastructure.Domain;

public class BaseEntity
{
    [Required]
    [Key]
    public Guid Id { get; set; }
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Required]
    public DateTime UpdatedAt { get; set; }
    [Required]
    public bool IsDeleted { get; set; } = false;
}