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

        Assert.Equal(first, second);
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
