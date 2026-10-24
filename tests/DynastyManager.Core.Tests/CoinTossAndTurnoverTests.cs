using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;
namespace DynastyManager.Core.Tests;

public sealed class CoinTossAndTurnoverTests
{
    private static GameSnapEvent Pick(int spot = 50) => new() { Sequence = 1, Quarter = 1, ClockSeconds = 800,
        OffenseTeamName = "Home", DefenseTeamName = "Away", OffensivePlayId = "off-spread-verts", DefensivePlayId = "", Outcome = SnapOutcome.Interception,
        StartYardLine = spot, BallEndYardLine = spot, Down = 1, Distance = 10 };

    [Fact]
    public void TossIsRepeatableCanFavorEitherSchoolAndDoesNotConsumeGameRandomness()
    {
        var source = GameDayEngineTests.Session(GameDayMode.Play);
        var receivers = new HashSet<string>();
        for (var seed = 0; seed < 50; seed++)
        {
            var game = source.Game with { SimulationSeed = seed };
            var toss = CoinTossService.Toss(game);
            Assert.Equal(toss, CoinTossService.Toss(game));
            receivers.Add(toss.ReceivingTeam);
            Assert.NotEqual(toss.Winner, toss.ReceivingTeam);
        }
        Assert.Equal(2, receivers.Count);
        var first = source.Random.Next();
        var fresh = GameDayEngineTests.Session(GameDayMode.Play);
        CoinTossService.Apply(fresh, CoinTossService.Toss(fresh.Game));
        Assert.Equal(first, fresh.Random.Next());
    }

    [Theory]
    [InlineData("Home")][InlineData("Away")]
    public void SecondHalfKickoffGoesToTheOtherOpeningReceiver(string receiver)
    {
        var s = GameDayEngineTests.Session(GameDayMode.Play);
        CoinTossService.Apply(s, new(true, receiver, receiver, false));
        Assert.Equal(receiver, s.PossessionTeamName);
        s.KickoffPending = false; s.Quarter = 2; s.ClockSeconds = 0;
        GameDayEngine.Step(s);
        Assert.Equal(3, s.Quarter);
        Assert.NotEqual(receiver, s.PossessionTeamName);
        Assert.Equal(receiver, s.KickoffTeamName);
        Assert.True(s.KickoffPending);
    }

    [Fact]
    public void ReturnsIncludeDownedRecoveriesExplosiveGainsAndTouchdownsWithinFieldBounds()
    {
        var session = GameDayEngineTests.Session(GameDayMode.Play);
        var defender = session.Roster.First(p => p.TeamName == "Away" && p.Position == Position.CB) with { SpeedRating = 99, AgilityRating = 99 };
        var offense = session.Roster.Where(p => p.TeamName == "Home").Take(11).ToArray();
        var samples = Enumerable.Range(0, 1000).Select(seed => TurnoverReturnService.Resolve(Pick(25), defender, offense, OffensiveConcept.QuickPass, new Random(seed))).ToArray();
        Assert.Contains(samples, s => s.ReturnYards == 0);
        Assert.Contains(samples, s => s.ReturnYards >= 20);
        Assert.Contains(samples, s => s.DefensiveScore);
        Assert.All(samples, s => { Assert.InRange(s.BallEndYardLine, 0, 100); Assert.Equal(s.TurnoverYardLine - s.ReturnYards, s.BallEndYardLine); });
        var fumble = TurnoverReturnService.Resolve(Pick() with { Outcome = SnapOutcome.Fumble }, defender, offense, OffensiveConcept.InsideRun, new Random(3));
        Assert.Equal(50, fumble.TurnoverYardLine);
        Assert.Equal(0, fumble.Yards);
    }

