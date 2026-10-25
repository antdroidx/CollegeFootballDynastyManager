using System.Text.Json;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;
namespace DynastyManager.Core.Tests;

public sealed class PenaltyPresentationTests
{
    private static (GameDaySession Session, GameSnapEvent Snap) Foul(bool offense, string possession, int spot, int distance=7)
    {
        for(var seed=1;seed<=1000;seed++)
        {
            var s=GameDayEngineTests.Session(GameDayMode.Play,includeOpeningKickoff:true,seed:seed);
            GameDayEngine.Step(s); s.PossessionTeamName=possession;s.YardLine=spot;s.Down=3;s.Distance=distance;s.ClockStopped=true;
            var snap=GameDayEngine.Step(s,"off-spread-zone");
            if(snap.Outcome==SnapOutcome.Penalty && (snap.PenalizedTeamName==possession)==offense)return(s,snap);
        }
        throw new InvalidOperationException("No seeded foul found");
    }
    [Theory]
    [InlineData("Home",true)] [InlineData("Home",false)]
    [InlineData("Away",true)] [InlineData("Away",false)]
    public void PenaltyNamesTheOffendingTeamRatherThanTheBeneficiary(string possession,bool offense)
    {
        var(s,snap)=Foul(offense,possession,50);
        var expectedTeam=offense?possession:possession=="Home"?"Away":"Home";
        Assert.Equal(expectedTeam,snap.PenalizedTeamName);Assert.Equal(5,snap.PenaltyYards);
        Assert.Equal(offense?45:55,s.YardLine);Assert.Equal(offense?12:2,s.Distance);Assert.Equal(3,s.Down);
        Assert.False(GameDayEngine.IsScrimmage(snap));Assert.Equal(0,snap.Yards);
        var display=GameDayCommentaryService.Penalty(snap)!;
        Assert.Equal(expectedTeam,display.TeamName);Assert.Equal(offense?"False start":"Offside",display.Foul);
        Assert.Equal($"PENALTY\n5 yards {display.Foul}\n{expectedTeam}",display.Banner);
        Assert.Equal(display.Commentary,GameDayCommentaryService.Describe(snap,s.PlayersById));
    }
    [Theory]
    [InlineData(1,true,0)] [InlineData(3,true,1)] [InlineData(4,true,2)]
    [InlineData(99,false,0)] [InlineData(97,false,1)] [InlineData(96,false,2)]
    public void WholeYardHalfDistanceEnforcementNeverCrossesTheGoalLine(int spot,bool offense,int enforced)
    {
        var(s,snap)=Foul(offense,"Home",spot);
        Assert.InRange(s.YardLine,1,99);Assert.Equal(enforced,snap.PenaltyYards);
        Assert.Equal(enforced,Math.Abs(snap.BallEndYardLine-snap.StartYardLine));
        Assert.Equal(enforced,GameDayCommentaryService.Penalty(snap)!.Yards);
        Assert.Equal(0,s.HomeScore+s.AwayScore);
    }
    [Fact]
    public void DefensivePenaltyCanGainFirstDownWithoutInventingAPlay()
    {
        var(s,snap)=Foul(false,"Home",50,3);
        Assert.Equal(55,s.YardLine);Assert.Equal(1,s.Down);Assert.Equal(10,s.Distance);
        Assert.All(s.Events,e=>Assert.False(GameDayEngine.IsScrimmage(e)));
    }
    [Fact]
    public void ExplicitPenaltyDetailsSurviveSaveAndMatchRequestedWashingtonBanner()
    {
        var snap=new GameSnapEvent {OffenseTeamName="Michigan",DefenseTeamName="Washington",OffensivePlayId="",DefensivePlayId="",
            Outcome=SnapOutcome.Penalty,StartYardLine=40,BallEndYardLine=45,PenalizedTeamName="Washington",PenaltyName="Offside",PenaltyYards=5};
        var restored=JsonSerializer.Deserialize<GameSnapEvent>(JsonSerializer.Serialize(snap))!;
        Assert.Equal("PENALTY\n5 yards Offside\nWashington",GameDayCommentaryService.Penalty(restored)!.Banner);
        Assert.Equal("Offside on Washington: 5 yards.",GameDayCommentaryService.Describe(restored,new Dictionary<Guid,DynastyPlayer>()));
        Assert.Equal("Washington",GameDayCommentaryService.Penalty(snap with {PenalizedTeamName=null,PenaltyName=null,PenaltyYards=null,Description="Defensive offside: 5 yards."})!.TeamName);
        Assert.Null(GameDayCommentaryService.Penalty(snap with {Outcome=SnapOutcome.Run}));
    }
}
