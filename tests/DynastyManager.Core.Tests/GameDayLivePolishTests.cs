using System.Text.Json;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;

namespace DynastyManager.Core.Tests;

public class GameDayLivePolishTests
{
    [Theory]
    [InlineData(1, 1)] [InlineData(1, -1)] [InlineData(25, 1)] [InlineData(25, -1)]
    [InlineData(75, 1)] [InlineData(75, -1)] [InlineData(99, 1)] [InlineData(99, -1)]
    public void BetweenPlayOfficialAndBallAlignWithLineAndClearBothHuddles(int yardLine, int direction)
    {
        var ball = GameDayLivePresentation.BallSpot(yardLine, direction);
        var official = GameDayLivePresentation.SpottingOfficial(yardLine, direction);
        Assert.Equal(GameDayAnimationPlanner.ProjectYardLine(yardLine, direction), ball.X, 8);
        Assert.Equal(ball.X, official.X, 8); Assert.True(official.Y > ball.Y);
        foreach (var offense in new[] { true, false })
            for (var slot = 0; slot < 11; slot++)
                {
                    var huddle = GameDayLivePresentation.HuddlePoint(yardLine, direction, offense, slot);
                    Assert.True(Math.Sqrt(Math.Pow(huddle.X - official.X, 2) + Math.Pow(huddle.Y - official.Y, 2)) > .04);
                }
    }

    [Fact]
    public void UserTimeoutIsRecordedWithoutAdvancingClockDownOrStats()
    {
        var s = GameDayEngineTests.Session(GameDayMode.Play); s.ClockStopped = false;
        var before = GameDayEngine.BoxScore(s); var clock = s.ClockSeconds; var spot = s.YardLine;
        Assert.True(GameDayEngine.TryCallTimeout(s, "Home"));
        var e = Assert.Single(s.Events);
        Assert.Equal(SnapOutcome.ClockStoppage, e.Outcome); Assert.Equal("Home", e.TimeoutTeamName);
        Assert.Equal(2, e.HomeTimeoutsRemaining); Assert.Equal(3, e.AwayTimeoutsRemaining);
        Assert.Equal(clock, s.ClockSeconds); Assert.Equal(spot, s.YardLine); Assert.Equal(1, s.Down);
        Assert.Equal(before.HomeStats, GameDayEngine.BoxScore(s).HomeStats);
        Assert.Empty(GameDayEngine.BoxScore(s).Players); Assert.False(GameDayEngine.TryCallTimeout(s, "Home"));
        Assert.Single(s.Events);
        var copy = JsonSerializer.Deserialize<GameSnapEvent>(JsonSerializer.Serialize(e))!;
        Assert.Equal(e.TimeoutTeamName, copy.TimeoutTeamName); Assert.Equal(2, copy.HomeTimeoutsRemaining);
    }

    [Fact]
    public void CpuTimeoutIsItsOwnVisibleEventBeforeTheNextSnap()
    {
        var s = GameDayEngineTests.Session(GameDayMode.Play); s.ControlledTeamName = "Home";
        s.TwoMinuteTimeoutHalf = 2; s.Quarter = 4; s.ClockSeconds = 60; s.HomeScore = 7; s.ClockStopped = false;
        var e = GameDayEngine.Step(s);
        Assert.Equal(SnapOutcome.ClockStoppage, e.Outcome); Assert.Equal("Away", e.TimeoutTeamName);
        Assert.Equal(60, s.ClockSeconds); Assert.Equal(2, s.AwayTimeouts);
        Assert.Equal(e, Assert.Single(s.Events));
        Assert.NotEqual(SnapOutcome.ClockStoppage, GameDayEngine.Step(s).Outcome);
    }

    [Theory]
    [InlineData(2, 0, 0)] [InlineData(2, 14, 0)] [InlineData(4, 0, 7)] [InlineData(4, 0, 0)]
    public void FinalHeaveIsRecommendedBeforeHalfOrGameRatherThanPunting(int quarter, int home, int away)
    {
        var s = GameDayEngineTests.Session(GameDayMode.Play); s.Quarter = quarter; s.ClockSeconds = 3;
        s.YardLine = 45; s.Down = 4; s.HomeScore = home; s.AwayScore = away;
        var advice = GameDayRecommendationService.Recommend(s, s.HomeProfile.GamePlan!);
        Assert.Equal("Hail Mary", advice.Play?.Name);
        Assert.Contains(advice.Play!, GamePlanService.AvailableOffensivePlays(s.HomeProfile.GamePlan!));
    }

