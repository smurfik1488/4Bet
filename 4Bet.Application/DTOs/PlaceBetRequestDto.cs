using System.ComponentModel.DataAnnotations;
using _4Bet.Infrastructure.Domain;

namespace _4Bet.Application.DTOs;

public class PlaceBetRequestDto
{
    [Range(1, 1_000_000)]
    public decimal Stake { get; set; }

    [MinLength(1)]
    public List<PlaceBetLegDto> Legs { get; set; } = new();
}

public class PlaceBetLegDto
{
    [Required]
    public string EventExternalId { get; set; } = string.Empty;

    [Required]
    public BetSelection Selection { get; set; }

    [Range(1.01, 1000)]
    public double RequestedOdds { get; set; }
}
