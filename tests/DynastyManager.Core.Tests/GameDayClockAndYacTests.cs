using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;
namespace DynastyManager.Core.Tests;

public class GameDayClockAndYacTests
{
    [Theory]
    [InlineData(1,"Home 1")] [InlineData(42,"Home 42")] [InlineData(50,"50")]
    [InlineData(58,"Away 42")] [InlineData(99,"Away 1")]
    public void BallOnUsesConventionalTeamAndYardLine(int spot,string expected)
    {
        var s=GameDayEngineTests.Session(GameDayMode.Play);s.YardLine=spot;
        Assert.Equal(expected,GameDayEngine.BallOn(s));
        s.PossessionTeamName="Away";
        Assert.Equal(expected.Replace("Home","TEMP").Replace("Away","Home").Replace("TEMP","Away"),GameDayEngine.BallOn(s));
    }
    [Theory]
    [InlineData(false, 31, 37)]
    [InlineData(true, 36, 39)]
    public void NormalPacingUsesLeagueRulesAndRespectsStoppedClock(bool pro, int minimum, int maximum)
    {
        var source = GameDayEngineTests.Session(GameDayMode.Play);
        var plan = source.HomeProfile.GamePlan! with { Tempo = OffensiveTempo.Normal };
        foreach (var stopped in new[] { false, true })
        {
            var s = GameDayEngine.Start(source.Game, source.HomeProfile with { GamePlan = plan },
                source.AwayProfile, source.Roster, GameDayMode.Play,
                rules: pro ? FootballRules.Professional : FootballRules.College);
            s.KickoffPending = false; s.PlayClockSeconds = 40; s.ClockStopped = stopped;
            var snap = GameDayEngine.Step(s, "off-spread-zone");
            Assert.InRange(s.LastPreSnapSeconds, stopped ? 0 : minimum, stopped ? 0 : maximum);
            Assert.Equal(900 - snap.PreSnapSeconds - snap.LiveBallSeconds, s.ClockSeconds);
        }
    }

    [Theory]
    [InlineData(false, OffensiveTempo.UpTempo, 12, 20)]
    [InlineData(true, OffensiveTempo.UpTempo, 12, 20)]
    [InlineData(false, OffensiveTempo.ChewClock, 37, 39)]
    [InlineData(true, OffensiveTempo.ChewClock, 37, 39)]
    [InlineData(true, OffensiveTempo.Normal, 24, 24)]
    public void LeaguePacingKeepsExplicitTemposAndAdministrativePlayClock(
        bool pro, OffensiveTempo tempo, int minimum, int maximum)
    {
        var source = GameDayEngineTests.Session(GameDayMode.Play);
        var plan = source.HomeProfile.GamePlan! with { Tempo = tempo };
        var s = GameDayEngine.Start(source.Game, source.HomeProfile with { GamePlan = plan },
            source.AwayProfile, source.Roster, GameDayMode.Play,
            rules: pro ? FootballRules.Professional : FootballRules.College);
        s.KickoffPending = false; s.ClockStopped = false;
        s.PlayClockSeconds = tempo == OffensiveTempo.Normal ? 25 : 40;
        GameDayEngine.Step(s, "off-spread-zone");
        Assert.InRange(s.LastPreSnapSeconds, minimum, maximum);
    }

