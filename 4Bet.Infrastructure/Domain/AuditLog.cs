using System.ComponentModel.DataAnnotations;

namespace _4Bet.Infrastructure.Domain;

public class AuditLog : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(50)]
    public string EntityType { get; set; } = string.Empty;

    public Guid? EntityId { get; set; }

    public Guid? UserId { get; set; }

    [MaxLength(300)]
    public string Summary { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? PayloadJson { get; set; }
}
