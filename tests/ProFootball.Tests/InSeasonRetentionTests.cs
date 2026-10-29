using DynastyManager.Core.Models;
using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public class InSeasonRetentionTests
{
    static ProFranchise State()
    {
        var s=ProLeagueService.Create("Seattle Seahawks");var p=s.Players.First(p=>p.Football.TeamName==s.UserTeam&&p.Age>28&&!p.PracticeSquad&&!p.Reserve);
        return s with{Phase=ProPhase.RegularSeason,Week=5,SalaryCap=2_000_000_000,TeamCapAllocations=[],Contracts=s.Contracts.Where(c=>c.PlayerId!=p.Football.PlayerId).Append(new(p.Football.PlayerId,s.UserTeam,2024,2,0,[new(2026,5_000_000,0)])).ToList()};
    }
    [Fact]public void VeteranCanExtendInSeasonWithoutReplacingCurrentSalary()
    {
        var s=State();var p=ProRetention.Expiring(s,s.UserTeam).First(p=>p.Age>28&&s.Contracts.Any(c=>c.PlayerId==p.Football.PlayerId&&c.SignedYear==2024));
        Assert.Null(ProRetention.ExtensionIssue(s,p));var c=s.Contracts.Last(c=>c.Active&&c.PlayerId==p.Football.PlayerId);var terms=ProOfferTerms.AgentDemand(s,p,true);
        var next=ProEconomy.Extend(s,s.UserTeam,p.Football.PlayerId,terms.Years,terms.Salary,0);var deal=next.Contracts.Last(c=>c.Active&&c.PlayerId==p.Football.PlayerId);
        Assert.Equal(c.Years.Single(y=>y.Year==s.Year).Salary,deal.Years.Single(y=>y.Year==s.Year).Salary);Assert.True(ProContractInsights.PlayingEnd(deal)>s.Year);
        Assert.Contains("Already changed",ProRetention.ExtensionIssue(next,p));
    }
    [Theory][InlineData(true,2027)][InlineData(false,2026)]public void RookieWindowUsesCompletedRegularSeasons(bool drafted,int eligibleYear)
    {
        var s=State();var p=s.Players.First(p=>p.Football.TeamName==s.UserTeam) with{RookieYear=2025,DraftOverall=drafted?20:null,Experience=1};
        s=s with{Players=s.Players.Select(x=>x.Football.PlayerId==p.Football.PlayerId?p:x).ToList(),Contracts=[new(p.Football.PlayerId,s.UserTeam,2025,4,0,Enumerable.Range(2025,drafted?4:3).Select(y=>new ContractYear(y,1_500_000,0)).ToList()){OriginalRookieYear=2025}]};
        Assert.Contains("Rookie deal",ProRetention.ExtensionIssue(s,p));
        Assert.Null(ProRetention.ExtensionIssue(s with{Year=eligibleYear,Phase=ProPhase.Playoffs},p));
    }
    [Fact]public void StaffCanNegotiateInSeasonButRespectsLetExpire()
    {
        var s=State();var p=ProRetention.Expiring(s,s.UserTeam).First(p=>p.Age>28&&s.Contracts.Any(c=>c.PlayerId==p.Football.PlayerId&&c.SignedYear==2024));
        s=s with{LetExpire=[p.Football.PlayerId]};var held=ProRetention.Negotiate(s,s.UserTeam);Assert.Contains(ProRetention.Expiring(held,s.UserTeam),x=>x.Football.PlayerId==p.Football.PlayerId);
        var next=ProRetention.Negotiate(s with{LetExpire=[]},s.UserTeam);Assert.DoesNotContain(ProRetention.Expiring(next,s.UserTeam),x=>x.Football.PlayerId==p.Football.PlayerId);
    }
    [Fact]public async Task ExportInSeasonUiFixture()
    {
        var s=State() with{Name="Synthetic in-season retention QA",Id=Guid.Parse("de101000-0000-0000-0000-000000000001")};
        var dir=Path.Combine(Path.GetTempPath(),"retention-ui-"+Guid.NewGuid());
        try{var store=new ProSaveStore(dir);await store.Save(s);Assert.Equal(ProPhase.RegularSeason,(await store.Load(s.Id)).Phase);}finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
        if(Environment.GetEnvironmentVariable("PRO_COORDINATOR_UI_FIXTURES") is string output){using var file=File.Create(Path.Combine(output,"pro-staff-inseason-ui-fixture.json.gz"));using var gzip=new System.IO.Compression.GZipStream(file,System.IO.Compression.CompressionLevel.SmallestSize);System.Text.Json.JsonSerializer.Serialize(gzip,s,new System.Text.Json.JsonSerializerOptions{PropertyNamingPolicy=System.Text.Json.JsonNamingPolicy.CamelCase});}
    }
}
