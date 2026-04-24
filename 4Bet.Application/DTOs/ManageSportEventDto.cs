using System.ComponentModel.DataAnnotations;
namespace _4Bet.Application.DTOs;

public class ManageSportEventDto
{
    [Required] public string ExternalId { get; set; } = string.Empty;
    [Required] public string HomeTeam { get; set; } = string.Empty;
    [Required] public string AwayTeam { get; set; } = string.Empty;
    [Required] public DateTime EventDate { get; set; }
    [Required] public string SportKey { get; set; } = string.Empty;
    
    [Range(1.0, 1000.0)] public double HomeWinOdds { get; set; }
    [Range(1.0, 1000.0)] public double DrawOdds { get; set; }
    [Range(1.0, 1000.0)] public double AwayWinOdds { get; set; }
}