using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public class StatisticsPresentationTests
{
    [Fact] public async Task CompletedGameTracksAllUnitsAndExportsRealLoaderFixture()
    {
        var s=ProLeagueService.Create("Seattle Seahawks") with {Name="Statistics QA",Phase=ProPhase.RegularSeason,Week=1};
        var g=s.Games.First(g=>g.Phase==ProPhase.RegularSeason&&(g.Football.HomeTeamName==s.UserTeam||g.Football.AwayTeamName==s.UserTeam));
        var session=ProLeagueService.Start(s,g,GameDayMode.Simulate);GameDayEngine.SimulateToEnd(session);var result=GameDayEngine.Finish(session);
        Assert.Contains(result.PlayerStats,p=>p.PassBlockSnaps>0);Assert.Contains(result.PlayerStats,p=>p.Targets>0);Assert.Contains(result.PlayerStats,p=>p.DefensiveSnaps>0);
        Assert.Equal(result.PlayerStats.Sum(p=>p.TimesSacked),result.HomeStats!.SacksAllowed+result.AwayStats!.SacksAllowed);
        Assert.Equal(result.PlayerStats.Sum(p=>p.Targets),result.PlayerStats.Sum(p=>p.CoverageTargets));
        s=s with{Games=s.Games.Select(x=>x.Football.GameId==g.Football.GameId?x with{Football=result}:x).ToList()};
        // Distinct synthetic playoff result exposes accidental phase mixing in native UI checks.
        s=s with{Games=s.Games.Append(new ProGame(result with{GameId="statistics-playoff-fixture",PlayerStats=result.PlayerStats.Select(p=>p with{PassYards=p.PassYards+111}).ToArray()},ProPhase.Playoffs)).ToList()};
        var dir=Path.Combine(Path.GetTempPath(),"statistics-ui-"+Guid.NewGuid());
        try{var store=new ProSaveStore(dir);await store.Save(s);var loaded=await store.Load(s.Id);Assert.Equal(2*result.PlayerStats.Sum(p=>p.PassBlockSnaps),loaded.Games.SelectMany(x=>x.Football.PlayerStats).Sum(p=>p.PassBlockSnaps));}finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
        if(Environment.GetEnvironmentVariable("PRO_COORDINATOR_UI_FIXTURES") is string output){using var file=File.Create(Path.Combine(output,"pro-staff-statistics-ui-fixture.json.gz"));using var gzip=new System.IO.Compression.GZipStream(file,System.IO.Compression.CompressionLevel.SmallestSize);System.Text.Json.JsonSerializer.Serialize(gzip,s,new System.Text.Json.JsonSerializerOptions{PropertyNamingPolicy=System.Text.Json.JsonNamingPolicy.CamelCase});}
    }
}
