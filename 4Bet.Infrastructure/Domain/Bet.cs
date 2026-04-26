using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _4Bet.Infrastructure.Domain;

public class Bet : BaseEntity
{
    [Required]
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Stake { get; set; }

    [Required]
    public double CombinedOdds { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PotentialPayout { get; set; }

    [Required]
    public BetStatus Status { get; set; } = BetStatus.Pending;

    [Column(TypeName = "decimal(18,2)")]
    public decimal? SettledPayout { get; set; }

    public DateTime? SettledAt { get; set; }

    public ICollection<BetLeg> Legs { get; set; } = new List<BetLeg>();
}