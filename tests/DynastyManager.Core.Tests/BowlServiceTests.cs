using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;

namespace DynastyManager.Core.Tests;

public class BowlServiceTests
{
    [Fact]
    public void BowlsUseEligibleNonPlayoffTeamsOnly()
    {
        var teams = new List<Team>();
        var schedule = new List<ScheduledGame>();

        for (var index = 1; index <= 20; index++)
        {
            var winner = Team($"Eligible {index:D2}", 80 - index);
            var loser = Team($"Filler {index:D2}", 40);
            teams.Add(winner);
            teams.Add(loser);

            for (var game = 1; game <= 6; game++)
            {
                schedule.Add(new ScheduledGame
                {
                    GameId = $"E{index}-{game}",
                    SeasonYear = 2026,
                    Week = game,
                    HomeTeamName = winner.Name,
                    AwayTeamName = loser.Name,
                    GameType = ScheduledGameType.NonConference,
                    SimulationSeed = index * 100 + game,
                    HasPlayed = true,
                    HomeScore = 28,
                    AwayScore = 14
                });
            }
        }

        var playoffTeams = Enumerable.Range(1, 12)
            .Select(seed => new CollegeFootballPlayoffSeedRecord
            {
                SeasonYear = 2026,
                Seed = seed,
                NationalRank = seed,
                TeamName = $"Eligible {seed:D2}",
                ConferenceName = "Test"
            })
            .ToArray();

        var state = new DynastyState
        {
            DynastyName = "Bowl Test",
            UserTeamName = "Eligible 20",
            SeasonYear = 2026,
            Week = BowlService.FirstBowlWeek,
            Phase = SeasonPhase.Postseason,
            Schedule = schedule,
            CollegeFootballPlayoffHistory = playoffTeams
        };

        var lookup = teams.ToDictionary(
            team => team.Name,
            StringComparer.OrdinalIgnoreCase);

        state = BowlService.InitializeBowls(state, lookup);

        var bowls = state.Schedule
            .Where(game => game.GameType == ScheduledGameType.Bowl)
            .ToArray();

        Assert.Equal(4, bowls.Length);
        Assert.All(bowls, game => Assert.True(game.IsNeutralSite));
        Assert.All(
            bowls,
            game => Assert.InRange(
                game.Week,
                BowlService.FirstBowlWeek,
                BowlService.LastBowlWeek));

        var playoffNames = playoffTeams
            .Select(team => team.TeamName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.All(
            bowls.SelectMany(game => new[]
            {
                game.HomeTeamName,
                game.AwayTeamName
            }),
            name => Assert.DoesNotContain(name, playoffNames));
    }

    [Fact]
    public void SimulatedBowlCreatesPersistentResultHistory()
    {
        var home = Team("Home", 80);
        var away = Team("Away", 75);
        var lookup = new[] { home, away }.ToDictionary(
            team => team.Name,
            StringComparer.OrdinalIgnoreCase);

        var state = new DynastyState
        {
            DynastyName = "Bowl Test",
            UserTeamName = home.Name,
            SeasonYear = 2026,
            Week = BowlService.FirstBowlWeek,
            Phase = SeasonPhase.Postseason,
            Schedule = new[]
            {
                new ScheduledGame
                {
                    GameId = "bowl-1",
                    SeasonYear = 2026,
                    Week = BowlService.FirstBowlWeek,
                    HomeTeamName = home.Name,
                    AwayTeamName = away.Name,
                    GameType = ScheduledGameType.Bowl,
                    BowlName = "Carnation Bowl",
                    IsNeutralSite = true,
                    SimulationSeed = 12345
                }
            }
        };

        state = BowlService.SimulateCurrentWeek(state, lookup);

        var game = Assert.Single(
            state.Schedule,
            game => game.GameType == ScheduledGameType.Bowl);
        Assert.True(game.HasPlayed);

        var result = Assert.Single(state.BowlHistory);
        Assert.Equal("Carnation Bowl", result.BowlName);
        Assert.Contains(
            result.WinnerTeamName,
            new[] { home.Name, away.Name });
        Assert.NotEqual(result.WinnerScore, result.LoserScore);
    }

    private static Team Team(string name, int prestige) =>
        new()
        {
            Name = name,
            Abbreviation = name,
            ConferenceName = "Test",
            Prestige = prestige
        };
}