    [Fact]
    public void IncompletionConsumesPriorRunoffButNextSnapStartsStopped()
    {
        for(var seed=1;seed<1000;seed++) {
            var s=GameDayEngineTests.Session(GameDayMode.Play,seed:seed);
            s.PlayClockSeconds=40;
            var snap=GameDayEngine.Step(s,"off-spread-verts");
            if(snap.Outcome!=SnapOutcome.Incompletion)continue;
            Assert.InRange(snap.PreSnapSeconds,12,39);
            Assert.Equal(900-snap.PreSnapSeconds-snap.LiveBallSeconds,s.ClockSeconds);
            Assert.True(s.ClockStopped);
            var next=GameDayEngine.Step(s,"off-spread-zone");
            Assert.Equal(0,s.LastPreSnapSeconds);
            return;
        }
        Assert.Fail("No incompletion encountered.");
    }
    [Fact]
    public void ExpiredClockDoesNotCreateAPhantomSnapOrStat()
    {
        var s=GameDayEngineTests.Session(GameDayMode.Play);s.Quarter=4;s.ClockSeconds=5;
        s.TwoMinuteTimeoutHalf=2;s.HomeScore=7;s.AwayTimeouts=0;
        var snap=GameDayEngine.Step(s,"off-spread-zone");
        Assert.Equal(SnapOutcome.ClockStoppage,snap.Outcome);Assert.True(s.IsComplete);
        var result=GameDayEngine.Finish(s);
        Assert.Equal(0,result.HomeStats!.RushAttempts+result.HomeStats.PassAttempts);
        Assert.Empty(result.Drives);Assert.Empty(result.PlayerStats);
    }
    [Fact]
    public void TwoMinuteTimeoutOccursOnceAndPreservesTheDown()
    {
        var s=GameDayEngineTests.Session(GameDayMode.Play);s.Quarter=2;s.ClockSeconds=125;
        var snap=GameDayEngine.Step(s,"off-spread-zone");
        Assert.Equal(SnapOutcome.ClockStoppage,snap.Outcome);Assert.Equal(120,s.ClockSeconds);
        Assert.Equal(1,s.Down);Assert.Equal(3,s.HomeTimeouts);Assert.Equal(25,s.PlayClockSeconds);
        var next=GameDayEngine.Step(s,"off-spread-zone");
        Assert.NotEqual(SnapOutcome.ClockStoppage,next.Outcome);Assert.Equal(0,s.LastPreSnapSeconds);
    }
    [Fact]
    public void LateFirstDownRestartsOnReadyWithoutGivingAwayAWholeSnapOfRunoff()
    {
        var s=GameDayEngineTests.Session(GameDayMode.Play);s.Quarter=4;s.ClockSeconds=100;s.TwoMinuteTimeoutHalf=2;
        s.ClockStopped=true;s.ClockRestartsOnReady=true;s.AwayScore=7;s.PlayClockSeconds=40;
        var snap=GameDayEngine.Step(s,"off-spread-zone");
        Assert.InRange(s.LastPreSnapSeconds,8,16);
        Assert.True(s.ClockSeconds<92);
    }
    [Fact]
    public void TempoIsPersistedAndAdaptsToLateScore()
    {
        var source=GameDayEngineTests.Session(GameDayMode.Play);
        var plan=source.HomeProfile.GamePlan! with {Tempo=OffensiveTempo.ChewClock};
        var s=GameDayEngine.Start(source.Game,source.HomeProfile with {GamePlan=plan},source.AwayProfile,source.Roster,GameDayMode.Play);
        s.KickoffPending=false;s.PlayClockSeconds=40;
        Assert.Equal(OffensiveTempo.ChewClock,GameDayEngine.EffectiveTempo(s));
        GameDayEngine.Step(s,"off-spread-zone");Assert.InRange(s.LastPreSnapSeconds,37,39);
        s.Quarter=4;s.ClockSeconds=90;s.AwayScore=7;
        Assert.Equal(OffensiveTempo.UpTempo,GameDayEngine.EffectiveTempo(s));
        var state=new DynastyState {DynastyName="Test",UserTeamName="Home"};
        Assert.Equal(plan,GamePlanService.SetPlan(state,plan).TeamGamePlans.Single());
    }
    [Fact]
    public void SacksAndNegativeCompletionsReconcileWithNcaaTeamAndPlayerTotals()
    {
        var s=GameDayEngineTests.Session(GameDayMode.Play);
        var qb=s.HomeRoster.First(p=>p.Position==Position.QB);var wr=s.HomeRoster.First(p=>p.Position==Position.WR);
        var defender=s.AwayRoster.First(p=>p.Position==Position.DE);
        GameSnapEvent Snap(SnapOutcome outcome,int yards)=>new() {Sequence=s.Events.Count+1,PossessionNumber=1,Quarter=1,
            OffenseTeamName="Home",DefenseTeamName="Away",OffensivePlayId="off-spread-verts",DefensivePlayId="",
            QuarterbackId=qb.PlayerId,TargetId=wr.PlayerId,PrimaryDefenderId=defender.PlayerId,Outcome=outcome,Yards=yards,Down=1,Distance=10};
        s.Events.Add(Snap(SnapOutcome.Sack,-7));s.Events.Add(Snap(SnapOutcome.Completion,-1));s.IsComplete=true;
        var result=GameDayEngine.Finish(s);var stats=result.HomeStats!;
        Assert.Equal(1,stats.RushAttempts);Assert.Equal(-7,stats.RushYards);Assert.Equal(1,stats.PassAttempts);Assert.Equal(-1,stats.PassYards);
        Assert.Equal(-7,result.PlayerStats.Single(p=>p.PlayerId==qb.PlayerId).RushYards);
        Assert.Equal(-1,result.PlayerStats.Single(p=>p.PlayerId==wr.PlayerId).ReceivingYards);
        Assert.Equal(2,result.PlayerStats.Single(p=>p.PlayerId==defender.PlayerId).TacklesForLoss);
        Assert.Equal(1,stats.Possessions);Assert.Equal(-8,result.Drives.Single().PassYards+result.Drives.Single().RushYards);
    }
    [Fact]
    public void YacHasAnExplosiveTailRespondsToRatingsAndStopsAtTheGoalLine()
    {
        var s=GameDayEngineTests.Session(GameDayMode.Play);var wr=s.HomeRoster.First(p=>p.Position==Position.WR);
        var fast=wr with {SpeedRating=99,AgilityRating=99,Skills=new(){Acceleration=99,BreakTackle=99}};
        var slow=wr with {SpeedRating=40,AgilityRating=40,Skills=new(){Acceleration=40,BreakTackle=40}};
        var defender=s.AwayRoster.First(p=>p.Position==Position.CB);
        var play=PlaybookCatalog.OffensivePlays.First(p=>p.Id=="off-spread-verts");
        var strong=Enumerable.Range(0,2000).Select(seed=>ReceivingGainService.Resolve(play,fast,defender,3,0,20,new(seed))).ToArray();
        var weak=Enumerable.Range(0,2000).Select(seed=>ReceivingGainService.Resolve(play,slow,defender,3,0,20,new(seed))).ToArray();
        Assert.True(strong.Average(g=>g.YardsAfterCatch)>weak.Average(g=>g.YardsAfterCatch));
        Assert.Contains(strong,g=>g.YardsAfterCatch>=20);
        for(var seed=0;seed<200;seed++) {var gain=ReceivingGainService.Resolve(play,fast,defender,3,0,95,new(seed));Assert.InRange(gain.TotalYards,-94,5);Assert.True(gain.YardsAfterCatch>=0);}
    }
    [Fact]
    public void FullGameReceivingAndYacTotalsAgreeAcrossPlayerTeamAndSnapStats()
    {
        var s=GameDayEngineTests.Session(GameDayMode.Simulate,includeOpeningKickoff:true);
        GameDayEngine.SimulateToEnd(s);var game=GameDayEngine.Finish(s);
        foreach(var (name,stats) in new[]{("Home",game.HomeStats!),("Away",game.AwayStats!)}) {
            var players=game.PlayerStats.Where(p=>p.TeamName==name).ToArray();
            var catches=s.Events.Where(e=>e.OffenseTeamName==name&&e.TargetId.HasValue&&e.Outcome is SnapOutcome.Completion or SnapOutcome.Touchdown).ToArray();
            Assert.All(catches,e=>Assert.Equal(e.Yards,e.AirYards+e.YardsAfterCatch));
            Assert.Equal(stats.PassYards,players.Sum(p=>p.ReceivingYards));Assert.Equal(stats.PassYards,players.Sum(p=>p.PassYards));
            Assert.Equal(stats.YardsAfterCatch,players.Sum(p=>p.YardsAfterCatch));Assert.Equal(stats.YardsAfterCatch,catches.Sum(e=>e.YardsAfterCatch));
            Assert.True(stats.YardsAfterCatch>0);Assert.Equal(stats.RushYards,players.Sum(p=>p.RushYards));
        }
    }
}
