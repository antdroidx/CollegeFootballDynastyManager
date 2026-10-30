using System.Text.Json;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;
namespace DynastyManager.Core.Tests;
public class DetailedStatisticsTests
{
    [Fact] public void SnapsBlockingTargetsPressureAndCoverageReconcileFromActualEvents()
    {
        var s=GameDayEngineTests.Session(GameDayMode.Simulate,seed:724);GameDayEngine.SimulateToEnd(s);var box=GameDayEngine.Finish(s);
        foreach(var line in box.PlayerStats)
        {
            Assert.Equal(s.Events.Count(e=>GameDayEngine.IsScrimmage(e)&&e.OffensePersonnel.Contains(line.PlayerId)),line.OffensiveSnaps);
            Assert.Equal(s.Events.Count(e=>GameDayEngine.IsScrimmage(e)&&e.DefensePersonnel.Contains(line.PlayerId)),line.DefensiveSnaps);
            Assert.Equal(s.Events.SelectMany(e=>e.Blocks).Count(b=>b.BlockerId==line.PlayerId&&b.PassBlock),line.PassBlockSnaps);
            Assert.InRange(line.PassBlockWins,0,line.PassBlockSnaps);Assert.InRange(line.RunBlockWins,0,line.RunBlockSnaps);
            Assert.InRange(line.SacksAllowed,0,line.PressuresAllowed);Assert.InRange(line.CoverageReceptions,0,line.CoverageTargets);
            Assert.InRange(line.TrackedReceptions,0,line.Targets);Assert.Equal(1,line.TrackedGames);
        }
        Assert.All(s.Events.Where(e=>e.Outcome==SnapOutcome.Sack),e=>{Assert.NotEmpty(e.PressureRushers);Assert.Contains(e.PrimaryDefenderId!.Value,e.PressureRushers);});
        Assert.Equal(box.PlayerStats.Sum(p=>p.Targets),box.PlayerStats.Sum(p=>p.CoverageTargets));
        Assert.Equal(box.PlayerStats.Sum(p=>p.TrackedReceptions),box.PlayerStats.Sum(p=>p.CoverageReceptions));
        Assert.Equal(s.Events.Count(e=>e.Outcome==SnapOutcome.Sack),box.PlayerStats.Sum(p=>p.TimesSacked));
    }
    [Fact] public void ProtectionIsAnActualMatchupAndUnblockedRushersHaveNoInventedSackLiability()
    {
        var s=GameDayEngineTests.Session(GameDayMode.Play);
        var blocker=s.HomeRoster.First(p=>p.Position==Position.OL) with {StrengthRating=75,Skills=new(){PassBlocking=99}};
        var weak=blocker with {Skills=new(){PassBlocking=1}};var rusher=s.AwayRoster.First(p=>p.Position==Position.DE);
        int Losses(DynastyPlayer b)=>Enumerable.Range(0,1000).Count(seed=>BlockMatchupService.Resolve([b],[rusher],3.1,true,new Random(seed)).PressureRushers.Count>0);
        Assert.True(Losses(blocker)<Losses(weak));
        var free=BlockMatchupService.Resolve([],[rusher],3,true,new Random(1));Assert.Single(free.PressureRushers);Assert.Null(free.SackBlockerId);
    }
    [Fact] public void AggregationKeepsMaximaAndRatesUseMatchedTrackedDenominators()
    {
        var old=new PlayerSeasonStatLine{PlayerName="OL",TeamName="A",PlayerId=Guid.NewGuid(),SeasonYear=2025,Games=17,Receptions=40};
        var a=old with {SeasonYear=2026,Games=1,Receptions=3,TrackedReceptions=3,Targets=5,TrackedGames=1,PassBlockSnaps=10,PassBlockWins=8,LongestReception=40};
        var b=a with {PassBlockSnaps=30,PassBlockWins=30,Receptions=1,TrackedReceptions=1,Targets=1,LongestReception=12};
        var total=PlayerStatTotals.Total([old,a,b]);Assert.Equal(40,total.LongestReception);Assert.Equal(40,total.PassBlockSnaps);Assert.Equal(95d,PlayerStatCatalog.Value(total,"PB Win%"));Assert.Equal(4d/6*100,PlayerStatCatalog.Value(total,"Catch%"));
        var punts=PlayerStatTotals.Total([old with {Punts=17},a with {Punts=2,TrackedPunts=2,NetPuntYards=80}]);Assert.Equal(40d,PlayerStatCatalog.Value(punts,"NetAvg"));
        Assert.Null(PlayerStatCatalog.Value(old,"PassBlockSnaps"));Assert.Equal("—",PlayerStatCatalog.Format(old,"PB Win%"));
        var restored=JsonSerializer.Deserialize<PlayerSeasonStatLine>(JsonSerializer.Serialize(total))!;Assert.Equal(total,restored);
    }
    [Fact] public void SpikeIsPassAttemptAndKneelIsRushWithoutFakeBlockingChances()
    {
        var s=GameDayEngineTests.Session(GameDayMode.Play);GameDayEngine.Step(s,"special-spike");GameDayEngine.Step(s,"special-kneel");s.IsComplete=true;
        var box=GameDayEngine.Finish(s);var qb=box.PlayerStats.Single(p=>p.Position==Position.QB);
        Assert.Equal(1,qb.PassAttempts);Assert.Equal(1,qb.RushAttempts);Assert.Equal(0,qb.Dropbacks);Assert.Equal(0,box.PlayerStats.Sum(p=>p.PassBlockSnaps+p.RunBlockSnaps));Assert.Equal(1,box.HomeStats!.PassAttempts);
    }
    [Fact] public void PenaltiesAndSpecialTeamMetricsComeFromTheRecordedPlay()
    {
        var s=GameDayEngineTests.Session(GameDayMode.Play);var kicker=s.HomeRoster.First(p=>p.Position==Position.QB);var ol=s.HomeRoster.First(p=>p.Position==Position.OL);
        s.Events.Add(new(){StatsVersion=1,OffenseTeamName="Home",DefenseTeamName="Away",OffensivePlayId="",DefensivePlayId="",Outcome=SnapOutcome.FieldGoal,KickerId=kicker.PlayerId,StartYardLine=60});
        s.Events.Add(new(){StatsVersion=1,OffenseTeamName="Home",DefenseTeamName="Away",OffensivePlayId="",DefensivePlayId="",Outcome=SnapOutcome.Punt,KickerId=kicker.PlayerId,StartYardLine=40,KickLandingYardLine=96,IsTouchback=true});
        s.Events.Add(new(){StatsVersion=1,OffenseTeamName="Home",DefenseTeamName="Away",OffensivePlayId="",DefensivePlayId="",Outcome=SnapOutcome.Penalty,PenalizedPlayerId=ol.PlayerId,PenalizedTeamName="Home",PenaltyYards=5});
        s.IsComplete=true;var box=GameDayEngine.Finish(s);var k=box.PlayerStats.Single(p=>p.PlayerId==kicker.PlayerId);Assert.Equal(57,k.LongFieldGoal);Assert.Equal(1,k.FieldGoalsMade50Plus);Assert.Equal(40,k.NetPuntYards);Assert.Equal(1,k.PuntTouchbacks);
        Assert.Equal(5,box.PlayerStats.Single(p=>p.PlayerId==ol.PlayerId).PenaltyYards);
    }
}
