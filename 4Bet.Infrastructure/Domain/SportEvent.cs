// 4Bet.Infrastructure/Domain/SportEvent.cs
using System.ComponentModel.DataAnnotations;

namespace _4Bet.Infrastructure.Domain;

public class SportEvent : BaseEntity
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

    // --- НОВІ ПОЛЯ ДЛЯ ЛАЙВ-МАТЧІВ ---
    public int? HomeScore { get; set; } 
    public int? AwayScore { get; set; }
    public string MatchStatus { get; set; } = "Not Started"; // Наприклад: NS, 1H, HT, 2H, FT
    public int? MatchMinute { get; set; }
    public ICollection<BetLeg> BetLegs { get; set; } = new List<BetLeg>();
}