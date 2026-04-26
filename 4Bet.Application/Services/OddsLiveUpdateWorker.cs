using _4Bet.Application.DTOs.External;
using _4Bet.Application.DTOs;
using _4Bet.Application.IServices;
using _4Bet.Infrastructure.Domain;
using _4Bet.Infrastructure.IRepositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace _4Bet.Application.Services;

public class OddsLiveUpdateWorker(
    IServiceProvider serviceProvider,
    ILogger<OddsLiveUpdateWorker> logger) : BackgroundService
{
    private static readonly TimeSpan LivePollingInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan NearKickoffPollingInterval = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan WarmupPollingInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan IdlePollingInterval = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan NearKickoffWindow = TimeSpan.FromMinutes(25);
    private static readonly TimeSpan WarmupWindow = TimeSpan.FromHours(6);

    private readonly string[] _targetLeagues =
    {
        "soccer_epl",
        "soccer_spain_la_liga",
        "soccer_uefa_champs_league",
        "soccer_uefa_europa_conference_league"
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Odds Live Update Worker started.");
        await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var nextDelay = WarmupPollingInterval;
            try
            {
                using var scope = serviceProvider.CreateScope();
                var parser = scope.ServiceProvider.GetRequiredService<ISportParserService>();
                var repo = scope.ServiceProvider.GetRequiredService<ISportRepository>();
                var notificationService = scope.ServiceProvider.GetRequiredService<ISportNotificationService>();
                var changed = new List<SportEvent>();
                var scoreUpdates = new List<SportEvent>();

                foreach (var league in _targetLeagues)
                {
                    var apiData = await parser.GetFootballOddsAsync(league, stoppingToken);
                    if (apiData is null || apiData.Count == 0)
                    {
                        continue;
                    }

                    var mapped = apiData
                        .Where(dto => dto.CommenceTime > DateTime.UtcNow)
                        .Select(MapToDomain)
                        .Where(ev => ev != null)
                        .Cast<SportEvent>()
                        .ToList();

                    if (mapped.Count == 0)
                    {
                        continue;
                    }

                    var existing = (await repo.GetByExternalIdsAsync(mapped.Select(e => e.ExternalId)))
                        .ToDictionary(e => e.ExternalId);

                    foreach (var candidate in mapped)
                    {
                        if (!existing.TryGetValue(candidate.ExternalId, out var current))
                        {
                            changed.Add(candidate);
                            continue;
                        }

                        if (Math.Abs(candidate.HomeWinOdds - current.HomeWinOdds) > 0.001d
                            || Math.Abs(candidate.DrawOdds - current.DrawOdds) > 0.001d
                            || Math.Abs(candidate.AwayWinOdds - current.AwayWinOdds) > 0.001d)
                        {
                            changed.Add(candidate);
                        }
                    }

                    var scoreFeed = await parser.GetFootballScoresAsync(league, stoppingToken);
                    if (scoreFeed is { Count: > 0 })
                    {
                        var byExternalId = (await repo.GetByExternalIdsAsync(scoreFeed.Select(x => x.Id)))
                            .ToDictionary(e => e.ExternalId);

                        foreach (var scoreItem in scoreFeed)
                        {
                            if (!byExternalId.TryGetValue(scoreItem.Id, out var existingEvent))
                            {
                                continue;
                            }

                            var (homeScore, awayScore) = ExtractScores(scoreItem, existingEvent.HomeTeam, existingEvent.AwayTeam);
                            if (homeScore == null && awayScore == null)
                            {
                                continue;
                            }

                            var hasChangedScore = existingEvent.HomeScore != homeScore || existingEvent.AwayScore != awayScore;
                            var targetStatus = scoreItem.Completed ? "FT" : "LIVE";
                            var hasChangedStatus = !string.Equals(existingEvent.MatchStatus, targetStatus, StringComparison.OrdinalIgnoreCase);
                            if (!hasChangedScore && !hasChangedStatus)
                            {
                                continue;
                            }

                            existingEvent.HomeScore = homeScore;
                            existingEvent.AwayScore = awayScore;
                            existingEvent.MatchStatus = targetStatus;
                            existingEvent.LastUpdated = DateTime.UtcNow;
                            scoreUpdates.Add(existingEvent);
                        }
                    }
                }

                var upsertBuffer = changed
                    .Concat(scoreUpdates)
                    .GroupBy(x => x.ExternalId)
                    .Select(g => g.Last())
                    .ToList();

                // Fallback: if score provider does not mark "completed" quickly,
                // close stale LIVE matches by kickoff time so they disappear from active feed.
                var activeEvents = (await repo.GetActiveEventsAsync()).ToList();
                var staleLiveCutoff = DateTime.UtcNow.AddHours(-3);
                var staleLiveEvents = activeEvents
                    .Where(e =>
                        string.Equals(e.MatchStatus, "LIVE", StringComparison.OrdinalIgnoreCase) &&
                        e.EventDate <= staleLiveCutoff)
                    .ToList();
                foreach (var stale in staleLiveEvents)
                {
                    stale.MatchStatus = "FT";
                    stale.LastUpdated = DateTime.UtcNow;
                }

                upsertBuffer = upsertBuffer
                    .Concat(staleLiveEvents)
                    .GroupBy(x => x.ExternalId)
                    .Select(g => g.Last())
                    .ToList();

                if (upsertBuffer.Count > 0)
                {
                    await repo.UpsertEventsAsync(upsertBuffer);
                }

                if (changed.Count > 0)
                {
                    await notificationService.BroadcastOddsUpdatedAsync(changed.Select(c => new OddsUpdateDto
                    {
                        ExternalId = c.ExternalId,
                        HomeWinOdds = c.HomeWinOdds,
                        DrawOdds = c.DrawOdds,
                        AwayWinOdds = c.AwayWinOdds,
                        LastUpdated = DateTime.UtcNow
                    }));
                    logger.LogInformation("Pushed {Count} live odds updates.", changed.Count);
                }

                if (scoreUpdates.Count > 0)
                {
                    logger.LogInformation("Applied {Count} score updates from The Odds API.", scoreUpdates.Count);
                }

                nextDelay = ResolveNextDelay(activeEvents, DateTime.UtcNow);
                logger.LogInformation("Next OddsLiveUpdateWorker run in {Seconds} sec.", nextDelay.TotalSeconds);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Critical error in OddsLiveUpdateWorker");
                nextDelay = NearKickoffPollingInterval;
            }

            await Task.Delay(nextDelay, stoppingToken);
        }
    }

    private static SportEvent? MapToDomain(OddsApiResponse dto)
    {
        var bookmaker = dto.Bookmakers?.FirstOrDefault();
        var market = bookmaker?.Markets?.FirstOrDefault(m => m.Key == "h2h");
        if (market is null)
        {
            return null;
        }

        return new SportEvent
        {
            ExternalId = dto.Id,
            HomeTeam = dto.HomeTeam,
            AwayTeam = dto.AwayTeam,
            EventDate = dto.CommenceTime,
            SportKey = dto.SportKey,
            HomeWinOdds = market.Outcomes.FirstOrDefault(o => o.Name == dto.HomeTeam)?.Price ?? 1.0,
            AwayWinOdds = market.Outcomes.FirstOrDefault(o => o.Name == dto.AwayTeam)?.Price ?? 1.0,
            DrawOdds = market.Outcomes.FirstOrDefault(o => o.Name == "Draw")?.Price ?? 1.0,
            LastUpdated = DateTime.UtcNow
        };
    }

    private static (int? HomeScore, int? AwayScore) ExtractScores(
        OddsScoreResponse source,
        string currentHomeTeam,
        string currentAwayTeam)
    {
        if (source.Scores == null || source.Scores.Count == 0)
        {
            return (null, null);
        }

        int? Parse(string? value) => int.TryParse(value, out var parsed) ? parsed : null;
        var home = source.Scores.FirstOrDefault(x => TeamMatches(x.Name, source.HomeTeam) || TeamMatches(x.Name, currentHomeTeam));
        var away = source.Scores.FirstOrDefault(x => TeamMatches(x.Name, source.AwayTeam) || TeamMatches(x.Name, currentAwayTeam));
        return (Parse(home?.Score), Parse(away?.Score));
    }

    private static bool TeamMatches(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        var normalizedLeft = NormalizeTeamName(left);
        var normalizedRight = NormalizeTeamName(right);
        return normalizedLeft == normalizedRight ||
               normalizedLeft.Contains(normalizedRight) ||
               normalizedRight.Contains(normalizedLeft);
    }

    private static string NormalizeTeamName(string value)
    {
        var normalized = value.ToLowerInvariant().Trim();
        normalized = normalized.Replace("football club", " ");
        normalized = normalized.Replace("fc", " ");
        normalized = normalized.Replace("cf", " ");
        normalized = normalized.Replace("utd", "united");
        normalized = new string(normalized.Where(char.IsLetterOrDigit).ToArray());
        return normalized;
    }

    private static TimeSpan ResolveNextDelay(IReadOnlyCollection<SportEvent> activeEvents, DateTime utcNow)
    {
        var hasLiveNow = activeEvents.Any(e => IsLiveStatus(e.MatchStatus, e.EventDate, utcNow));
        if (hasLiveNow)
        {
            return LivePollingInterval;
        }

        var nearestUpcomingStart = activeEvents
            .Where(e => !IsFinishedStatus(e.MatchStatus) && e.EventDate >= utcNow)
            .Select(e => e.EventDate)
            .OrderBy(d => d)
            .FirstOrDefault();

        if (nearestUpcomingStart != default)
        {
            var untilKickoff = nearestUpcomingStart - utcNow;
            if (untilKickoff <= NearKickoffWindow)
            {
                return NearKickoffPollingInterval;
            }

            if (untilKickoff <= WarmupWindow)
            {
                return WarmupPollingInterval;
            }
        }

        return IdlePollingInterval;
    }

    private static bool IsLiveStatus(string status, DateTime eventDate, DateTime utcNow)
    {
        var normalized = (status ?? string.Empty).Trim().ToUpperInvariant();
        if (normalized is "LIVE" or "1H" or "HT" or "2H" or "ET" or "INPLAY" or "IN_PLAY")
        {
            return true;
        }

        // Fallback for score feeds that may not return detailed status quickly.
        return !IsFinishedStatus(status) && eventDate <= utcNow && eventDate >= utcNow.AddHours(-3);
    }

    private static bool IsFinishedStatus(string status)
    {
        var normalized = (status ?? string.Empty).Trim().ToUpperInvariant();
        return normalized is "FT" or "AET" or "PEN" or "CANC" or "PST" or "FINISHED";
    }
}
