using DynastyManager.Core.Simulation;
using Xunit;
namespace DynastyManager.Core.Tests;
public sealed class BlockingPlaybackTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void BlockedDefenderStaysInFrontOfMovingBlockerThroughoutPlay(int direction)
    {
        var start = new GameFieldPoint(.5 + direction * .02, .4);
        var blockStart = new GameFieldPoint(.5, .4); var blockEnd = new GameFieldPoint(.5 - direction * .03, .42);
        var pocket = new GameFieldPoint(.5 - direction * .08, .5);
        var points = Enumerable.Range(0, 101).Select(i => GameDayAnimationPlanner.BlockedRusherPoint(start, blockStart, blockEnd, pocket, direction, i / 100d, 1)).ToArray();
        for (var i = 0; i <= 100; i++)
            Assert.True(direction * (points[i].X - (blockStart.X + (blockEnd.X - blockStart.X) * i / 100d)) > 0);
        Assert.True(points.Select(p => p.X).Distinct().Count() > 90);
        Assert.DoesNotContain(points, p => Math.Abs(p.X - pocket.X) < .01);
    }
    [Theory]
    [InlineData(.32, -.06)] // Recorded sack.
    [InlineData(.42, -.02)] // Recorded tackle for loss.
    [InlineData(.78, .25)] // Pursuit only after a completed downfield pass.
    public void RecordedPursuerEngagesBeforeReleaseAndReachesTheRecordedContact(double release, double depth)
    {
        var start = new GameFieldPoint(.52, .4); var block = new GameFieldPoint(.5, .4); var finish = new GameFieldPoint(.5 + depth, .6);
        var before = GameDayAnimationPlanner.BlockedRusherPoint(start, block, block, finish, 1, release - .001, release);
        Assert.True(before.X > block.X);
        var at = GameDayAnimationPlanner.BlockedRusherPoint(start, block, block, finish, 1, release, release);
        var after = GameDayAnimationPlanner.BlockedRusherPoint(start, block, block, finish, 1, release + .000001, release);
        Assert.True(GameDayAnimationPlanner.DistanceYards(at, after) < .001);
        Assert.Equal(finish, GameDayAnimationPlanner.BlockedRusherPoint(start, block, block, finish, 1, 1, release));
    }
    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void BreakthroughMovesAroundTheBlockersShoulderBeforeCrossingTheLine(int direction)
    {
        var start = new GameFieldPoint(.5 + direction * .02, .4); var blocker = new GameFieldPoint(.5, .4);
        var finish = new GameFieldPoint(.5 - direction * .07, .5);
        var shoulder = GameDayAnimationPlanner.BlockedRusherPoint(start, blocker, blocker, finish, direction, .49, .32);
        Assert.True(direction * (shoulder.X - blocker.X) > 0);
        Assert.True(Math.Abs(shoulder.Y - blocker.Y) >= .075);
        var behind = GameDayAnimationPlanner.BlockedRusherPoint(start, blocker, blocker, finish, direction, .66, .32);
        Assert.True(direction * (behind.X - blocker.X) < 0);
        Assert.True(Math.Abs(behind.Y - blocker.Y) >= .075);
    }
    [Theory]
    [InlineData(0, -1, 0)]
    [InlineData(1, -1, 45)]
    [InlineData(1, 0, 90)]
    [InlineData(1, 1, 135)]
    [InlineData(0, 1, 180)]
    [InlineData(-1, 1, 225)]
    [InlineData(-1, 0, 270)]
    [InlineData(-1, -1, 315)]
    public void SpriteFacesAllEightMovementDirections(double dx, double dy, double heading) =>
        Assert.Equal(heading, GameDayAnimationPlanner.SpriteHeading(dx, dy, 0));
    [Fact]
    public void StationaryPlayerKeepsHeadingAndRouteCutsChangeItImmediately()
    {
        Assert.Equal(135, GameDayAnimationPlanner.SpriteHeading(0, 0, 135));
        var upfield = GameDayAnimationPlanner.SpriteHeading(4, 0, 0);
        Assert.Equal(180, GameDayAnimationPlanner.SpriteHeading(0, 2, upfield));
    }
}
