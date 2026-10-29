using DynastyManager.Core.Models;
using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public class StandingsTiebreakTests
{
    static ProFranchise League() => new() { UserTeam="A Club", Year=2026, League=new(1,"Test",53,16,Enumerable.Range(0,8).Select(i=>new ProTeam(((char)('A'+i)).ToString(),((char)('A'+i)).ToString(),"Club",((char)('A'+i)).ToString(),"AFC",(i/2).ToString(),i%2,"#000000","#ffffff")).ToList()) };
    static ProGame Game(string home,string away,int hs,int aws,int week=1,int year=2026,ProPhase phase=ProPhase.RegularSeason) => new(new ScheduledGame { GameId=$"{year}-{week}-{home}-{away}",SeasonYear=year,Week=week,HomeTeamName=home+" Club",AwayTeamName=away+" Club",HasPlayed=true,HomeScore=hs,AwayScore=aws },phase);
    static string[] Seeds(ProFranchise s)=>ProStandings.Seeds(s,"AFC").Select(r=>r.Team.Id).ToArray();
    [Fact] public void DivisionHeadToHeadBeatsScoringMarginAndTieCountsHalf()
    {
        var s=League() with { Games=[Game("B","A",10,7),Game("A","C",50,0),Game("B","D",0,20),Game("A","E",10,10),Game("B","F",10,10)] };
        var rows=ProStandings.Build(s);Assert.Equal(.5,rows.Single(r=>r.Team.Id=="A").Percentage);Assert.Equal(1,rows.Single(r=>r.Team.Id=="A").Ties);
        Assert.Contains("B",Seeds(s).Take(4));Assert.DoesNotContain("A",Seeds(s).Take(4));
    }
    [Fact] public void CrossDivisionHeadToHeadPrecedesDivisionPercentage()
    {
        var s=League() with { Games=[Game("C","A",10,7),Game("A","B",10,0),Game("C","E",0,10)] };
        Assert.True(Array.IndexOf(Seeds(s),"C")<Array.IndexOf(Seeds(s),"A"));
    }
    [Fact] public void OldSeasonAndPreseasonResultsCannotChangeSeeds()
    {
        var s=League() with { Games=[Game("A","B",10,0),Game("C","D",7,0)] };var expected=Seeds(s);
        var other=s with { Games=s.Games.Concat(new[]{Game("B","A",70,0,year:2025),Game("D","C",80,0,phase:ProPhase.Preseason)}).ToList() };
        Assert.Equal(expected,Seeds(other));Assert.Equal(expected,Seeds(s));
    }
    [Fact] public void MultiClubReductionRestartsHeadToHead()
    {
        var s=League() with { Games=[Game("A","C",10,0),Game("C","E",10,0),Game("E","A",10,0),Game("A","B",10,0),Game("C","D",10,0),Game("E","F",0,10)] };
        // A/C are both 2-1; direct A win must decide their home-field order.
        Assert.True(Array.IndexOf(Seeds(s),"A")<Array.IndexOf(Seeds(s),"C"));
    }
    [Fact] public void SevenUniqueQualifiersAndStableFinalCoinToss()
    {
        var s=League();var seeds=Seeds(s);Assert.Equal(7,seeds.Length);Assert.Equal(7,seeds.Distinct().Count());
        Assert.Equal(seeds,Seeds(s with { League=s.League with { Teams=s.League.Teams.AsEnumerable().Reverse().ToList() } }));
    }
    [Fact] public void DivisionCommonOpponentsPrecedeOverallMargin()
    {
        var s=League() with{Games=[Game("A","B",7,0),Game("B","A",50,0,2),Game("A","C",7,0),Game("A","E",0,60),Game("B","C",0,1),Game("B","G",60,0)]};
        Assert.Contains("A",Seeds(s).Take(4));Assert.DoesNotContain("B",Seeds(s).Take(4));
    }
    [Fact] public void StrengthOfVictoryPrecedesScoringForDivisionWinners()
    {
        var s=League() with{Games=[Game("A","E",7,0),Game("A","F",0,10),Game("C","G",50,0),Game("C","H",0,1),Game("E","D",7,0),Game("E","D",7,0,2),Game("G","B",7,0)]};
        Assert.True(Array.IndexOf(Seeds(s),"A")<Array.IndexOf(Seeds(s),"C"));
    }
    [Fact] public void ThreeClubHeadToHeadSweepBeatsPoints()
    {
        var s=League() with{Games=[Game("A","C",7,0),Game("A","E",7,0),Game("A","G",0,60),Game("C","B",60,0),Game("C","D",60,0),Game("E","B",60,0),Game("E","D",60,0)]};
        Assert.Equal("A",Seeds(s)[1]);
    }

}
