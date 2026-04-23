using System.Text.Json.Serialization;

namespace _4Bet.Application.DTOs.External;
public class Outcome
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public double Price { get; set; }
}