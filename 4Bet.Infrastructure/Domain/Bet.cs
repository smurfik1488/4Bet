namespace _4Bet.Infrastructure.Domain;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _4Bet.Infrastructure.Domain;




public class Bet : BaseEntity
{
    [Required]
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    [Required]
    public Guid SportEventId { get; set; }
    public SportEvent SportEvent { get; set; } = null!;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; } // Скільки поставив гравець

    [Required]
    public double LockedOdds { get; set; } // Коефіцієнт, який був у момент ставки

    [Required]
    public BetSelection Selection { get; set; } // Вибраний результат (1, X, 2)

    [Required]
    public BetStatus Status { get; set; } = BetStatus.Pending;

    // Скільки фактично виплачено після завершення. 
    // Заповнюється тільки коли статус стає Won або Refunded.
    [Column(TypeName = "decimal(18,2)")]
    public decimal? Payout { get; set; } 

    public DateTime PlacedAt { get; set; } = DateTime.UtcNow;
}