using DynastyManager.Core.Models;
using DynastyManager.Core.Seasons;
using DynastyManager.Core.Simulation;
using Xunit;

namespace DynastyManager.Core.Tests;

public class GameDayLiveBoxAndPursuitTests
{
    [Theory]
    [InlineData(false,1)] [InlineData(true,0)]
    public void StartingSeasonOnlyVisitsWeekZeroWhenLeagueHasOpeningGames(bool openingGame,int expected)
    {
        var game=new ScheduledGame {GameId="opener",SeasonYear=2026,Week=openingGame?0:1,HomeTeamName="Home",AwayTeamName="Away"};
        var preseason=new DynastyState {DynastyName="Test",UserTeamName="Other school",SeasonYear=2026,Phase=SeasonPhase.Preseason,Schedule=[game]};
        Assert.Equal("Preseason",SeasonProgression.PeriodLabel(preseason.Phase,preseason.Week));
        Assert.Equal("Start season",SeasonProgression.AdvanceLabel(preseason.Phase,preseason.Week));
        var started=SeasonProgression.Advance(preseason);Assert.Equal(expected,started.Week);Assert.Equal(SeasonPhase.RegularSeason,started.Phase);
        Assert.Equal(1,SeasonProgression.Advance(started with {Week=0}).Week);
        Assert.NotEqual("Preseason",SeasonProgression.PeriodLabel(started.Phase,started.Week));
    }
    [Fact]
    public void OldSeasonOpeningGamesDoNotKeepANewSeasonAtZero()
    {
        var state=new DynastyState {DynastyName="Test",UserTeamName="Home",SeasonYear=2027,Phase=SeasonPhase.Preseason,
            Schedule=[new ScheduledGame {GameId="old",SeasonYear=2026,Week=0,HomeTeamName="Home",AwayTeamName="Away"}]};
        Assert.Equal(1,SeasonProgression.Advance(state).Week);
    }
    [Fact]
    public void LookingAtLiveStatsDoesNotAdvanceOrChangeTheSimulation()
    {
        var first=GameDayEngineTests.Session(GameDayMode.Watch,includeOpeningKickoff:true);
        var second=GameDayEngine.Start(first.Game,first.HomeProfile,first.AwayProfile,first.Roster,GameDayMode.Watch);
        var empty=GameDayEngine.BoxScore(first);Assert.Equal(0,empty.HomeStats.TotalYards);Assert.Empty(empty.Players);Assert.False(first.IsComplete);
        for(var i=0;i<35;i++)
        {
            GameDayEngine.Step(first);GameDayEngine.Step(second);
            var count=first.Events.Count;var clock=first.ClockSeconds;var down=first.Down;var spot=first.YardLine;
            for(var refresh=0;refresh<3;refresh++)GameDayEngine.BoxScore(first);
            Assert.Equal(count,first.Events.Count);Assert.Equal(clock,first.ClockSeconds);Assert.Equal(down,first.Down);Assert.Equal(spot,first.YardLine);
        }
        GameDayEngine.SimulateToEnd(first);GameDayEngine.SimulateToEnd(second);
        var a=GameDayEngine.Finish(first);var b=GameDayEngine.Finish(second);var box=GameDayEngine.BoxScore(first);
        Assert.Equal(a.HomeStats,b.HomeStats);Assert.Equal(a.AwayStats,b.AwayStats);Assert.Equal(a.HomeScore,b.HomeScore);Assert.Equal(a.AwayScore,b.AwayScore);
        Assert.Equal(a.PlayerStats,b.PlayerStats);Assert.Equal(a.HomeStats,box.HomeStats);Assert.Equal(a.AwayStats,box.AwayStats);Assert.Equal(a.PlayerStats,box.Players);
        Assert.Equal(box.HomeScore,box.Periods.Sum(p=>p.HomePoints));Assert.Equal(box.AwayScore,box.Periods.Sum(p=>p.AwayPoints));
    }
    [Fact]
    public void LiveScoreboardIncludesConversionsAndOvertimeInTheCorrectPeriod()
    {
        var s=GameDayEngineTests.Session(GameDayMode.Play);s.Quarter=5;s.HomeScore=15;s.AwayScore=3;
        GameSnapEvent Event(int q,string offense,SnapOutcome outcome,int points=0)=>new() {Quarter=q,OffenseTeamName=offense,DefenseTeamName=offense=="Home"?"Away":"Home",OffensivePlayId="",DefensivePlayId="",Outcome=outcome,Points=points};
        s.Events.AddRange([Event(1,"Home",SnapOutcome.Touchdown),Event(1,"Home",SnapOutcome.ExtraPoint,1),Event(2,"Away",SnapOutcome.FieldGoal),Event(5,"Home",SnapOutcome.Touchdown),Event(5,"Home",SnapOutcome.TwoPointConversion,2)]);
        var box=GameDayEngine.BoxScore(s);Assert.Equal(5,box.Periods.Count);Assert.Equal(7,box.Periods[0].HomePoints);Assert.Equal(3,box.Periods[1].AwayPoints);Assert.Equal(8,box.Periods[4].HomePoints);
        Assert.False(s.IsComplete);Assert.False(box.IsComplete);
    }
    [Fact]
    public void TouchdownsAndInterceptionsDoNotInventDefensiveTackles()
    {
        var s=GameDayEngineTests.Session(GameDayMode.Play);var qb=s.HomeRoster.First(p=>p.Position==Position.QB);
        var wr=s.HomeRoster.First(p=>p.Position==Position.WR);var rb=s.HomeRoster.First(p=>p.Position==Position.RB);var db=s.AwayRoster.First(p=>p.Position==Position.CB);
        GameSnapEvent Event(SnapOutcome outcome,bool pass,int yards)=>new() {Quarter=1,OffenseTeamName="Home",DefenseTeamName="Away",OffensivePlayId=pass?"off-spread-verts":"off-spread-zone",DefensivePlayId="",Outcome=outcome,Yards=yards,QuarterbackId=qb.PlayerId,TargetId=pass?wr.PlayerId:null,BallCarrierId=pass?null:rb.PlayerId,PrimaryDefenderId=db.PlayerId};
        s.Events.AddRange([Event(SnapOutcome.Touchdown,true,30),Event(SnapOutcome.Interception,true,0),Event(SnapOutcome.Run,false,-2)]);
        var defender=GameDayEngine.BoxScore(s).Players.Single(p=>p.PlayerId==db.PlayerId);
        Assert.Equal(1,defender.Tackles);Assert.Equal(1,defender.Interceptions);Assert.Equal(1,defender.TacklesForLoss);
    }
    private static (DynastyPlayer[] Players,DefensivePlayDefinition Play) Defense()
    {
        var roster=GameDayEngineTests.Session(GameDayMode.Play).AwayRoster;
        var players=new[]{Position.DT,Position.MLB,Position.CB,Position.FS}.Select(position=>roster.First(p=>p.Position==position)).ToArray();
        var play=new DefensivePlayDefinition("test","Test front",DefensiveFormation.FourThree,DefensivePersonnel.BaseFourThree,DefensiveApproach.Balanced,"")
        {Assignments=[new(){Position=Position.DT,Depth=1,Lane=.5,Task=AssignmentTask.Rush},new(){Position=Position.MLB,Depth=6,Lane=.5,Task=AssignmentTask.Zone},new(){Position=Position.CB,Depth=5,Lane=.85,Task=AssignmentTask.Man},new(){Position=Position.FS,Depth=15,Lane=.5,Task=AssignmentTask.Zone}]};
        return(players,play);
    }
    [Fact]
    public void ShortBackfieldStopsFavorTheFrontRatherThanDeepSafeties()
    {
        var (players,play)=Defense();
        var decisions=Enumerable.Range(0,500).Select(seed=>DefensivePursuitService.Select(players,play,-3,.5,false,false,0,0,null,0,new Random(seed))).ToArray();
        Assert.True(decisions.Count(d=>d.Tackler!.Position==Position.DT)>300);
        Assert.All(decisions,d=>{Assert.Contains(d.Tackler!.PlayerId,d.AttemptIds);Assert.DoesNotContain(players.Single(p=>p.Position==Position.FS).PlayerId,d.AttemptIds);});
    }
    [Fact]
    public void OutsideCompletionsFavorTheNearbyCoverageCorner()
    {
        var (players,play)=Defense();var corner=players.Single(p=>p.Position==Position.CB);
        var decisions=Enumerable.Range(0,100).Select(seed=>DefensivePursuitService.Select(players,play,18,.85,true,false,12,2.5,corner.PlayerId,2,new Random(seed))).ToArray();
        Assert.True(decisions.Count(d=>d.Tackler!.PlayerId==corner.PlayerId)>=90);
    }
    [Fact]
    public void SacksSelectActualRushersAndExcludeIneligiblePlayers()
    {
        var (players,play)=Defense();
        var result=DefensivePursuitService.Select(players,play,-5,.5,true,true,0,3,null,0,new Random(1));
        Assert.Equal(Position.DT,result.Tackler!.Position);Assert.Single(result.AttemptIds);
        var inactive=players.Select(p=>p with {IsRedshirted=true}).ToArray();
        Assert.Null(DefensivePursuitService.Select(inactive,play,0,.5,false,false,0,0,null,0,new Random(1)).Tackler);
        var starts=DefensivePursuitService.Alignments(players,play);Assert.Equal(new GameFieldPoint(5,.85),starts[players.Single(p=>p.Position==Position.CB).PlayerId]);
    }
}
