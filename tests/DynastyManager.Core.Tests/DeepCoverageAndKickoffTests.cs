using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;
namespace DynastyManager.Core.Tests;

public sealed class DeepCoverageAndKickoffTests
{
    [Theory]
    [InlineData(1, false)][InlineData(1, true)][InlineData(-1, false)][InlineData(-1, true)]
    public void DeepCoverageContinuesDownfieldWithTheRoute(int direction, bool zone)
    {
        var start = new GameFieldPoint(GameDayAnimationPlanner.ProjectFieldYardLine(35, direction), .2);
        var mid = new GameFieldPoint(GameDayAnimationPlanner.ProjectFieldYardLine(55, direction), .35);
        var deep = new GameFieldPoint(GameDayAnimationPlanner.ProjectFieldYardLine(85, direction), .4);
        var first = LiveFootballChoreography.Coverage(start, mid, .5, zone, direction);
        var last = LiveFootballChoreography.Coverage(start, deep, 1, zone, direction);
        Assert.True((last.X - first.X) * direction > .15);
        Assert.InRange(GameDayAnimationPlanner.DistanceYards(last, deep), 0, zone ? 15 : 4);
        if (zone) Assert.True(Math.Abs(last.Y - start.Y) < Math.Abs(deep.Y - start.Y));
    }

    [Fact]
    public void ManAssignmentsOverrideCoverageNameAndZoneCallsRemainZone()
    {
        var play = PlaybookCatalog.DefensivePlays.First();
        Assert.False(LiveFootballChoreography.Zone(play with { Name = "Cover 1 Robber" }, null));
        Assert.False(LiveFootballChoreography.Zone(play with { Name = "Cover 2 Man" }, null));
        Assert.True(LiveFootballChoreography.Zone(play with { Name = "Cover 3" }, null));
        Assert.False(LiveFootballChoreography.Zone(play with { Name = "Cover 3" }, new() { Task = AssignmentTask.Man }));
        Assert.True(LiveFootballChoreography.Zone(play with { Name = "Cover 1" }, new() { Task = AssignmentTask.Zone }));
    }

    [Theory]
    [InlineData(1)][InlineData(-1)]
    public void KickoffBothUnitsHaveSpreadFormationsAndMove(int direction)
    {
        foreach (var receiving in new[] { true, false })
        {
            var tracks = Enumerable.Range(0, 11).Select(i => LiveFootballChoreography.Kickoff(receiving, i == 0, i, 5, 28, direction)).ToArray();
            Assert.True(tracks.Select(t => t.From.Y).Distinct().Count() >= 9);
            Assert.All(tracks, t => { Assert.True(GameDayAnimationPlanner.DistanceYards(t.From, t.To) > .5); Assert.InRange(t.From.X, 0, 1); Assert.InRange(t.To.X, 0, 1); });
            if (!receiving) Assert.All(tracks.Skip(1), t => Assert.True((t.To.X - t.From.X) * direction < 0));
        }
    }

    [Fact]
    public void TouchbackStaysInEndZoneVisuallyAndAwards25WithoutAReturnOrClockRunoff()
    {
        var found = false; var source = GameDayEngineTests.Session(GameDayMode.Play);
        for (var seed = 0; seed < 100 && !found; seed++)
        {
            var s = GameDayEngine.Start(source.Game with { SimulationSeed = seed }, source.HomeProfile, source.AwayProfile, source.Roster, GameDayMode.Play);
            var snap = GameDayEngine.Step(s);
            if (!snap.IsTouchback) continue;
            found = true;
            Assert.Equal(-3, LiveFootballChoreography.KickLandingYardLine(snap));
            Assert.Equal(25, s.YardLine); Assert.Equal(900, s.ClockSeconds);
            Assert.Equal(0, snap.ReturnYards); Assert.Null(snap.ReturnerId);
            var box = GameDayEngine.BoxScore(s);
            Assert.All(box.Players, p => Assert.Equal(0, p.KickReturns));
            Assert.Equal(0, box.HomeStats.KickReturns); Assert.Equal(0, box.AwayStats.KickReturns);
            Assert.Contains("TOUCHBACK", GameDayCommentaryService.Describe(snap, s.PlayersById));
            Assert.Contains("25-yard", GameDayCommentaryService.Describe(snap, s.PlayersById));
        }
        Assert.True(found);
    }

    [Fact]
    public void OutsideReceiversAreMatchedToCornersInsteadOfCyclingThroughLinebackers()
    {
        var s = GameDayEngineTests.Session(GameDayMode.Play);
        var offense = s.HomeRoster.Where(p => p.Position is Position.QB or Position.WR).Take(8).ToArray();
        var defense = s.AwayRoster.Where(p => p.Position is Position.CB or Position.MLB or Position.FS).Take(14).ToArray();
        var play = PlaybookCatalog.OffensivePlays.First(p => p.Concept == OffensiveConcept.DeepPass);
        var call = PlaybookCatalog.DefensivePlays.First();
        for (var seed = 0; seed < 25; seed++)
        {
            var read = PassingReadService.Read(play, call, offense, defense, offense.First(p => p.Position == Position.QB), 75, new Random(seed));
            Assert.NotNull(read.Defender); Assert.Equal(Position.CB, read.Defender.Position);
            Assert.InRange(read.CatchChance, .08, .88);
        }
    }
}
