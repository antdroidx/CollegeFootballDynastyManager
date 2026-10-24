using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;

namespace DynastyManager.Core.Tests;

public class GameSimulationBoxScoreTests
{
    [Fact]
    public void SimulationProducesDeterministicBoxScoreStats()
    {
        var home = Team("Home", 80);
        var away = Team("Away", 75);
        var game = new ScheduledGame
        {
            GameId = "box-score",
            SeasonYear = 2026,
            Week = 1,
            HomeTeamName = home.Name,
            AwayTeamName = away.Name,
            SimulationSeed = 20260929
        };

        var homeProfile = Profile(home.Name, 84, 82, 80, 81, 79);
        var awayProfile = Profile(away.Name, 77, 78, 76, 75, 77);

        var first = DeterministicGameSimulator.Simulate(
            game,
            home,
            away,
            homeProfile,
            awayProfile);

        var second = DeterministicGameSimulator.Simulate(
            game,
            home,
            away,
            homeProfile,
            awayProfile);

        Assert.Equivalent(first, second, strict: true);
        Assert.True(first.HasPlayed);
        Assert.NotEqual(first.HomeScore, first.AwayScore);

        Assert.NotNull(first.HomeStats);
        Assert.NotNull(first.AwayStats);
        Assert.Equal(12, first.HomeStats.Possessions);
        Assert.Equal(12, first.AwayStats.Possessions);
        Assert.True(first.HomeStats.TotalYards > 0);
        Assert.True(first.AwayStats.TotalYards > 0);
        Assert.True(first.HomeStats.PassAttempts > 0);
        Assert.True(first.HomeStats.RushAttempts > 0);
        Assert.InRange(first.HomeStats.PassCompletions, 0, first.HomeStats.PassAttempts);
        Assert.Equal(first.HomeStats.Turnovers,
            first.HomeStats.InterceptionsThrown + first.HomeStats.FumblesLost);
        Assert.InRange(first.HomeStats.FieldGoalsMade, 0, first.HomeStats.FieldGoalsAttempted);
        Assert.InRange(first.HomeStats.ExtraPointsMade, 0, first.HomeStats.ExtraPointsAttempted);
        Assert.True(first.HomeStats.PuntYards >= first.HomeStats.LongPunt);
        Assert.True(first.HomeStats.KickReturns >= 0);
        Assert.True(first.HomeStats.PuntReturns >= 0);
    }

    [Fact]
    public void SimulationPopulatesExpandedStatCategoriesAcrossGames()
    {
        var home = Team("Home", 80);
        var away = Team("Away", 75);
        var homeProfile = Profile(home.Name, 84, 82, 80, 81, 79);
        var awayProfile = Profile(away.Name, 77, 78, 76, 75, 77);

        var totals = Enumerable.Range(1, 24).Select(seed => DeterministicGameSimulator.Simulate(
            new ScheduledGame
            {
                GameId = $"expanded-{seed}", SeasonYear = 2026, Week = seed,
                HomeTeamName = home.Name, AwayTeamName = away.Name, SimulationSeed = seed * 7919
            }, home, away, homeProfile, awayProfile).HomeStats!).ToArray();

        Assert.True(totals.Sum(stat => stat.PassCompletions) > 0);
        Assert.True(totals.Sum(stat => stat.PassingTouchdowns + stat.RushingTouchdowns) > 0);
        Assert.True(totals.Sum(stat => stat.ExplosivePasses20 + stat.ExplosiveRuns20) > 0);
        Assert.True(totals.Sum(stat => stat.FieldGoalsAttempted) > 0);
        Assert.True(totals.Sum(stat => stat.Punts) > 0);
        Assert.True(totals.Sum(stat => stat.KickReturns + stat.PuntReturns) > 0);
    }

    private static Team Team(string name, int prestige) =>
        new()
        {
            Name = name,
            Abbreviation = name,
            ConferenceName = "Test",
            Prestige = prestige
        };

    private static TeamSimulationProfile Profile(
        string name,
        double passOffense,
        double rushOffense,
        double passDefense,
        double rushDefense,
        double specialTeams) =>
        new()
        {
            TeamName = name,
            PassOffenseRating = passOffense,
            RushOffenseRating = rushOffense,
            PassDefenseRating = passDefense,
            RushDefenseRating = rushDefense,
            SpecialTeamsRating = specialTeams,
            RosterSize = 85
        };
}
