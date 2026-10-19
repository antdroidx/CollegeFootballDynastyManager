using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;

namespace DynastyManager.Core.Tests;

public class GameDayPresentationTests
{
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
        var release = GameDayAnimationPlanner.RoutePoint(start, meshEnd, GameDayRouteFamily.Mesh, .25);
        var cross = GameDayAnimationPlanner.RoutePoint(start, meshEnd, GameDayRouteFamily.Mesh, .85);
        Assert.Equal(start.Y, release.Y);
        Assert.True(cross.Y > .50);
        Assert.Equal(start.Y, GameDayAnimationPlanner.RouteEndLane(
            GameDayAnimationPlanner.RouteFamily(verticals), Position.WR, 0, start.Y, false));
        Assert.True(GameDayAnimationPlanner.PassReleaseProgress(verticals) >
            GameDayAnimationPlanner.PassReleaseProgress(mesh));
    }
}
