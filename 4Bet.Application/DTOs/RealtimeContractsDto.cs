namespace _4Bet.Application.DTOs;

public class OddsUpdateDto
{
    public string ExternalId { get; set; } = string.Empty;
    public double HomeWinOdds { get; set; }
    public double DrawOdds { get; set; }
    public double AwayWinOdds { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class BetLifecycleUpdateDto
{
    public Guid BetId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public decimal? SettledPayout { get; set; }
    public string Status { get; set; } = string.Empty;
}
