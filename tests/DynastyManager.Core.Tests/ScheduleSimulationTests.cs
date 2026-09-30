using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;

namespace DynastyManager.Core.Tests;

public class ScheduleSimulationTests
{
    private static readonly Guid DynastyId =
        Guid.Parse("11111111-2222-3333-4444-555555555555");

    [Fact]
    public void DefaultScheduleHasTwelveGamesPerTeamAndSmallWeekZeroSlate()
    {
        var teams = CreateTeams(14);
        var schedule = SeasonScheduleBuilder.BuildRegularSeason(
            teams,
            DynastyId,
            2026);

        Assert.Equal(84, schedule.Count);
        Assert.Equal(5, schedule.Count(game => game.Week == 0));

        foreach (var team in teams)
        {
            var teamSchedule = schedule
                .Where(game => game.InvolvesTeam(team.Name))
                .ToArray();

            Assert.Equal(12, teamSchedule.Length);
            Assert.Equal(
                teamSchedule.Length,
                teamSchedule
                    .Select(game => game.Week)
                    .Distinct()
                    .Count());
            Assert.Equal(
                6,
                teamSchedule.Count(game =>
                    game.HomeTeamName.Equals(
                        team.Name,
                        StringComparison.OrdinalIgnoreCase)));
        }
    }

    [Fact]
    public void ScheduleBuilderCreatesOneGamePerTeamPerWeek()
    {
        var teams = CreateTeams(8);
        var schedule = SeasonScheduleBuilder.BuildRegularSeason(
            teams,
            DynastyId,
            2026,
            weeks: 3,
            gamesPerTeam: 3,
            weekZeroGames: 0);

        Assert.Equal(12, schedule.Count);

        for (var week = 1; week <= 3; week++)
        {
            var games = schedule.Where(game => game.Week == week).ToArray();
            var teamNames = games
                .SelectMany(game => new[]
                {
                    game.HomeTeamName,
                    game.AwayTeamName
                })
                .ToArray();

            Assert.Equal(
                teamNames.Length,
                teamNames
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count());
        }

        foreach (var team in teams)
            Assert.Equal(3, schedule.Count(game => game.InvolvesTeam(team.Name)));
    }

    [Fact]
    public void ScheduleBuilderIsDeterministic()
    {
        var teams = CreateTeams(10);

        var first = SeasonScheduleBuilder.BuildRegularSeason(
            teams,
            DynastyId,
            2030,
            weeks: 5,
            gamesPerTeam: 5,
            weekZeroGames: 0);

        var second = SeasonScheduleBuilder.BuildRegularSeason(
            teams,
            DynastyId,
            2030,
            weeks: 5,
            gamesPerTeam: 5,
            weekZeroGames: 0);

        Assert.Equal(first, second);
    }

    [Fact]
    public void GameSimulationIsDeterministicAndCannotTie()
    {
        var home = CreateTeam("Home", 82, "Home Conference");
        var away = CreateTeam("Away", 78, "Away Conference");
        var game = new ScheduledGame
        {
            GameId = "test",
            SeasonYear = 2026,
            Week = 1,
            HomeTeamName = home.Name,
            AwayTeamName = away.Name,
            SimulationSeed = 123456
        };

        var first = DeterministicGameSimulator.Simulate(game, home, away);
        var second = DeterministicGameSimulator.Simulate(game, home, away);

        Assert.Equal(first, second);
        Assert.True(first.HasPlayed);
        Assert.NotNull(first.HomeScore);
        Assert.NotNull(first.AwayScore);
        Assert.NotEqual(first.HomeScore, first.AwayScore);
    }

    [Fact]
    public void WeekSimulationProcessesWeekZero()
    {
        var teams = CreateTeams(4);
        var lookup = teams.ToDictionary(
            team => team.Name,
            StringComparer.OrdinalIgnoreCase);

        var schedule = SeasonScheduleBuilder.BuildRegularSeason(
            teams,
            DynastyId,
            2026,
            weeks: 2,
            gamesPerTeam: 2,
            weekZeroGames: 1);

        var state = new DynastyState
        {
            DynastyId = DynastyId,
            DynastyName = "Test Dynasty",
            UserTeamName = teams[0].Name,
            SeasonYear = 2026,
            Week = 0,
            Phase = SeasonPhase.RegularSeason,
            Schedule = schedule
        };

        var simulated = WeekSimulation.SimulateCurrentRegularSeasonWeek(
            state,
            lookup);

        Assert.All(
            simulated.Schedule.Where(game => game.Week == 0),
            game => Assert.True(game.HasPlayed));

        Assert.All(
            simulated.Schedule.Where(game => game.Week > 0),
            game => Assert.False(game.HasPlayed));
    }

    private static Team[] CreateTeams(int count) =>
        Enumerable.Range(1, count)
            .Select(index => CreateTeam(
                $"Team {index:D2}",
                55 + index,
                $"Conference {index:D2}"))
            .ToArray();

    private static Team CreateTeam(
        string name,
        int prestige,
        string conference) =>
        new()
        {
            Name = name,
            Abbreviation = name.Replace("Team ", "T"),
            ConferenceName = conference,
            Prestige = prestige
        };
}
