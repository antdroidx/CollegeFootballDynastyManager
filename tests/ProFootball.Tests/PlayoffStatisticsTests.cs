using DynastyManager.Core.Models;
using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public class PlayoffStatisticsTests
{
    [Fact] public void SeasonCareerAndCompetitionStaySeparateWithoutArchiveDuplicates()
    {
        var id=Guid.NewGuid();
        ProGame Game(string game,int year,ProPhase phase,int yards,bool played=true)=>new(new ScheduledGame{GameId=game,SeasonYear=year,Week=1,HomeTeamName="A",AwayTeamName="B",HasPlayed=played,PlayerStats=[new(){PlayerId=id,PlayerName="Quarterback",TeamName="A",SeasonYear=year,Games=1,PassYards=yards,PassAttempts=1,TrackedGames=1,LongestPass=yards}]},phase);
        var regular=Game("regular",2026,ProPhase.RegularSeason,10);var playoff=Game("playoff",2026,ProPhase.Playoffs,20);
        var s=new ProFranchise{League=new(1,"Test",53,0,[]),UserTeam="A",Year=2026,Games=[regular,playoff,Game("pre",2026,ProPhase.Preseason,900),Game("pending",2026,ProPhase.Playoffs,800,false)],History=[new(2026,"A",[regular,playoff]),new(2025,"A",[Game("regular",2025,ProPhase.RegularSeason,30),Game("playoff",2025,ProPhase.Playoffs,40)])]};
        Assert.Equal(10,ProPlayerStatistics.Totals(s,false,false).Single().PassYards);
        Assert.Equal(20,ProPlayerStatistics.Totals(s,true,false).Single().PassYards);
        Assert.Equal(40,ProPlayerStatistics.Totals(s,false,true).Single().PassYards);
        var career=ProPlayerStatistics.Totals(s,true,true).Single();Assert.Equal(60,career.PassYards);Assert.Equal(2,career.Games);Assert.Equal(40,career.LongestPass);
        Assert.Empty(ProPlayerStatistics.Totals(s,true,true,"B"));
        Assert.Empty(ProPlayerStatistics.Lines(s with{Year=2027},true,false));
    }
}
