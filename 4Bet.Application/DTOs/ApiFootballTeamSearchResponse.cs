using System.Text.Json.Serialization;

namespace _4Bet.Application.DTOs;

public class ApiFootballTeamSearchResponse
{
    [JsonPropertyName("response")]
    public List<ApiFootballTeamSearchItem>? Response { get; set; }
}

public class ApiFootballTeamSearchItem
{
    [JsonPropertyName("team")]
    public ApiFootballTeamInfo Team { get; set; } = new();
}

public class ApiFootballTeamInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;

    [JsonPropertyName("logo")]
    public string? Logo { get; set; }
}
