namespace _4Bet.Application.DTOs;

public class SportEventDto
{
    public string ExternalId { get; set; } = string.Empty;
    public string HomeTeam { get; set; } = string.Empty;
    public string AwayTeam { get; set; } = string.Empty;
    public string? HomeTeamLogoUrl { get; set; }
    public string? AwayTeamLogoUrl { get; set; }
    public DateTime EventDate { get; set; }
    public string SportKey { get; set; } = string.Empty;
    public double HomeWinOdds { get; set; }
    public double DrawOdds { get; set; }
    public double AwayWinOdds { get; set; }
    public DateTime LastUpdated { get; set; }

    // --- НОВІ ПОЛЯ ДЛЯ ВІДПРАВКИ НА ФРОНТЕНД ---
    public int? HomeScore { get; set; } 
    public int? AwayScore { get; set; }
    public string MatchStatus { get; set; } = string.Empty;
    public int? MatchMinute { get; set; }
}