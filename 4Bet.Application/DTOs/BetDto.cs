using _4Bet.Infrastructure.Domain;

namespace _4Bet.Application.DTOs;

public class BetDto
{
    public Guid Id { get; set; }
    public decimal Stake { get; set; }
    public double CombinedOdds { get; set; }
    public decimal PotentialPayout { get; set; }
    public BetStatus Status { get; set; }
    public decimal? SettledPayout { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SettledAt { get; set; }
    public List<BetLegDto> Legs { get; set; } = new();
}

public class BetLegDto
{
    public string EventExternalId { get; set; } = string.Empty;
    public string HomeTeam { get; set; } = string.Empty;
    public string AwayTeam { get; set; } = string.Empty;
    public BetSelection Selection { get; set; }
    public double LockedOdds { get; set; }
}
