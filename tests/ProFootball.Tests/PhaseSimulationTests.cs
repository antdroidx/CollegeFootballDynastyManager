using DynastyManager.Core.Models;
using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public class PhaseSimulationTests
{
    static ProFranchise Fixture(ProPhase phase)
    {
        var s=ProLeagueService.Create("Seattle Seahawks") with{SalaryCap=2_000_000_000,TeamCapAllocations=[],Phase=phase,Week=phase==ProPhase.Preseason?3:phase==ProPhase.RegularSeason?18:4,Name="Synthetic phase "+phase};
        s=ProCpu.Trim(s,s.UserTeam);
        s=s with{Games=s.Games.Select(g=>g with{Football=g.Football with{HasPlayed=true,HomeScore=21,AwayScore=14}}).ToList()};
        if(phase==ProPhase.Playoffs)s=s with{Games=s.Games.Append(new(new ScheduledGame{GameId="phase-final",SeasonYear=s.Year,Week=4,HomeTeamName=s.UserTeam,AwayTeamName="Tennessee Titans",HasPlayed=true,HomeScore=21,AwayScore=14},ProPhase.Playoffs,4)).ToList()};
        return s;
    }
    [Theory][InlineData(ProPhase.Preseason,ProPhase.RegularSeason)][InlineData(ProPhase.RegularSeason,ProPhase.Playoffs)][InlineData(ProPhase.Playoffs,ProPhase.Offseason)]
    public async Task PhaseBoundariesUseNormalWeeklyAdvanceAndPersist(ProPhase start,ProPhase target)
    {
        var s=Fixture(start);var next=ProLeagueService.Advance(s);Assert.Equal(target,next.Phase);Assert.Equal(s.Year,next.Year);
        var dir=Path.Combine(Path.GetTempPath(),"phase-"+Guid.NewGuid());
        try{var store=new ProSaveStore(dir);await store.Save(next);Assert.Equal(target,(await store.Load(next.Id)).Phase);}finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
        if(Environment.GetEnvironmentVariable("PRO_COORDINATOR_UI_FIXTURES") is string output){using var file=File.Create(Path.Combine(output,"pro-staff-phase-"+start+"-ui-fixture.json.gz"));using var gzip=new System.IO.Compression.GZipStream(file,System.IO.Compression.CompressionLevel.SmallestSize);System.Text.Json.JsonSerializer.Serialize(gzip,s,new System.Text.Json.JsonSerializerOptions{PropertyNamingPolicy=System.Text.Json.JsonNamingPolicy.CamelCase});}
    }
    [Fact]public void CapFailureCannotBeSkippedByWeeklyAdvance()
    {
        var s=Fixture(ProPhase.RegularSeason) with{SalaryCap=1};Assert.Throws<InvalidOperationException>(()=>ProLeagueService.Advance(s));Assert.Equal(18,s.Week);Assert.Equal(ProPhase.RegularSeason,s.Phase);
    }
}
