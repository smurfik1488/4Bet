
using System.Text.Json.Serialization;

namespace _4Bet.Application.DTOs;

public class ApiFootballLiveResponse
{
    [JsonPropertyName("response")]
    public List<ApiFootballFixtureItem>? Response { get; set; }
}

public class ApiFootballFixtureItem
{
    [JsonPropertyName("fixture")]
    public FixtureInfo Fixture { get; set; } = new();

    [JsonPropertyName("teams")]
    public TeamsInfo Teams { get; set; } = new();

    [JsonPropertyName("goals")]
    public GoalsInfo Goals { get; set; } = new();
}

public class FixtureInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("status")]
    public FixtureStatus Status { get; set; } = new();
}

public class FixtureStatus
{
    [JsonPropertyName("short")] // NS, 1H, HT, 2H, FT тощо
    public string Short { get; set; } = string.Empty;

    [JsonPropertyName("elapsed")] // Поточна хвилина
    public int? Elapsed { get; set; }
}

public class TeamsInfo
{
    [JsonPropertyName("home")]
    public TeamDetail Home { get; set; } = new();

    [JsonPropertyName("away")]
    public TeamDetail Away { get; set; } = new();
}

public class TeamDetail
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("logo")]
    public string? Logo { get; set; }
}

public class GoalsInfo
{
    [JsonPropertyName("home")]
    public int? Home { get; set; }

    [JsonPropertyName("away")]
    public int? Away { get; set; }
}