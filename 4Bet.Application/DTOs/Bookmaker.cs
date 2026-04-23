using System.Text.Json.Serialization;

namespace _4Bet.Application.DTOs.External;
public class Bookmaker
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("markets")]
    public List<Market> Markets { get; set; } = new();
}