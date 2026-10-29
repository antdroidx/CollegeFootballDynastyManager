using System.Text.Json;
using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public class CapDisplayTests
{
    static ProFranchise Camp()
    {
        var s=ProLeagueService.Create("Seattle Seahawks");var p=s.Players.First();
        var players=Enumerable.Range(0,52).Select(_=>p with{Age=27,Experience=4,CreditedSeasons=4,RookieYear=2022,DraftOverall=100,ExternalId="",Reserve=false,ReserveInjury=null,Retired=false,PracticeSquad=false,Football=p.Football with{PlayerId=Guid.NewGuid(),TeamName=s.UserTeam,CurrentInjury=null}}).ToArray();
        return s with{Phase=ProPhase.Preseason,Week=1,CapRulesVersion=2,SalaryCap=77_500_000,TeamCapAllocations=[],DeadCap=[],Bids=[],RolloverElections=[],
            Players=s.Players.Where(x=>x.Football.TeamName!=s.UserTeam).Concat(players).ToList(),
            Contracts=s.Contracts.Where(c=>c.Team!=s.UserTeam).Concat(players.Select(x=>new ProContract(x.Football.PlayerId,s.UserTeam,s.Year,1,0,[new(s.Year,1_500_000,0)]){Estimated=false})).ToList()};
    }
    [Theory][InlineData(ProPhase.Preseason)][InlineData(ProPhase.RegularSeason)][InlineData(ProPhase.Playoffs)][InlineData(ProPhase.Offseason)]
    public void OneSeasonFigureAcrossPhasesWithoutChangingTransactionAccounting(ProPhase phase)
    {
        var s=Camp() with{Phase=phase};var before=JsonSerializer.Serialize(s);var displayed=ProCapDisplay.Budget(s,s.UserTeam);
        Assert.Equal(-500_000,displayed.Available);Assert.Equal(0,displayed.OffseasonSalaryExcluded);Assert.Equal(78_000_000,displayed.PlayerCommitments);Assert.Equal(before,JsonSerializer.Serialize(s));
        if(phase is ProPhase.Preseason or ProPhase.Offseason&&s.OffseasonStep!=OffseasonStep.Retention)Assert.Equal(1_000_000,ProEconomy.Room(s,s.UserTeam));
        var alert=Assert.Single(ProInbox.Build(s),n=>n.Id.StartsWith("cap-"));Assert.Equal("Over season salary cap",alert.Title);Assert.Contains("$500,000",alert.Detail);Assert.Equal("Front Office",alert.Section);Assert.DoesNotContain(ProInbox.Build(s),n=>n.Id.StartsWith("opening-cap-"));
    }
    [Fact]public void PositiveAndZeroBalancesDoNotProduceCapAlertsAndPendingOffersStaySeparate()
    {
        var s=Camp() with{SalaryCap=80_000_000};var original=ProCapDisplay.Room(s,s.UserTeam);s=s with{Bids=[new(Guid.NewGuid(),s.UserTeam,1,1_000_000,0,0)]};
        Assert.Equal(original,ProCapDisplay.Room(s,s.UserTeam));Assert.Equal(1_000_000,ProEconomy.Pending(s,s.UserTeam));Assert.Null(ProCapDisplay.ActionNeeded(s,s.UserTeam));
        Assert.Null(ProCapDisplay.ActionNeeded(s with{SalaryCap=78_000_000},s.UserTeam));
    }
    [Fact]public async Task ExportedCapDisplayFixtureLoadsWithSeasonDeficitDespiteCampRoom()
    {
        var s=Camp() with{Name="Synthetic season cap QA",Id=Guid.Parse("ca900000-0000-0000-0000-000000000001")};var dir=Path.Combine(Path.GetTempPath(),"cap-display-"+Guid.NewGuid());
        try{var store=new ProSaveStore(dir);await store.Save(s);var loaded=await store.Load(s.Id);Assert.Equal(-500_000,ProCapDisplay.Room(loaded,s.UserTeam));Assert.Equal(1_000_000,ProEconomy.Room(loaded,s.UserTeam));}
        finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
        if(Environment.GetEnvironmentVariable("PRO_COORDINATOR_UI_FIXTURES") is string output)
        {
            using var file=File.Create(Path.Combine(output,"pro-staff-cap-display-ui-fixture.json.gz"));using var gzip=new System.IO.Compression.GZipStream(file,System.IO.Compression.CompressionLevel.SmallestSize);
            JsonSerializer.Serialize(gzip,s,new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.CamelCase});
        }
    }
}
