using System.ComponentModel.DataAnnotations;

namespace _4Bet.Infrastructure.Domain;

public class BetLeg : BaseEntity
{
    [Required]
    public Guid BetId { get; set; }
    public Bet Bet { get; set; } = null!;

    [Required]
    public Guid SportEventId { get; set; }
    public SportEvent SportEvent { get; set; } = null!;

    [Required]
    public BetSelection Selection { get; set; }

    [Required]
    public double LockedOdds { get; set; }
}
