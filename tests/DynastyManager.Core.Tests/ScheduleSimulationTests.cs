using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;

namespace DynastyManager.Core.Tests;

public class ScheduleSimulationTests
{
    private static readonly Guid DynastyId =
        Guid.Parse("11111111-2222-3333-4444-555555555555");

    [Fact]
    public void DefaultScheduleHasTwelveGamesPerTeam()
    {
        var teams = CreateTeams(14);
        var schedule = SeasonScheduleBuilder.BuildRegularSeason(
            teams,
            DynastyId,
            2026);

        Assert.Equal(84, schedule.Count);
        Assert.All(
            teams,
            team => Assert.Equal(
                12,
                schedule.Count(game => game.InvolvesTeam(team.Name))));
        Assert.Equal(12, schedule.Max(game => game.Week));
        Assert.Empty(schedule.Where(game => game.Week == 13));
    }

    [Fact]
    public void ScheduleBuilderCreatesOneGamePerTeamPerWeek()
    {
        var teams = CreateTeams(8);
        var schedule = SeasonScheduleBuilder.BuildRegularSeason(
            teams,
            DynastyId,
            2026,
            weeks: 3);

        Assert.Equal(12, schedule.Count);

        for (var week = 1; week <= 3; week++)
        {
            var games = schedule.Where(game => game.Week == week).ToArray();
            Assert.Equal(4, games.Length);

            var teamNames = games
                .SelectMany(game => new[] { game.HomeTeamName, game.AwayTeamName })
                .ToArray();

            Assert.Equal(8, teamNames.Distinct(StringComparer.OrdinalIgnoreCase).Count());
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
            weeks: 5);

        var second = SeasonScheduleBuilder.BuildRegularSeason(
            teams,
            DynastyId,
            2030,
            weeks: 5);

        Assert.Equal(first, second);
    }

    [Fact]
    public void GameSimulationIsDeterministicAndCannotTie()
    {
        var home = CreateTeam("Home", 82);
        var away = CreateTeam("Away", 78);
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
    public void WeekSimulationOnlyProcessesCurrentWeek()
    {
        var teams = CreateTeams(4);
        var lookup = teams.ToDictionary(team => team.Name, StringComparer.OrdinalIgnoreCase);
        var schedule = SeasonScheduleBuilder.BuildRegularSeason(
            teams,
            DynastyId,
            2026,
            weeks: 2);

        var state = new DynastyState
        {
            DynastyId = DynastyId,
            DynastyName = "Test Dynasty",
            UserTeamName = teams[0].Name,
            SeasonYear = 2026,
            Week = 1,
            Phase = SeasonPhase.RegularSeason,
            Schedule = schedule
        };

        var simulated = WeekSimulation.SimulateCurrentRegularSeasonWeek(state, lookup);

        Assert.All(simulated.Schedule.Where(game => game.Week == 1), game => Assert.True(game.HasPlayed));
        Assert.All(simulated.Schedule.Where(game => game.Week == 2), game => Assert.False(game.HasPlayed));
    }

    private static Team[] CreateTeams(int count) =>
        Enumerable.Range(1, count)
            .Select(index => CreateTeam($"Team {index:D2}", 55 + index))
            .ToArray();

    private static Team CreateTeam(string name, int prestige) =>
        new()
        {
            Name = name,
            Abbreviation = name.Replace("Team ", "T"),
            ConferenceName = "Test",
            Prestige = prestige
        };
}
