using ProFootball.Core;
using DynastyManager.Core.Models;
using Xunit;
namespace ProFootball.Tests;
public class CompensatoryDraftTests
{
    static ProFranchise New()=>ProLeagueService.Create("Seattle Seahawks");
    [Fact]public void OpeningProjectionsAreSeparateFromTradableInventory()
    {
        var s=New();var rows=ProCompensatoryDraft.Project(s,2027);
        Assert.Equal(32,rows.Count(x=>!x.Special));Assert.Single(rows,x=>x.Special);
        Assert.Equal(5,rows.Count(x=>x.Team==s.UserTeam));Assert.Single(ProCompensatoryDraft.Project(s,2028),x=>x.Special&&x.Team==s.UserTeam);
        Assert.DoesNotContain(s.Picks,p=>p.Compensatory);Assert.Equal(672,s.Picks.Count);
    }
    [Fact]public void AwardsSurviveRegenerationAndIncludeCorrectOverallNumbers()
    {
        var s=ProDraft.Generate(New() with{Year=2027,Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.FreeAgency});
        if(Environment.GetEnvironmentVariable("PRO_COORDINATOR_UI_FIXTURES") is string output){Directory.CreateDirectory(output);var fixture=s with{Name="CompensatoryDraftQA"};using var file=System.IO.File.Create(Path.Combine(output,"pro-staff-compensatory-ui-fixture.json.gz"));using var zip=new System.IO.Compression.GZipStream(file,System.IO.Compression.CompressionLevel.SmallestSize);System.Text.Json.JsonSerializer.Serialize(zip,fixture,new System.Text.Json.JsonSerializerOptions{PropertyNamingPolicy=System.Text.Json.JsonNamingPolicy.CamelCase});}
        var picks=s.Picks.Where(p=>p.Year==2027).ToArray();Assert.Equal(257,picks.Length);Assert.Equal(33,picks.Count(p=>p.Compensatory));
        Assert.Equal(picks.Length,picks.Select(p=>(p.Round,p.Slot)).Distinct().Count());
        Assert.All(picks.Where(p=>p.Compensatory),p=>Assert.True(p.Slot>32));
        var third=picks.Where(p=>p.Round==3).OrderBy(p=>p.Slot).ToArray();Assert.True(third[^1].SpecialCompensatory);
        var fourth=picks.First(p=>p.Round==4&&p.Slot==1);Assert.Equal(102,ProDraft.Overall(s,fourth));
        var comp=picks.First(p=>p.Compensatory);s=s with{Picks=s.Picks.Select(p=>p.Id==comp.Id?p with{Owner="Miami Dolphins"}:p).ToList()};
        s=ProDraft.Generate(s);Assert.Equal("Miami Dolphins",s.Picks.Single(p=>p.Id==comp.Id).Owner);Assert.Equal(257,s.Picks.Count(p=>p.Year==2027));
        Assert.True(ProTrade.Available(s,comp));Assert.Equal(comp.Slot,ProPickProjection.Slot(s,comp));
    }
    static ProFranchise Market()
    {
        var s=New();var pool=s.Players.Take(100).ToArray();
        return s with{Year=2028,CompensatoryProjections=[],CompensatoryCandidates=[],CompensatorySignings=[],
            Contracts=pool.Select(p=>new ProContract(p.Football.PlayerId,p.Football.TeamName,2027,1,0,[new(2027,1_000_000,0)])).ToList()};
    }
    [Fact]public void SigningsCancelLossesAndVeteranAwardsAreCappedAtFifth()
    {
        var s=Market();var p=s.Players.Take(6).ToArray();var from=s.UserTeam;var other="Miami Dolphins";
        s=s with{CompensatorySignings=[new(2027,p[0].Football.PlayerId,from,other,40_000_000,5,Position.WR),new(2027,p[1].Football.PlayerId,from,other,30_000_000,5,Position.CB),new(2027,p[2].Football.PlayerId,from,other,20_000_000,12,Position.DE),new(2027,p[3].Football.PlayerId,other,from,40_000_000,5,Position.WR)]};
        var awards=ProCompensatoryDraft.Project(s,2028);var own=awards.Where(x=>x.Team==from).ToArray();Assert.Equal(2,own.Length);Assert.DoesNotContain(own,x=>x.Reason==p[0].Football.FullName);Assert.Equal(5,own.Single(x=>x.Reason==p[2].Football.FullName).Round);
        Assert.DoesNotContain(awards,x=>x.Team==other);
    }
    [Fact]public void LeagueAndClubLimitsAreEnforcedWithoutSpecialAwardsUsingQuota()
    {
        var s=Market();var players=s.Players.Take(200).ToArray();var events=new List<ProCompensatorySigning>();var i=0;
        foreach(var club in s.League.Teams)for(var n=0;n<5;n++){var p=players[i++];events.Add(new(2027,p.Football.PlayerId,club.Name,"External league",50_000_000+n*100_000,5,Position.QB));}
        s=s with{CompensatorySignings=events};var awards=ProCompensatoryDraft.Project(s,2028);Assert.Equal(32,awards.Count);Assert.All(awards.GroupBy(x=>x.Team),g=>Assert.InRange(g.Count(),1,4));Assert.All(awards,x=>Assert.InRange(x.Round,3,7));
    }
    [Fact]public void ExpirySigningAndEarlyReleaseHooksExcludeStreetAndOwnFreeAgents()
    {
        var s=Market() with{Year=2026,Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.Retention,Games=[]};
        var p=s.Players.First(x=>x.Football.TeamName==s.UserTeam) with{Experience=6,CreditedSeasons=6,PracticeSquad=false,Reserve=false};
        s=s with{Players=s.Players.Select(x=>x.Football.PlayerId==p.Football.PlayerId?p:x).ToList(),Contracts=[new(p.Football.PlayerId,s.UserTeam,2024,1,0,[new(2026,2_000_000,0)])],DeadCap=[]};
        var expired=ProEconomy.Expire(s,s.UserTeam,p.Football.PlayerId) with{Year=2027,OffseasonStep=OffseasonStep.FreeAgency};Assert.Single(expired.CompensatoryCandidates);
        var pay=ProEconomy.MarketSalary(expired,p)*2;var signed=ProEconomy.Sign(expired,new(p.Football.PlayerId,"Miami Dolphins",2,pay,0,0));Assert.Single(signed.CompensatorySignings);
        var cut=ProEconomy.Release(signed,"Miami Dolphins",p.Football.PlayerId,false);Assert.False(Assert.Single(cut.CompensatorySignings).Eligible);
        var retained=ProEconomy.Sign(expired,new(p.Football.PlayerId,s.UserTeam,2,pay,0,0));Assert.Empty(retained.CompensatorySignings);
        var street=expired with{CompensatoryCandidates=[]};Assert.Empty(ProEconomy.Sign(street,new(p.Football.PlayerId,"Miami Dolphins",2,pay,0,0)).CompensatorySignings);
        var late=ProEconomy.Release(signed with{Phase=ProPhase.RegularSeason,Week=10},"Miami Dolphins",p.Football.PlayerId,false);Assert.True(Assert.Single(late.CompensatorySignings).Eligible);
    }
    [Fact]public async Task SavedAwardsAndNegotiatedOwnershipAreStableOnReload()
    {
        var s=ProDraft.Generate(New() with{Year=2027,Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.FreeAgency});var dir=Path.Combine(Path.GetTempPath(),"comp-save-"+Guid.NewGuid());
        try{var store=new ProSaveStore(dir);await store.Save(s);var loaded=await store.Load(s.Id);Assert.Equal(s.Picks,loaded.Picks);Assert.Equal(s.CompensatoryAwardYears,loaded.CompensatoryAwardYears);Assert.Equal(s.CompensatoryProjections,loaded.CompensatoryProjections);}
        finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
    }
    [Fact]public void SpecialFutureEntitlementsDoNotSuppressSimulatedFreeAgentAwards()
    {
        var opening=New();var s=Market();var p=s.Players.First();
        s=s with{CompensatoryProjections=opening.CompensatoryProjections.Where(x=>x.Year==2028).ToList(),CompensatorySignings=[new(2027,p.Football.PlayerId,"Miami Dolphins","Buffalo Bills",50_000_000,5,Position.QB)]};
        var awards=ProCompensatoryDraft.Project(s,2028);Assert.Contains(awards,x=>x.Special&&x.Team==s.UserTeam);Assert.Contains(awards,x=>!x.Special&&x.Team=="Miami Dolphins");
    }

}
