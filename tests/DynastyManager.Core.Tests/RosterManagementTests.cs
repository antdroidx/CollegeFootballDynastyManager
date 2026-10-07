using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;

namespace DynastyManager.Core.Tests;

public class RosterManagementTests
{
    [Fact]
    public void NormalizeDepthChartOrdersPlayersByRatingAndIdentifiesStarter()
    {
        var state = State(new[]
        {
            Player("QB Three", Position.QB, 78),
            Player("QB One", Position.QB, 90),
            Player("QB Four", Position.QB, 72),
            Player("QB Two", Position.QB, 84)
        });

        state = RosterManagementService.NormalizeAllDepthCharts(state);

        var qbs = state.ActiveRoster
            .Where(player => player.Position == Position.QB)
            .OrderBy(player => player.DepthChartOrder)
            .ToArray();

        Assert.Equal(
            new[] { "QB One", "QB Two", "QB Three", "QB Four" },
            qbs.Select(player => player.FullName));

        Assert.True(RosterManagementService.IsStarter(qbs[0]));
        Assert.False(RosterManagementService.IsStarter(qbs[1]));
    }

    [Fact]
    public void MovePlayerChangesDepthOrderWithinPosition()
    {
        var first = Player("First", Position.RB, 90);
        var second = Player("Second", Position.RB, 85);
        var third = Player("Third", Position.RB, 80);

        var state = RosterManagementService.NormalizeAllDepthCharts(
            State(new[] { first, second, third }));

        state = RosterManagementService.MovePlayer(
            state,
            "Test",
            third.PlayerId,
            -1);

        var rbs = state.ActiveRoster
            .Where(player => player.Position == Position.RB)
            .OrderBy(player => player.DepthChartOrder)
            .Select(player => player.FullName)
            .ToArray();

        Assert.Equal(
            new[] { "First", "Third", "Second" },
            rbs);
    }

    [Fact]
    public void RedshirtRemovesPlayerFromPlayableDepthAndCanOnlyBeUsedOnce()
    {
        var first = Player("QB One", Position.QB, 90);
        var second = Player("QB Two", Position.QB, 84);
        var third = Player("QB Three", Position.QB, 80);

        var state = RosterManagementService.NormalizeAllDepthCharts(
            State(new[] { first, second, third }));

        state = RosterManagementService.ToggleRedshirt(
            state,
            "Test",
            first.PlayerId);

        var redshirt = state.ActiveRoster.Single(player =>
            player.PlayerId == first.PlayerId);

        Assert.True(redshirt.IsRedshirted);
        Assert.False(RosterManagementService.IsStarter(redshirt));

        var warning = Assert.Single(
            RosterManagementService.GetPositionWarnings(
                state,
                "Test"),
            value => value.StartsWith("QB:", StringComparison.Ordinal));

        Assert.Contains("2/3", warning);

        var used = state with
        {
            ActiveRoster = state.ActiveRoster
                .Select(player =>
                    player.PlayerId == first.PlayerId
                        ? player with
                        {
                            IsRedshirted = false,
                            HasRedshirted = true
                        }
                        : player)
                .ToArray()
        };

        var unchanged = RosterManagementService.ToggleRedshirt(
            used,
            "Test",
            first.PlayerId);

        Assert.False(
            unchanged.ActiveRoster.Single(player =>
                player.PlayerId == first.PlayerId).IsRedshirted);
    }

    [Fact]
    public void ReleasePlayerRemovesPlayerDuringRosterManagement()
    {
        var player = Player("Cut Me", Position.WR, 70);
        var state = State(new[] { player });

        state = RosterManagementService.ReleasePlayer(
            state,
            "Test",
            player.PlayerId);

        Assert.DoesNotContain(
            state.ActiveRoster,
            candidate => candidate.PlayerId == player.PlayerId);
    }

    private static DynastyState State(
        IReadOnlyList<DynastyPlayer> players) =>
        new()
        {
            DynastyName = "Roster Test",
            UserTeamName = "Test",
            SeasonYear = 2027,
            Week = 21,
            Phase = SeasonPhase.RosterManagement,
            ActiveRoster = players
        };

    private static DynastyPlayer Player(
        string name,
        Position position,
        int overall) =>
        new()
        {
            PlayerId = Guid.NewGuid(),
            FullName = name,
            TeamName = "Test",
            Position = position,
            ClassYear = 2,
            TalentLevel = 6,
            OverallRating = overall
        };
}
