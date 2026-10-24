using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;

namespace DynastyManager.Core.Tests;

public sealed class PassRushAnimationTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void DeepPassRushMovesTowardQuarterbackUntilCatchThenPursues(int direction)
    {
        var start = new GameFieldPoint(.5 + direction / 120d, .35);
        var pocket = new GameFieldPoint(.5 - direction * 5 / 120d, .43);
        var contact = new GameFieldPoint(.5 + direction * 30 / 120d, .25);
        const double caught = .78;
        var prior = start;
        for (var frame = 0; frame <= 78; frame++)
        {
            var point = GameDayAnimationPlanner.PassRushPoint(start, pocket, contact, frame / 100d, caught);
            Assert.True(direction * (point.X - prior.X) <= .000001);
            prior = point;
        }
        Assert.Equal(pocket, GameDayAnimationPlanner.PassRushPoint(start, pocket, contact, caught, caught));
        Assert.Equal(contact, GameDayAnimationPlanner.PassRushPoint(start, pocket, contact, 1, caught));
        var after = GameDayAnimationPlanner.PassRushPoint(start, pocket, contact, .9, caught);
        Assert.True(direction * (after.X - pocket.X) > 0);
    }

    [Fact]
    public void BlockedRusherStaysAtPocketOnIncompletePassInsteadOfDroppingDownfield()
    {
        var start = new GameFieldPoint(.51, .65); var pocket = new GameFieldPoint(.46, .58);
        foreach (var progress in new[] { .75, .85, 1.0 })
            Assert.Equal(pocket, GameDayAnimationPlanner.PassRushPoint(start, pocket, pocket, progress, .75));
    }

    [Fact]
    public void RushAssignmentUsesExactPositionAndRoleAndHonorsCoverageOverrides()
    {
        var play = new DefensivePlayDefinition("custom", "Zone exchange", DefensiveFormation.FourThree,
            DefensivePersonnel.BaseFourThree, DefensiveApproach.Pressure, "")
        { Assignments = [new() { Position = Position.DE, RoleIndex = 0, Task = AssignmentTask.Zone },
            new() { Position = Position.OLB, RoleIndex = 0, Task = AssignmentTask.Man },
            new() { Position = Position.CB, RoleIndex = 1, Task = AssignmentTask.Rush }] };
        Assert.False(DefensivePursuitService.IsPassRusher(play, Position.DE, 0));
        Assert.True(DefensivePursuitService.IsPassRusher(play, Position.DE, 1));
        Assert.True(DefensivePursuitService.IsPassRusher(play, Position.DT, 0));
        Assert.False(DefensivePursuitService.IsPassRusher(play, Position.OLB, 0));
        Assert.True(DefensivePursuitService.IsPassRusher(play, Position.OLB, 1));
        Assert.False(DefensivePursuitService.IsPassRusher(play, Position.CB, 0));
        Assert.True(DefensivePursuitService.IsPassRusher(play, Position.CB, 1));
        Assert.True(DefensivePursuitService.IsPassRusher(null, Position.DT, 0));
        Assert.False(DefensivePursuitService.IsPassRusher(null, Position.MLB, 0));
    }
}
