using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;

namespace DynastyManager.Core.Tests;

public class GameDayPresentationTests
{
    [Fact]
    public void SinglebackAndPistolPutTheBackDirectlyBehindTheQuarterback()
    {
        foreach (var formation in new[] { OffensiveFormation.Singleback, OffensiveFormation.Pistol })
        {
            var qb = GameDayAnimationPlanner.Alignment(formation, Position.QB, 0);
            var back = GameDayAnimationPlanner.Alignment(formation, Position.RB, 0);
            Assert.Equal(qb.Y, back.Y);
            Assert.True(back.X < qb.X);
        }
        var flexbone = Enumerable.Range(0, 3).Select(i => GameDayAnimationPlanner.Alignment(OffensiveFormation.Flexbone, Position.RB, i)).ToArray();
        Assert.Equal(.50, flexbone[0].Y);
        Assert.True(flexbone[1].Y < .50 && flexbone[2].Y > .50);
    }

    [Fact]
    public void BallHighlightTransfersOnlyAtHandoffAndCatch()
    {
        var qb = Guid.NewGuid(); var back = Guid.NewGuid(); var receiver = Guid.NewGuid();
        var run = new GameSnapEvent { OffenseTeamName = "H", DefenseTeamName = "A", OffensivePlayId = "off-spread-zone",
            DefensivePlayId = "def-43-cover3", QuarterbackId = qb, BallCarrierId = back, Outcome = SnapOutcome.Run };
        Assert.Equal(qb, GameDayAnimationPlanner.BallOwner(run, .10, .43));
        Assert.Equal(back, GameDayAnimationPlanner.BallOwner(run, .26, .43));
        var pass = run with { BallCarrierId = null, TargetId = receiver, Outcome = SnapOutcome.Completion };
        Assert.Equal(qb, GameDayAnimationPlanner.BallOwner(pass, .30, .43));
        Assert.Null(GameDayAnimationPlanner.BallOwner(pass, .50, .43));
        Assert.Equal(receiver, GameDayAnimationPlanner.BallOwner(pass, .70, .43));
    }

    [Fact]
    public void PlaybackDurationAccountsForTheReturnersShortActiveWindow()
    {
        var full = GameDayAnimationPlanner.PlaybackDurationSeconds(new[] { (30d, 80, 1d) });
        var returning = GameDayAnimationPlanner.PlaybackDurationSeconds(new[] { (30d, 80, .35) });
        Assert.True(returning > full);
        Assert.True(30 / (returning * .35) <= (6 + 80 / 20d) * 2.5);
    }
    [Fact]
    public void FieldContainsOneHundredYardsBetweenTwoTenYardEndZones()
    {
        var leftGoal = GameDayAnimationPlanner.ProjectYardLine(0, 1);
        var rightGoal = GameDayAnimationPlanner.ProjectYardLine(100, 1);
        var tenYards = GameDayAnimationPlanner.ProjectYardLine(10, 1) - leftGoal;
        Assert.Equal(tenYards, leftGoal, 10);
        Assert.Equal(tenYards, 1 - rightGoal, 10);
        Assert.Equal(.5, GameDayAnimationPlanner.ProjectYardLine(50, 1));
        Assert.Equal(rightGoal, GameDayAnimationPlanner.ProjectYardLine(0, -1), 10);
        Assert.Equal(leftGoal, GameDayAnimationPlanner.ProjectYardLine(100, -1), 10);
    }

    [Fact]
    public void ReturnerWaitsForTheCatchBeforeFollowingTheBallReturnTimeline()
    {
        Assert.Equal(0, GameDayAnimationPlanner.ReturnProgress(.40));
        Assert.Equal(0, GameDayAnimationPlanner.ReturnProgress(.65));
        Assert.Equal(.5, GameDayAnimationPlanner.ReturnProgress(.825), 10);
        Assert.Equal(1, GameDayAnimationPlanner.ReturnProgress(1));
    }

    [Fact]
    public void MeshCrossesAfterReleaseWhileVerticalsStayInTheirLane()
    {
        var mesh = PlaybookCatalog.OffensivePlays.Single(play => play.Id == "off-spread-mesh");
        var verticals = PlaybookCatalog.OffensivePlays.Single(play => play.Id == "off-spread-verts");
        var start = new GameFieldPoint(.40, .10);
        var meshEnd = new GameFieldPoint(.55, GameDayAnimationPlanner.RouteEndLane(
            GameDayAnimationPlanner.RouteFamily(mesh), Position.WR, 0, start.Y, false));
        var release = GameDayAnimationPlanner.RoutePoint(start, meshEnd, GameDayRouteFamily.Mesh, .05);
        var cross = GameDayAnimationPlanner.RoutePoint(start, meshEnd, GameDayRouteFamily.Mesh, .85);
        Assert.Equal(start.Y, release.Y);
        Assert.True(cross.Y > .50);
        Assert.Equal(start.Y, GameDayAnimationPlanner.RouteEndLane(
            GameDayAnimationPlanner.RouteFamily(verticals), Position.WR, 0, start.Y, false));
        Assert.True(GameDayAnimationPlanner.PassReleaseProgress(verticals) >
            GameDayAnimationPlanner.PassReleaseProgress(mesh));
    }
}
