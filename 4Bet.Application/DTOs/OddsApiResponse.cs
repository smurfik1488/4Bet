using System.Text.Json.Serialization;

namespace _4Bet.Application.DTOs.External;

public class OddsApiResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("sport_key")]
    public string SportKey { get; set; } = string.Empty;

    [JsonPropertyName("home_team")]
    public string HomeTeam { get; set; } = string.Empty;

    [JsonPropertyName("away_team")]
    public string AwayTeam { get; set; } = string.Empty;

    [JsonPropertyName("commence_time")]
    public DateTime CommenceTime { get; set; }

    [JsonPropertyName("bookmakers")]
    public List<Bookmaker> Bookmakers { get; set; } = new();
}