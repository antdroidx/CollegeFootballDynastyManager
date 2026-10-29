using System.Text.Json;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Xunit;
namespace DynastyManager.Core.Tests;
public class FootballEfficiencyTests
{
    static GameTeamStats Stats(int yards=300)=>new(){PassAttempts=30,RushAttempts=30,PassYards=yards-100,RushYards=100,SacksAllowed=3,PassingTouchdowns=1,Turnovers=1};
    static ScheduledGame Game(string id,string home,string away,int h=300,int a=300)=>new(){GameId=id,HomeTeamName=home,AwayTeamName=away,HasPlayed=true,HomeScore=7,AwayScore=7,HomeStats=Stats(h),AwayStats=Stats(a)};
    static GameSnapEvent Snap(int down,int distance,int yards,SnapOutcome outcome=SnapOutcome.Run)=>new(){OffenseTeamName="A",DefenseTeamName="B",OffensivePlayId="run",DefensivePlayId="base",Down=down,Distance=distance,Yards=yards,Outcome=outcome};
    [Fact] public void ContextUsesDownDistanceAndExcludesSpecialTeamsAndClockManagement()
    {
        var result=FootballEfficiency.Capture([Snap(1,10,4),Snap(2,10,5),Snap(2,10,6),Snap(3,3,3),Snap(3,3,2),Snap(1,10,40,SnapOutcome.Fumble),Snap(1,10,-7,SnapOutcome.Sack),Snap(1,10,60,SnapOutcome.Kickoff),Snap(1,1,1,SnapOutcome.Kneel),Snap(1,1,0,SnapOutcome.Spike)]);
        Assert.Equal(7,result.Plays);Assert.Equal(3,result.Successful);Assert.Equal(2,result.ThirdDowns);Assert.Equal(1,result.ThirdDownConversions);Assert.Equal(7,result.SackYardsLost);
    }
    [Fact] public void BalancedLeagueHasZeroAdjustmentAndSacksAreNotDoubleCounted()
    {
        var rows=FootballEfficiency.Build([Game("1","A","B"),Game("2","A","C"),Game("3","B","C")]);
        Assert.Equal(3,rows.Count);foreach(var row in rows){Assert.Equal(120,row.Plays);Assert.Equal(5,row.YardsPerPlay);Assert.Equal(0,row.Net,8);Assert.Null(row.SuccessRate);}
        Assert.Equal(100d*6/66,rows[0].SackRate,8);
    }
    [Fact] public void MissingUnplayedAndDuplicateGamesDoNotInflateSamples()
    {
        var game=Game("1","A","B");var rows=FootballEfficiency.Build([game,game,game with{GameId="2",HasPlayed=false},game with{GameId="3",HomeStats=null}]);Assert.All(rows,x=>Assert.Equal(1,x.Games));
        Assert.Empty(FootballEfficiency.Build([game with{HomeStats=new(),AwayStats=new()}]));
    }
    [Fact] public void BetterOpponentMakesSameOffenseMoreValuableAndGoodDefenseIsPositive()
    {
        var vsStrong=FootballEfficiency.Build([Game("1","A","B",300,300),Game("2","C","B",120,300)]).Single(x=>x.Team=="A");
        var vsWeak=FootballEfficiency.Build([Game("1","A","B",300,300),Game("2","C","B",480,300)]).Single(x=>x.Team=="A");Assert.True(vsStrong.Offense>vsWeak.Offense);
        var defense=FootballEfficiency.Build([Game("1","A","B",300,120),Game("2","B","C",480,300)]).Single(x=>x.Team=="A");Assert.True(defense.Defense>0);
    }
    [Fact] public void OptionalContextRoundTripsWithoutFabricatingOldSaveRates()
    {
        var game=Game("1","A","B");var old=JsonSerializer.Deserialize<ScheduledGame>(JsonSerializer.Serialize(game))!;Assert.Null(old.HomeStats!.Efficiency);
        game=game with{HomeStats=Stats() with{Efficiency=new(10,50,4,3,1,0)}};var saved=JsonSerializer.Deserialize<ScheduledGame>(JsonSerializer.Serialize(game))!;Assert.Equal(40,FootballEfficiency.Build([saved]).Single(x=>x.Team=="A").SuccessRate);Assert.Equal(10,saved.HomeStats!.Efficiency!.Plays);
    }
    [Fact] public void PlayerRatesUseOpportunityWeightedSamePositionComparisonsAndSampleFloor()
    {
        var id=Guid.NewGuid();var other=Guid.NewGuid();PlayerSeasonStatLine Line(Guid who,int attempts,int yards,Position pos=Position.QB)=>new(){PlayerId=who,PlayerName="Player",TeamName="A",Position=pos,PassAttempts=attempts,PassYards=yards};
        Assert.Empty(PlayerEfficiency.Build([Line(id,19,190)],id));
        var result=Assert.Single(PlayerEfficiency.Build([Line(id,20,200),Line(other,40,200),Line(Guid.NewGuid(),100,9999,Position.WR)],id));Assert.Equal(10,result.Value);Assert.Equal(400d/60,result.PeerAverage,8);Assert.Equal(1,result.Rank);Assert.Equal(2,result.Players);
    }
    [Theory][InlineData(1,1,1)][InlineData(.7,.7,.7)][InlineData(.1,.2,.3)][InlineData(0,.42,.44)]
    public void ContrastCorrectionMeetsNormalTextThreshold(double br,double bg,double bb)
    {
        foreach(var c in new[]{(1d,1d,1d),(.5,.6,.7),(0d,.7,.4),(.9,.7,.1)}){var ink=DynastyManager.App.ColorContrast.Ensure(c.Item1,c.Item2,c.Item3,br,bg,bb);Assert.True(DynastyManager.App.ColorContrast.Ratio(ink.R,ink.G,ink.B,br,bg,bb)>=4.5-1e-8);}
    }
}
