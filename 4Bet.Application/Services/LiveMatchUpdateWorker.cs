using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using _4Bet.Application.IServices;
using _4Bet.Application.DTOs;
using _4Bet.Infrastructure.Domain;
using _4Bet.Infrastructure.IRepositories;
using AutoMapper;

namespace _4Bet.Application.Services;

public class LiveMatchUpdateWorker(
    IServiceProvider serviceProvider,
    ILogger<LiveMatchUpdateWorker> logger) : BackgroundService
{
    private readonly TimeSpan _pollingInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Live Match Update Worker (API-Football) started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var footballApi = scope.ServiceProvider.GetRequiredService<IApiFootballService>();
                var repo = scope.ServiceProvider.GetRequiredService<ISportRepository>();
                var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
                var notificationService = scope.ServiceProvider.GetRequiredService<ISportNotificationService>();
                var betService = scope.ServiceProvider.GetRequiredService<IBetService>();

                var liveFixtures = await footballApi.GetLiveFixturesAsync(stoppingToken) ?? new List<ApiFootballFixtureItem>();
                var upcomingFixtures = await footballApi.GetUpcomingFixturesAsync(40, stoppingToken) ?? new List<ApiFootballFixtureItem>();
                var fixturesById = new Dictionary<int, ApiFootballFixtureItem>();

                foreach (var fixture in liveFixtures)
                {
                    fixturesById[fixture.Fixture.Id] = fixture;
                }

                foreach (var fixture in upcomingFixtures)
                {
                    fixturesById[fixture.Fixture.Id] = fixture;
                }

                var allFixtures = fixturesById.Values.ToList();

                if (allFixtures.Any())
                {
                    var teamIdentities = allFixtures
                        .SelectMany(f => new[]
                        {
                            new TeamIdentity
                            {
                                Provider = "ApiFootball",
                                ProviderTeamId = f.Teams.Home.Id,
                                TeamName = f.Teams.Home.Name,
                                TeamNameNormalized = NormalizeTeamName(f.Teams.Home.Name),
                                LogoUrl = f.Teams.Home.Logo,
                                LastSeenAt = DateTime.UtcNow
                            },
                            new TeamIdentity
                            {
                                Provider = "ApiFootball",
                                ProviderTeamId = f.Teams.Away.Id,
                                TeamName = f.Teams.Away.Name,
                                TeamNameNormalized = NormalizeTeamName(f.Teams.Away.Name),
                                LogoUrl = f.Teams.Away.Logo,
                                LastSeenAt = DateTime.UtcNow
                            }
                        })
                        .Where(x => x.ProviderTeamId > 0 && !string.IsNullOrWhiteSpace(x.TeamName))
                        .GroupBy(x => new { x.Provider, x.ProviderTeamId })
                        .Select(g => g.Last())
                        .ToList();

                    if (teamIdentities.Count > 0)
                    {
                        await repo.UpsertTeamIdentitiesAsync(teamIdentities);
                    }

                    var fixtureIds = allFixtures.Select(f => f.Fixture.Id.ToString()).ToList();
                    var dbEventsByExternalId = (await repo.GetByExternalIdsAsync(fixtureIds)).ToDictionary(e => e.ExternalId);
                    var activeEvents = (await repo.GetActiveEventsAsync()).ToList();

                    var updatedEvents = new List<SportEvent>();

                    foreach (var fixture in allFixtures)
                    {
                        dbEventsByExternalId.TryGetValue(fixture.Fixture.Id.ToString(), out var dbEvent);
                        if (dbEvent == null)
                        {
                            dbEvent = TryMatchByTeams(activeEvents, fixture.Teams.Home.Name, fixture.Teams.Away.Name);
                        }

                        if (dbEvent == null)
                        {
                            dbEvent = new SportEvent
                            {
                                ExternalId = fixture.Fixture.Id.ToString(),
                                HomeTeam = fixture.Teams.Home.Name,
                                AwayTeam = fixture.Teams.Away.Name,
                                EventDate = fixture.Fixture.Date,
                                SportKey = "soccer_api_football",
                                HomeWinOdds = 1.0,
                                DrawOdds = 1.0,
                                AwayWinOdds = 1.0,
                                HomeScore = fixture.Goals.Home,
                                AwayScore = fixture.Goals.Away,
                                MatchMinute = fixture.Fixture.Status.Elapsed,
                                MatchStatus = fixture.Fixture.Status.Short,
                                LastUpdated = DateTime.UtcNow
                            };
                            updatedEvents.Add(dbEvent);
                            continue;
                        }

                        if (dbEvent != null)
                        {
                            if (dbEvent.ExternalId != fixture.Fixture.Id.ToString() ||
                                dbEvent.HomeScore != fixture.Goals.Home || 
                                dbEvent.AwayScore != fixture.Goals.Away ||
                                dbEvent.MatchMinute != fixture.Fixture.Status.Elapsed ||
                                dbEvent.MatchStatus != fixture.Fixture.Status.Short)
                            {
                                dbEvent.ExternalId = fixture.Fixture.Id.ToString();
                                dbEvent.HomeScore = fixture.Goals.Home;
                                dbEvent.AwayScore = fixture.Goals.Away;
                                dbEvent.MatchMinute = fixture.Fixture.Status.Elapsed;
                                dbEvent.MatchStatus = fixture.Fixture.Status.Short;
                                dbEvent.LastUpdated = DateTime.UtcNow;

                                updatedEvents.Add(dbEvent);
                            }
                        }
                    }

                    if (updatedEvents.Any())
                    {
                        await repo.UpsertEventsAsync(updatedEvents);
                        logger.LogInformation("Updated {Count} live matches in DB.", updatedEvents.Count);

                        var dtos = mapper.Map<List<SportEventDto>>(updatedEvents);
                        await notificationService.BroadcastMatchStateUpdatedAsync(dtos);
                        await betService.SettleByLiveEventsAsync(updatedEvents.Select(e => e.ExternalId), stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in LiveMatchUpdateWorker");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }

    private static string NormalizeTeamName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.ToLowerInvariant().Trim();
        normalized = normalized.Replace("utd", "united");
        normalized = normalized.Replace("football club", " ");
        normalized = normalized.Replace("fc", " ");
        normalized = normalized.Replace("cf", " ");
        normalized = new string(normalized.Where(char.IsLetterOrDigit).ToArray());
        return normalized;
    }

    private static string BuildTeamsKey(string home, string away)
    {
        return $"{NormalizeTeamName(home)}|{NormalizeTeamName(away)}";
    }

    private static SportEvent? TryMatchByTeams(IReadOnlyList<SportEvent> candidates, string fixtureHome, string fixtureAway)
    {
        var fixtureHomeNormalized = NormalizeTeamName(fixtureHome);
        var fixtureAwayNormalized = NormalizeTeamName(fixtureAway);
        var directKey = BuildTeamsKey(fixtureHome, fixtureAway);
        var reverseKey = BuildTeamsKey(fixtureAway, fixtureHome);

        var exact = candidates.FirstOrDefault(e =>
            BuildTeamsKey(e.HomeTeam, e.AwayTeam) == directKey ||
            BuildTeamsKey(e.HomeTeam, e.AwayTeam) == reverseKey);
        if (exact != null)
        {
            return exact;
        }

        return candidates.FirstOrDefault(e =>
        {
            var eventHome = NormalizeTeamName(e.HomeTeam);
            var eventAway = NormalizeTeamName(e.AwayTeam);

            var directSimilar = TeamSimilar(eventHome, fixtureHomeNormalized) && TeamSimilar(eventAway, fixtureAwayNormalized);
            var reverseSimilar = TeamSimilar(eventHome, fixtureAwayNormalized) && TeamSimilar(eventAway, fixtureHomeNormalized);
            return directSimilar || reverseSimilar;
        });
    }

    private static bool TeamSimilar(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        return left == right || left.Contains(right) || right.Contains(left);
    }
}