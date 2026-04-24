namespace _4Bet.Application.DTOs;

public class SportEventDto
{
    public string ExternalId { get; set; } = string.Empty;
    public string HomeTeam { get; set; } = string.Empty;
    public string AwayTeam { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string SportKey { get; set; } = string.Empty;
    public double HomeWinOdds { get; set; }
    public double DrawOdds { get; set; }
    public double AwayWinOdds { get; set; }
    public DateTime LastUpdated { get; set; }
}