    [Fact]
    public void DefensiveTouchdownIsCreditedToDefenseAndSetsUpItsTry()
    {
        var s = GameDayEngineTests.Session(GameDayMode.Play);
        var defender = s.Roster.First(p => p.TeamName == "Away" && p.Position == Position.CB);
        var qb = s.Roster.First(p => p.TeamName == "Home" && p.Position == Position.QB);
        var snap = Pick() with { TurnoverYardLine = 60, ReturnYards = 60, BallEndYardLine = 0, Points = 6,
            PrimaryDefenderId = defender.PlayerId, ReturnerId = defender.PlayerId, QuarterbackId = qb.PlayerId };
        s.Events.Add(snap);
        GameDayEngine.ApplyTurnoverReturn(s, snap);
        Assert.Equal(0, s.HomeScore); Assert.Equal(6, s.AwayScore);
        Assert.Equal("Away", s.PossessionTeamName); Assert.True(s.TryPending);
        var box = GameDayEngine.BoxScore(s);
        Assert.Equal(6, box.Periods[0].AwayPoints);
        Assert.Equal(0, box.Periods[0].HomePoints);
        var stats = box.Players.Single(p => p.PlayerId == defender.PlayerId);
        Assert.Equal(1, stats.Interceptions); Assert.Equal(1, stats.DefensiveTouchdowns); Assert.Equal(60, stats.DefensiveReturnYards);
        var conversion = GameDayEngine.Step(s, "special-extra-point");
        Assert.Equal("Away", conversion.OffenseTeamName);
        Assert.True(s.KickoffPending); Assert.Equal("Away", s.KickoffTeamName);
    }

    [Fact]
    public void TouchbackAndReturnUseNewPossessionsCoordinateSystem()
    {
        var s = GameDayEngineTests.Session(GameDayMode.Play);
        GameDayEngine.ApplyTurnoverReturn(s, Pick() with { TurnoverYardLine = 70, BallEndYardLine = 55, ReturnYards = 15 });
        Assert.Equal("Away", s.PossessionTeamName); Assert.Equal(45, s.YardLine);
        s.PossessionTeamName = "Home";
        GameDayEngine.ApplyTurnoverReturn(s, Pick() with { TurnoverYardLine = 100, BallEndYardLine = 100, IsTouchback = true });
        Assert.Equal(20, s.YardLine);
    }

    [Fact]
    public void LiveBallOwnerChangesToRecoveringDefenderAndCommentaryDescribesReturn()
    {
        var s = GameDayEngineTests.Session(GameDayMode.Play);
        var defender = s.Roster.First(p => p.TeamName == "Away");
        var snap = Pick() with { TurnoverYardLine = 60, ReturnerId = defender.PlayerId, PrimaryDefenderId = defender.PlayerId,
            QuarterbackId = s.Roster.First(p => p.TeamName == "Home").PlayerId, ReturnYards = 20 };
        Assert.Equal(snap.QuarterbackId, GameDayAnimationPlanner.BallOwner(snap, .1, .3));
        Assert.Equal(defender.PlayerId, GameDayAnimationPlanner.BallOwner(snap, .9, .3));
        Assert.Contains("returns it 20 yards", GameDayCommentaryService.Describe(snap, s.PlayersById));
    }
    [Fact]
    public void OvertimeDefensiveScoreEndsGameWithoutAnUnnecessaryConversion()
    {
        var s = GameDayEngineTests.Session(GameDayMode.Play);
        s.OvertimeRound = 1; s.Quarter = 5;
        GameDayEngine.ApplyTurnoverReturn(s, Pick() with { TurnoverYardLine = 25, BallEndYardLine = 0, ReturnYards = 25, Points = 6 });
        Assert.Equal(6, s.AwayScore); Assert.True(s.IsComplete); Assert.False(s.TryPending); Assert.False(s.KickoffPending);
    }

    [Fact]
    public void ReturnAbilityChangesYardageAndRecordedReturnSurvivesSerialization()
    {
        var s = GameDayEngineTests.Session(GameDayMode.Play);
        var defender = s.Roster.First(p => p.TeamName == "Away");
        var fast = defender with { SpeedRating = 99, AgilityRating = 99 };
        var slow = defender with { SpeedRating = 40, AgilityRating = 40 };
        var offense = s.HomeRoster.Take(11).ToArray();
        int Sum(DynastyPlayer p) => Enumerable.Range(0, 500).Sum(seed => TurnoverReturnService.Resolve(Pick(75), p, offense, OffensiveConcept.QuickPass, new Random(seed)).ReturnYards);
        Assert.True(Sum(fast) > Sum(slow));
        var snap = TurnoverReturnService.Resolve(Pick(), fast, offense, OffensiveConcept.DeepPass, new Random(1));
        var roundtrip = System.Text.Json.JsonSerializer.Deserialize<GameSnapEvent>(System.Text.Json.JsonSerializer.Serialize(snap));
        Assert.NotNull(roundtrip); Assert.Equal(snap.TurnoverYardLine, roundtrip.TurnoverYardLine); Assert.Equal(snap.ReturnYards, roundtrip.ReturnYards); Assert.Equal(snap.ReturnerId, roundtrip.ReturnerId);
    }
}
