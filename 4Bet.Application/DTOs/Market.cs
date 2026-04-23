using System.Text.Json.Serialization;

namespace _4Bet.Application.DTOs.External;

public class Market
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("outcomes")]
    public List<Outcome> Outcomes { get; set; } = new();
}