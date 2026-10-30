using DynastyManager.Core.Models;
using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public class GuidedOffseasonTests
{
    static ProFranchise Fictional()=>ProLeagueService.Create("Seattle Seahawks",generatedPlayers:true) with{SalaryCap=2_000_000_000,TeamCapAllocations=[]};
    [Fact] public void BackfieldReviewCorrectsStableIdsWithoutChangingContractsOrAppearance()
    {
        var s=ProLeagueService.Create("Seattle Seahawks");
        foreach(var name in new[]{"Robbie Ouzts","Brady Russell","Hunter Luepke","Patrick Ricard","Ben VanSumeren","DJ Herman"})Assert.Equal(Position.FB,s.Players.Single(p=>p.Football.FullName==name).Football.Position);
        Assert.Equal(Position.TE,s.Players.Single(p=>p.Football.FullName=="Jack Westover").Football.Position);Assert.Equal(4,s.Players.Single(p=>p.Football.FullName=="Jack Westover").Football.NcaaPositionCode);
        Assert.Equal(Position.RB,s.Players.Single(p=>p.Football.FullName=="Derrick Henry").Football.Position);
        var p=s.Players.Single(p=>p.Football.FullName=="Brady Russell");var old=s with{Players=s.Players.Select(x=>x==p?p with{Football=p.Football with{Position=Position.RB,NcaaPositionCode=1}}:x).ToList()};
        var next=ProPlayerRatingService.Migrate(old);Assert.Same(old.Contracts,next.Contracts);Assert.Equal(p.Football.Appearance,next.Players.Single(x=>x.Football.PlayerId==p.Football.PlayerId).Football.Appearance);
        Assert.Equal(Position.FB,next.Players.Single(x=>x.Football.PlayerId==p.Football.PlayerId).Football.Position);
        Assert.True(ProBackfieldPositions.Order(Position.RB)<ProBackfieldPositions.Order(Position.FB));Assert.True(ProBackfieldPositions.Order(Position.FB)<ProBackfieldPositions.Order(Position.WR));
    }
    [Fact] public void OffseasonExtensionCanExceedPlanButRegularSeasonRemainsProtectedAndPreviewUsesCorrectYears()
    {
        var s=Fictional() with{Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.Retention,SalaryCap=1};var p=s.Players.First(p=>p.Football.TeamName==s.UserTeam&&p.Age>=29);
        var c=new ProContract(p.Football.PlayerId,s.UserTeam,2024,0,0,[new(s.Year,5_000_000,0)]);
        s=s with{Contracts=s.Contracts.Where(c=>c.PlayerId!=p.Football.PlayerId).Append(c).ToList()};
        var salary=Math.Max(10_000_000,ProEconomy.MarketSalary(s,p)*2);var offer=ProContractInsights.ProjectOffer(s,p,2,salary,0,true);var view=ProOfferCapProjection.Build(s,p,offer,true);
        Assert.Equal(view[0].Before,view[0].After);Assert.Equal(view[1].Before-salary,view[1].After);Assert.Equal(s.Year+1,view[1].Year);
        var next=ProEconomy.Extend(s,s.UserTeam,p.Football.PlayerId,2,salary);Assert.True(ProFinance.Budget(next,s.UserTeam,s.Year+1).Available<0);
        Assert.Throws<InvalidOperationException>(()=>ProEconomy.Extend(s with{Phase=ProPhase.RegularSeason},s.UserTeam,p.Football.PlayerId,2,salary));
        Assert.Throws<InvalidOperationException>(()=>ProSeasonEligibility.Require(next with{Phase=ProPhase.RegularSeason},s.UserTeam));
    }
    [Fact] public void AssistanceOnlySubmitsAffordableBidsAndNeverChangesTheRosterOrSignedContracts()
    {
        var s=Fictional();var qb=s.Players.First(p=>p.Football.TeamName==s.UserTeam&&p.Football.Position==Position.QB);foreach(var release in s.Players.Where(p=>p.Football.TeamName==s.UserTeam&&p.Football.Position==Position.QB).Take(2).ToArray())s=ProEconomy.Release(s,s.UserTeam,release.Football.PlayerId,false);
        var next=ProFreeAgencyAssistance.Propose(s);Assert.Contains(next.Bids,b=>b.Team==s.UserTeam);Assert.Same(s.Players,next.Players);Assert.Same(s.Contracts,next.Contracts);
        Assert.True(ProEconomy.Pending(next,s.UserTeam)<=ProEconomy.Room(next,s.UserTeam));Assert.DoesNotContain(next.Bids,b=>b.Team!=s.UserTeam);
    }
    [Fact] public void NormalDraftProgressesToTrainingCampAndPreseasonButCannotBypassOpeningDayCap()
    {
        var s=ProDraft.Generate(Fictional() with{Year=2027,Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.FreeAgency});
        s=ProOffseason.Advance(s);Assert.Equal(OffseasonStep.Draft,s.OffseasonStep);Assert.Equal(s.UserTeam,ProDraft.Current(s)!.Owner);Assert.NotNull(ProOffseasonFlow.Blocker(s));
        s=ProDraft.AutoUntilUser(s,true);Assert.Null(ProDraft.Current(s));s=ProRookieOptions.ManageCpu(s,true,s.UserTeam);s=ProOffseason.Advance(s);Assert.Equal(OffseasonStep.RosterCuts,s.OffseasonStep);Assert.Equal("Training camp",ProOffseasonFlow.Title(s.OffseasonStep));Assert.Equal(3,(int)s.OffseasonStep);
        s=s with{DeadCap=s.DeadCap.Append(new(s.UserTeam,s.Year,Guid.NewGuid(),3_000_000_000)).ToList()};s=ProOffseason.Advance(s);Assert.Equal(ProPhase.Preseason,s.Phase);Assert.Contains("over the salary cap",ProSeasonEligibility.Issue(s,s.UserTeam));
    }
    [Fact] public async Task ExportValidatedGuidedWorkflowFixtures()
    {
        if(Environment.GetEnvironmentVariable("PRO_COORDINATOR_UI_FIXTURES") is not string output)return;
        var roster=ProLeagueService.Create("Seattle Seahawks") with{Name="Workflow roster QA"};var retention=Fictional() with{Name="Workflow retention QA",Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.Retention};
        var p=retention.Players.First(p=>p.Football.TeamName==retention.UserTeam&&p.Age>=29);retention=retention with{Contracts=retention.Contracts.Where(c=>c.PlayerId!=p.Football.PlayerId).Append(new(p.Football.PlayerId,retention.UserTeam,2024,0,0,[new(retention.Year,5_000_000,0)])).ToList()};
        var draft=ProDraft.Generate(Fictional() with{Name="Workflow draft QA",Year=2027,Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.Draft,AutoManageRookieOptions=true});
        var dir=Path.Combine(Path.GetTempPath(),"guided-qa-"+Guid.NewGuid());try{var store=new ProSaveStore(dir);foreach(var s in new[]{roster,retention,draft}){await store.Save(s);Assert.Equal(s.OffseasonStep,(await store.Load(s.Id)).OffseasonStep);}}finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
        using var file=File.Create(Path.Combine(output,"pro-staff-guided-ui-fixture.json.gz"));using var gzip=new System.IO.Compression.GZipStream(file,System.IO.Compression.CompressionLevel.SmallestSize);System.Text.Json.JsonSerializer.Serialize(gzip,new{roster,retention,draft,extensionPlayerId=p.Football.PlayerId,extensionPlayerName=p.Football.FullName},new System.Text.Json.JsonSerializerOptions{PropertyNamingPolicy=System.Text.Json.JsonNamingPolicy.CamelCase});
    }
}