    [Fact]
    public void CpuUsesEndOfHalfHeaveAndDoesNotOverrideUserCallsOrCustomRestrictions()
    {
        var s = GameDayEngineTests.Session(GameDayMode.Watch); s.Quarter = 2; s.ClockSeconds = 2; s.YardLine = 45; s.Down = 2; s.ClockStopped = true;
        s.TwoMinuteTimeoutHalf = 1;
        Assert.Equal("Hail Mary", GameDayRecommendationService.Recommend(s, GamePlanService.Normalize(s.HomeProfile.GamePlan!)).Play?.Name);
        var heaveSnap = GameDayEngine.Step(s);
        Assert.True(heaveSnap.OffensivePlayId.Contains("hail-mary"), $"{heaveSnap.OffensivePlayId}; q{heaveSnap.Quarter}; clock{heaveSnap.ClockSeconds}; spot{heaveSnap.StartYardLine}; {heaveSnap.Outcome}");
        var plan = s.HomeProfile.GamePlan! with { Offense = OffensivePlaybook.Custom, CustomOffensivePlayIds = ["off-spread-zone"] };
        var fresh = GameDayEngineTests.Session(GameDayMode.Play); fresh.Quarter = 2; fresh.ClockSeconds = 2; fresh.YardLine = 45;
        Assert.NotEqual("Hail Mary", GameDayRecommendationService.Recommend(fresh, plan).Play?.Name);
    }

    [Fact]
    public void RealBreakupsHaveANearbyCreditedDefenderAndAreNotDropsOrTackles()
    {
        GameDaySession? found = null; GameSnapEvent? e = null;
        for (var seed = 1; seed <= 1000 && e is null; seed++)
        {
            var s = GameDayEngineTests.Session(GameDayMode.Play, seed: seed);
            var snap = GameDayEngine.Step(s, "off-spread-verts");
            if (snap.IsPassBreakup) { found = s; e = snap; }
        }
        Assert.NotNull(e); Assert.Equal(SnapOutcome.Incompletion, e.Outcome); Assert.False(e.IsDrop);
        Assert.InRange(e.TargetSeparation, 0, 1.5); Assert.Contains(e.PrimaryDefenderId!.Value, e.DefensePersonnel);
        var stats = GameDayEngine.BoxScore(found!).Players.Single(p => p.PlayerId == e.PrimaryDefenderId);
        Assert.Equal(1, stats.PassesDefended); Assert.Equal(0, stats.Tackles); Assert.Equal(0, stats.Interceptions);
        Assert.Contains("breaks up", GameDayCommentaryService.Describe(e, found!.PlayersById));
        Assert.True(JsonSerializer.Deserialize<GameSnapEvent>(JsonSerializer.Serialize(e))!.IsPassBreakup);
    }

    [Theory]
    [InlineData(-1)] [InlineData(1)]
    public void TipStartsAtTheDefenderAndTravelsAwayWithoutTransferringPossession(int direction)
    {
        var contact = new GameFieldPoint(.5, .5);
        Assert.Equal(contact, GameDayLivePresentation.DeflectedBallPoint(contact, direction, .7, .7));
        var end = GameDayLivePresentation.DeflectedBallPoint(contact, direction, 1, .7);
        Assert.True((end.X - contact.X) * direction > 0); Assert.True(end.Y > contact.Y);
        var snap = new GameSnapEvent { OffenseTeamName = "Home", DefenseTeamName = "Away", OffensivePlayId = "pass", DefensivePlayId = "def",
            Outcome = SnapOutcome.Incompletion, IsPassBreakup = true, QuarterbackId = Guid.NewGuid(), TargetId = Guid.NewGuid(), PrimaryDefenderId = Guid.NewGuid() };
        Assert.Null(GameDayAnimationPlanner.BallOwner(snap, .9, .5));
    }

    [Theory]
    [InlineData(1)] [InlineData(50)] [InlineData(99)]
    public void HuddlesKeepPlayersOnFieldAndOnTheirOwnSideOfTheBall(int yard)
    {
        foreach (var direction in new[] { -1, 1 })
        {
            var offense = Enumerable.Range(0, 11).Select(i => GameDayLivePresentation.HuddlePoint(yard, direction, true, i)).ToArray();
            var defense = Enumerable.Range(0, 11).Select(i => GameDayLivePresentation.HuddlePoint(yard, direction, false, i)).ToArray();
            Assert.Equal(11, offense.Distinct().Count());
            foreach (var p in offense.Concat(defense)) { Assert.InRange(p.X, .08, .92); Assert.InRange(p.Y, .3, .7); }
            Assert.True((defense.Average(p => p.X) - offense.Average(p => p.X)) * direction > 0);
        }
    }

    [Theory]
    [InlineData(SnapOutcome.Touchdown, 5, true)] [InlineData(SnapOutcome.Interception, 0, true)]
    [InlineData(SnapOutcome.Completion, 25, true)] [InlineData(SnapOutcome.Run, 4, false)]
    [InlineData(SnapOutcome.Incompletion, 0, false)] [InlineData(SnapOutcome.Penalty, 25, false)]
    public void AutomaticReplayTargetsScoresTurnoversAndExplosivePlays(SnapOutcome outcome, int yards, bool expected)
    {
        var snap = new GameSnapEvent { OffenseTeamName = "H", DefenseTeamName = "A", OffensivePlayId = "", DefensivePlayId = "", Outcome = outcome, Yards = yards };
        Assert.Equal(expected, GameDayLivePresentation.InstantReplay(snap));
    }
}
