using System.Text.Json;
using System.IO.Compression;
using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public sealed class TerminationDeviceFixtures
{
    [Fact] public async Task DeviceSavesRoundTripAndExpectedTransactionsUseTheProductionRules()
    {
        var template=ProLeagueService.Create("Seattle Seahawks") with{SalaryCap=900_000_000,TeamCapAllocations=[],Games=[],Bids=[],Waivers=[],DeadCap=[]};
        var player=template.Players.First(p=>p.Football.TeamName==template.UserTeam&&!p.Reserve&&!p.PracticeSquad);
        var releaseContract=new ProContract(player.Football.PlayerId,template.UserTeam,2025,5,10_000_000,Enumerable.Range(2026,4).Select(y=>new ContractYear(y,10_000_000,0)).ToList()){LastChangedYear=2025};
        var release=template with{Id=Guid.NewGuid(),Name="Synthetic June1 QA",Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.FreeAgency,Contracts=template.Contracts.Select(c=>c.PlayerId==player.Football.PlayerId?releaseContract:c).ToList(),Players=template.Players.Select(p=>p==player?p with{Experience=6,Football=p.Football with{FullName="June1 Release",CurrentInjury=null}}:p).ToList()};
        var released=ProEconomy.Release(release,release.UserTeam,player.Football.PlayerId,false,true);
        Assert.Contains(released.DeadCap,d=>d.June1Hold);Assert.True(ProFinance.Budget(released,release.UserTeam).TotalCommitments>=ProFinance.Budget(release,release.UserTeam).TotalCommitments);
        var candidate=template.Players.First(p=>p.Football.TeamName!=template.UserTeam&&!p.Reserve&&!p.PracticeSquad);
        candidate=candidate with{Experience=2,RookieYear=2025,DraftOverall=12,Football=candidate.Football with{FullName="Waiver Rookie",CurrentInjury=null}};
        var contract=new ProContract(candidate.Football.PlayerId,candidate.Football.TeamName,2025,4,12_000_000,[new(2026,6_000_000,3_000_000),new(2027,8_000_000,5_000_000),new(2028,10_000_000,0)]){OriginalRookieYear=2025,LastChangedYear=2025};
        var waiver=template with{Id=Guid.NewGuid(),Name="Synthetic waiver QA",Phase=ProPhase.Preseason,Week=1,Players=template.Players.Select(p=>p.Football.PlayerId==candidate.Football.PlayerId?candidate:p).ToList(),Contracts=template.Contracts.Select(c=>c.PlayerId==candidate.Football.PlayerId?contract:c).ToList()};
        waiver=ProEconomy.Release(waiver,candidate.Football.TeamName,candidate.Football.PlayerId);
        var awarded=ProRoster.ResolveWaivers(ProRoster.Claim(waiver,waiver.UserTeam,candidate.Football.PlayerId) with{TransactionTick=1});
        var assumed=Assert.Single(awarded.Contracts,c=>c.Active&&c.PlayerId==candidate.Football.PlayerId);Assert.Equal(2028,ProContractInsights.PlayingEnd(assumed));Assert.Equal(2025,assumed.LastChangedYear);
        var root=Path.Combine(Path.GetTempPath(),"termination-fixtures-"+Guid.NewGuid());
        try{var store=new ProSaveStore(root);foreach(var save in new[]{release,waiver}){await store.Save(save);var loaded=await store.Load(save.Id);Assert.Equal(JsonSerializer.Serialize(save.Contracts),JsonSerializer.Serialize(loaded.Contracts));Assert.Equal(JsonSerializer.Serialize(save.Waivers),JsonSerializer.Serialize(loaded.Waivers));}}
        finally{if(Directory.Exists(root))Directory.Delete(root,true);}
        if(Environment.GetEnvironmentVariable("PRO_COORDINATOR_UI_FIXTURES") is string output)
        {
            using var file=File.Create(Path.Combine(output,"pro-staff-termination-ui-fixture.json.gz"));using var gzip=new GZipStream(file,CompressionLevel.SmallestSize);
            JsonSerializer.Serialize(gzip,new{release,releasePlayer=player.Football.PlayerId,releaseDebt=released.DeadCap,waiver,waiverPlayer=candidate.Football.PlayerId,assumed},new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.CamelCase});
        }
    }
}
