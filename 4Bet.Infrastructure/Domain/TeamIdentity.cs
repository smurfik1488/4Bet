namespace _4Bet.Infrastructure.Domain;

public class TeamIdentity : BaseEntity
{
    public string Provider { get; set; } = "ApiFootball";
    public int ProviderTeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public string TeamNameNormalized { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
}
