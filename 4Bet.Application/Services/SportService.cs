using AutoMapper;
using _4Bet.Application.DTOs;
using _4Bet.Application.IServices;
using _4Bet.Infrastructure.Domain;
using _4Bet.Infrastructure.IRepositories;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace _4Bet.Application.Services;

public class SportService(
    ISportRepository sportRepository,
    IMapper mapper,
    IBusinessRulesService businessRules,
    IAuditLogService auditLogService) : ISportService
{
    private sealed record MissingTeamLogoRequest(string Name, string Normalized);
    private static readonly HttpClient LogoHttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(8)
    };

    private static readonly Dictionary<string, int> ManualTeamLogoIds = new()
    {
        ["arsenal"] = 42,
        ["newcastleunited"] = 34,
        ["manchesterunited"] = 33,
        ["liverpool"] = 40,
        ["chelsea"] = 49,
        ["valencia"] = 532,
        ["girona"] = 547,
        ["atleticomadrid"] = 530,
        ["atléticomadrid"] = 530,
        ["athleticbilbao"] = 531,
        ["realsociedad"] = 548,
        ["villarreal"] = 533,
        ["celtavigo"] = 538,
        ["espanyol"] = 540,
        ["levante"] = 539,
        ["getafe"] = 546,
        ["rayo"] = 728,
        ["rayovallecano"] = 728,
        ["realbetis"] = 543,
        ["osasuna"] = 727,
        ["caosasuna"] = 727,
        ["barcelona"] = 529,
        ["realmadrid"] = 541,
        ["sevilla"] = 536
    };

    public async Task<IEnumerable<SportEventDto>> GetActiveEventsAsync()
    {
        var events = await sportRepository.GetActiveEventsAsync();
        var mappedEvents = mapper.Map<List<SportEventDto>>(events);

        var normalizedNames = mappedEvents
            .SelectMany(x => new[] { NormalizeTeamName(x.HomeTeam), NormalizeTeamName(x.AwayTeam) })
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();
        var cachedTeamIdentities = await sportRepository.GetTeamIdentitiesByNormalizedNamesAsync(normalizedNames);
        var logoByNormalizedName = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var group in cachedTeamIdentities.GroupBy(x => x.TeamNameNormalized))
        {
            var best = group
                .OrderByDescending(x => x.LastSeenAt)
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.LogoUrl));
            if (best != null)
            {
                logoByNormalizedName[group.Key] = best.LogoUrl;
            }
        }

        foreach (var ev in mappedEvents)
        {
            var homeNormalized = NormalizeTeamName(ev.HomeTeam);
            var awayNormalized = NormalizeTeamName(ev.AwayTeam);
            if (logoByNormalizedName.TryGetValue(homeNormalized, out var homeLogo) && !string.IsNullOrWhiteSpace(homeLogo))
            {
                ev.HomeTeamLogoUrl = homeLogo;
            }

            if (logoByNormalizedName.TryGetValue(awayNormalized, out var awayLogo) && !string.IsNullOrWhiteSpace(awayLogo))
            {
                ev.AwayTeamLogoUrl = awayLogo;
            }
        }

        var missingTeams = mappedEvents
            .SelectMany(ev => new[]
            {
                new { Name = ev.HomeTeam, Normalized = NormalizeTeamName(ev.HomeTeam), NeedsLogo = string.IsNullOrWhiteSpace(ev.HomeTeamLogoUrl) },
                new { Name = ev.AwayTeam, Normalized = NormalizeTeamName(ev.AwayTeam), NeedsLogo = string.IsNullOrWhiteSpace(ev.AwayTeamLogoUrl) }
            })
            .Where(x => x.NeedsLogo && !string.IsNullOrWhiteSpace(x.Normalized))
            .GroupBy(x => x.Normalized)
            .Select(g => new MissingTeamLogoRequest(g.First().Name, g.Key))
            .ToList();

        if (missingTeams.Count > 0)
        {
            var resolvedIdentities = await ResolveMissingTeamLogosAsync(missingTeams);
            if (resolvedIdentities.Count > 0)
            {
                await sportRepository.UpsertTeamIdentitiesAsync(resolvedIdentities);
                foreach (var identity in resolvedIdentities)
                {
                    logoByNormalizedName[identity.TeamNameNormalized] = identity.LogoUrl;
                }
            }
        }

        foreach (var ev in mappedEvents)
        {
            if (string.IsNullOrWhiteSpace(ev.HomeTeamLogoUrl))
            {
                var normalizedHome = NormalizeTeamName(ev.HomeTeam);
                if (logoByNormalizedName.TryGetValue(normalizedHome, out var resolvedHomeLogo) && !string.IsNullOrWhiteSpace(resolvedHomeLogo))
                {
                    ev.HomeTeamLogoUrl = resolvedHomeLogo;
                }
            }

            if (string.IsNullOrWhiteSpace(ev.HomeTeamLogoUrl))
            {
                var normalizedHome = NormalizeTeamName(ev.HomeTeam);
                if (ManualTeamLogoIds.TryGetValue(normalizedHome, out var homeTeamId))
                {
                    ev.HomeTeamLogoUrl = BuildApiSportsLogoUrl(homeTeamId);
                }
                else
                {
                    ev.HomeTeamLogoUrl = BuildInlineFallbackLogo(ev.HomeTeam);
                }
            }

            if (string.IsNullOrWhiteSpace(ev.AwayTeamLogoUrl))
            {
                var normalizedAway = NormalizeTeamName(ev.AwayTeam);
                if (logoByNormalizedName.TryGetValue(normalizedAway, out var resolvedAwayLogo) && !string.IsNullOrWhiteSpace(resolvedAwayLogo))
                {
                    ev.AwayTeamLogoUrl = resolvedAwayLogo;
                }
            }

            if (string.IsNullOrWhiteSpace(ev.AwayTeamLogoUrl))
            {
                var normalizedAway = NormalizeTeamName(ev.AwayTeam);
                if (ManualTeamLogoIds.TryGetValue(normalizedAway, out var awayTeamId))
                {
                    ev.AwayTeamLogoUrl = BuildApiSportsLogoUrl(awayTeamId);
                }
                else
                {
                    ev.AwayTeamLogoUrl = BuildInlineFallbackLogo(ev.AwayTeam);
                }
            }

            ev.HomeTeamLogoUrl = TeamLogoUrls.WrapForBrowserProxy(ev.HomeTeamLogoUrl);
            ev.AwayTeamLogoUrl = TeamLogoUrls.WrapForBrowserProxy(ev.AwayTeamLogoUrl);
        }

        return mappedEvents;
    }
    
    public async Task<SportEventDto> AddEventAsync(ManageSportEventDto dto)
    {
        businessRules.ValidateSportEventInput(dto);
        businessRules.EnsureValidOdds(dto.HomeWinOdds, dto.DrawOdds, dto.AwayWinOdds);

        var newEvent = new SportEvent
        {
            ExternalId = string.IsNullOrWhiteSpace(dto.ExternalId) ? GenerateManualExternalId() : dto.ExternalId.Trim(),
            HomeTeam = dto.HomeTeam,
            AwayTeam = dto.AwayTeam,
            EventDate = dto.EventDate,
            SportKey = dto.SportKey,
            HomeWinOdds = dto.HomeWinOdds,
            DrawOdds = dto.DrawOdds,
            AwayWinOdds = dto.AwayWinOdds,
            HomeScore = dto.HomeScore,
            AwayScore = dto.AwayScore,
            MatchStatus = businessRules.NormalizeMatchStatus(dto.MatchStatus, "NS"),
            MatchMinute = dto.MatchMinute,
            LastUpdated = DateTime.UtcNow
        };

        await sportRepository.AddAsync(newEvent);
        await auditLogService.LogAsync(
            action: "SportEventCreated",
            entityType: "SportEvent",
            entityId: newEvent.Id,
            userId: null,
            summary: $"Event created: {newEvent.HomeTeam} vs {newEvent.AwayTeam}.",
            payload: new { newEvent.EventDate, newEvent.SportKey, newEvent.ExternalId });
        return mapper.Map<SportEventDto>(newEvent);
    }

    public async Task UpdateEventAsync(Guid id, ManageSportEventDto dto)
    {
        businessRules.ValidateSportEventInput(dto);
        businessRules.EnsureValidOdds(dto.HomeWinOdds, dto.DrawOdds, dto.AwayWinOdds);

        var existingEvent = await sportRepository.GetByIdAsync(id) 
                            ?? throw new KeyNotFoundException("Event not found.");

        if (!string.IsNullOrWhiteSpace(dto.ExternalId))
        {
            existingEvent.ExternalId = dto.ExternalId.Trim();
        }
        existingEvent.HomeTeam = dto.HomeTeam;
        existingEvent.AwayTeam = dto.AwayTeam;
        existingEvent.EventDate = dto.EventDate;
        existingEvent.SportKey = dto.SportKey;
        existingEvent.HomeWinOdds = dto.HomeWinOdds;
        existingEvent.DrawOdds = dto.DrawOdds;
        existingEvent.AwayWinOdds = dto.AwayWinOdds;
        existingEvent.HomeScore = dto.HomeScore;
        existingEvent.AwayScore = dto.AwayScore;
        existingEvent.MatchStatus = string.IsNullOrWhiteSpace(dto.MatchStatus)
            ? existingEvent.MatchStatus
            : businessRules.NormalizeMatchStatus(dto.MatchStatus, existingEvent.MatchStatus);
        existingEvent.MatchMinute = dto.MatchMinute;
        existingEvent.LastUpdated = DateTime.UtcNow;

        await sportRepository.UpdateAsync(existingEvent);
        await auditLogService.LogAsync(
            action: "SportEventUpdated",
            entityType: "SportEvent",
            entityId: existingEvent.Id,
            userId: null,
            summary: $"Event updated: {existingEvent.HomeTeam} vs {existingEvent.AwayTeam}.",
            payload: new { existingEvent.EventDate, existingEvent.SportKey, existingEvent.MatchStatus });
    }

    public async Task DeleteEventAsync(Guid id)
    {
        var existingEvent = await sportRepository.GetByIdAsync(id) 
                            ?? throw new KeyNotFoundException("Event not found.");

        await sportRepository.DeleteAsync(existingEvent);
        await auditLogService.LogAsync(
            action: "SportEventDeleted",
            entityType: "SportEvent",
            entityId: existingEvent.Id,
            userId: null,
            summary: $"Event deleted: {existingEvent.HomeTeam} vs {existingEvent.AwayTeam}.",
            payload: new { existingEvent.ExternalId, existingEvent.EventDate });
    }

    public async Task<TeamImportResultDto> ImportTeamsFromJsonAsync(Stream jsonStream, CancellationToken cancellationToken = default)
    {
        using var document = await JsonDocument.ParseAsync(jsonStream, cancellationToken: cancellationToken);
        var parsedRows = ParseTeamRows(document.RootElement);
        if (parsedRows.Count == 0)
        {
            throw new InvalidOperationException("No team rows found in JSON file.");
        }

        var validRows = parsedRows
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .Select(x => new
            {
                Name = x.Name.Trim(),
                Normalized = NormalizeTeamName(x.Name),
                x.LogoUrl
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Normalized))
            .ToList();

        var uniqueByNormalized = validRows
            .GroupBy(x => x.Normalized)
            .Select(g => g.First())
            .ToList();
        var normalizedNames = uniqueByNormalized.Select(x => x.Normalized).ToList();
        var existing = await sportRepository.GetTeamIdentitiesByNormalizedNamesAsync(normalizedNames);
        var existingNormalized = existing
            .Select(x => x.TeamNameNormalized)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.Ordinal);

        var newIdentities = uniqueByNormalized
            .Where(x => !existingNormalized.Contains(x.Normalized))
            .Select(x => new TeamIdentity
            {
                Provider = "ModeratorJson",
                ProviderTeamId = GetStableId($"moderator-json:{x.Normalized}"),
                TeamName = x.Name,
                TeamNameNormalized = x.Normalized,
                LogoUrl = string.IsNullOrWhiteSpace(x.LogoUrl) ? null : x.LogoUrl,
                LastSeenAt = DateTime.UtcNow
            })
            .ToList();

        if (newIdentities.Count > 0)
        {
            await sportRepository.UpsertTeamIdentitiesAsync(newIdentities);
        }

        var result = new TeamImportResultDto
        {
            TotalRows = parsedRows.Count,
            UniqueRows = uniqueByNormalized.Count,
            InsertedRows = newIdentities.Count,
            ExistingRows = uniqueByNormalized.Count - newIdentities.Count,
            InvalidRows = Math.Max(0, parsedRows.Count - validRows.Count)
        };

        await auditLogService.LogAsync(
            action: "TeamIdentityJsonImported",
            entityType: "TeamIdentity",
            entityId: null,
            userId: null,
            summary: $"Moderator team import. Inserted: {result.InsertedRows}, existing: {result.ExistingRows}, invalid: {result.InvalidRows}.",
            payload: result,
            cancellationToken: cancellationToken);

        return result;
    }

    private static string GenerateManualExternalId()
    {
        return $"manual-{Guid.NewGuid():N}";
    }

    private static string NormalizeTeamName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.ToLowerInvariant();
        normalized = normalized.Replace("football club", " ");
        normalized = normalized.Replace("fc", " ");
        normalized = normalized.Replace("cf", " ");
        normalized = normalized.Replace("utd", "united");
        normalized = new string(normalized.Where(char.IsLetterOrDigit).ToArray());
        return normalized;
    }

    private static string BuildApiSportsLogoUrl(int teamId)
    {
        return $"https://media.api-sports.io/football/teams/{teamId}.png";
    }

    private async Task<List<TeamIdentity>> ResolveMissingTeamLogosAsync(IEnumerable<MissingTeamLogoRequest> missingTeams)
    {
        var resolved = new List<TeamIdentity>();

        foreach (var team in missingTeams)
        {
            var normalizedName = team.Normalized;
            var teamName = team.Name;

            string? logoUrl = null;
            if (ManualTeamLogoIds.TryGetValue(normalizedName, out var knownTeamId))
            {
                logoUrl = BuildApiSportsLogoUrl(knownTeamId);
            }
            else
            {
                logoUrl = await TryResolveLogoFromTheSportsDbAsync(teamName);
            }

            logoUrl ??= BuildInlineFallbackLogo(teamName);
            resolved.Add(new TeamIdentity
            {
                Provider = "LogoResolver",
                ProviderTeamId = GetStableId(normalizedName),
                TeamName = teamName,
                TeamNameNormalized = normalizedName,
                LogoUrl = logoUrl,
                LastSeenAt = DateTime.UtcNow
            });
        }

        return resolved;
    }

    private static async Task<string?> TryResolveLogoFromTheSportsDbAsync(string teamName)
    {
        try
        {
            var encoded = Uri.EscapeDataString(teamName.Trim());
            var response = await LogoHttpClient.GetFromJsonAsync<TheSportsDbSearchResponse>(
                $"https://www.thesportsdb.com/api/v1/json/3/searchteams.php?t={encoded}");

            var candidates = response?.Teams;
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            var normalizedTarget = NormalizeTeamName(teamName);
            var exact = candidates.FirstOrDefault(x => NormalizeTeamName(x.Name) == normalizedTarget);
            var best = exact ?? candidates.FirstOrDefault();
            return string.IsNullOrWhiteSpace(best?.Badge) ? null : best.Badge;
        }
        catch
        {
            return null;
        }
    }

    private static int GetStableId(string normalizedName)
    {
        unchecked
        {
            int hash = 17;
            foreach (var c in normalizedName)
            {
                hash = (hash * 31) + c;
            }

            return Math.Abs(hash == int.MinValue ? int.MaxValue : hash);
        }
    }

    private static string BuildInlineFallbackLogo(string teamName)
    {
        var normalized = (teamName ?? "Team").Trim();
        var initials = normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(part => char.ToUpperInvariant(part[0]))
            .ToArray();
        var text = initials.Length > 0 ? new string(initials) : "TM";

        var palette = new (string Start, string End)[]
        {
            ("#1D2B52", "#26396C"),
            ("#1E3A8A", "#1D4ED8"),
            ("#14532D", "#15803D"),
            ("#5B21B6", "#7C3AED"),
            ("#7F1D1D", "#B91C1C"),
            ("#0F766E", "#0D9488")
        };
        var index = Math.Abs(normalized.ToLowerInvariant().Sum(ch => ch)) % palette.Length;
        var colors = palette[index];

        var safeName = EscapeXml(normalized);
        var safeText = EscapeXml(text);
        var svg = $"""
<svg xmlns="http://www.w3.org/2000/svg" width="96" height="96" viewBox="0 0 96 96" role="img" aria-label="{safeName}">
  <defs>
    <linearGradient id="g" x1="0" x2="1" y1="0" y2="1">
      <stop offset="0%" stop-color="{colors.Start}"/>
      <stop offset="100%" stop-color="{colors.End}"/>
    </linearGradient>
  </defs>
  <rect width="96" height="96" rx="48" fill="url(#g)"/>
  <text x="50%" y="54%" dominant-baseline="middle" text-anchor="middle" font-family="Inter,Segoe UI,Arial,sans-serif" font-size="34" font-weight="700" fill="#FFFFFF">{safeText}</text>
</svg>
""";
        return $"data:image/svg+xml;charset=UTF-8,{Uri.EscapeDataString(svg)}";
    }

    private static string EscapeXml(string value)
    {
        var safe = value.Replace("&", "&amp;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
        return safe;
    }

    private sealed class TheSportsDbSearchResponse
    {
        [JsonPropertyName("teams")]
        public List<TheSportsDbTeam>? Teams { get; set; }
    }

    private sealed class TheSportsDbTeam
    {
        [JsonPropertyName("strTeam")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("strTeamBadge")]
        public string? Badge { get; set; }
    }

    private sealed record TeamJsonRow(string Name, string? LogoUrl);

    private static List<TeamJsonRow> ParseTeamRows(JsonElement root)
    {
        var rows = new List<TeamJsonRow>();
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                TryAppendTeamRow(item, rows);
            }
            return rows;
        }

        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("teams", out var teamsElement) &&
            teamsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in teamsElement.EnumerateArray())
            {
                TryAppendTeamRow(item, rows);
            }
        }

        return rows;
    }

    private static void TryAppendTeamRow(JsonElement element, List<TeamJsonRow> rows)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            var rawName = element.GetString() ?? string.Empty;
            rows.Add(new TeamJsonRow(rawName, null));
            return;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var name = TryGetString(element, "name")
            ?? TryGetString(element, "teamName")
            ?? TryGetString(element, "clubName")
            ?? string.Empty;
        var logoUrl = TryGetString(element, "logoUrl") ?? TryGetString(element, "logo");
        rows.Add(new TeamJsonRow(name, logoUrl));
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return property.GetString();
    }
}
