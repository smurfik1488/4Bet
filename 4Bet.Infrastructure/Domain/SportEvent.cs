// 4Bet.Infrastructure/Domain/SportEvent.cs
using System.ComponentModel.DataAnnotations;

namespace _4Bet.Infrastructure.Domain;

public class SportEvent : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string ExternalId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string HomeTeam { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string AwayTeam { get; set; } = string.Empty;

    [Required]
    public DateTime EventDate { get; set; }

    [Required]
    [MaxLength(50)]
    public string SportKey { get; set; } = string.Empty;

    [Range(1.0, 1000.0)]
    public double HomeWinOdds { get; set; }

    [Range(1.0, 1000.0)]
    public double DrawOdds { get; set; }

    [Range(1.0, 1000.0)]
    public double AwayWinOdds { get; set; }

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}