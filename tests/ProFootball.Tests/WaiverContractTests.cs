using System.Text.Json;
using DynastyManager.Core.Models;
using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public sealed class WaiverContractTests
{
    static readonly ProFranchise Template=ProLeagueService.Create("Seattle Seahawks");
    static (ProFranchise S,ProPlayer P,string Buyer,ProContract C) Setup()
    {
        var s=Template with{Phase=ProPhase.RegularSeason,Week=10,TransactionTick=12,SalaryCap=900_000_000,TeamCapAllocations=[],Contracts=[],DeadCap=[],Waivers=[],Bids=[]};
        var p=s.Players.First(p=>p.Football.TeamName==s.UserTeam&&!p.Reserve&&!p.PracticeSquad);
        p=p with{Experience=2,RookieYear=2024,DraftOverall=30,Football=p.Football with{CurrentInjury=null}};
        var buyer=s.League.Teams.First(t=>t.Name!=s.UserTeam).Name;
        var c=new ProContract(p.Football.PlayerId,s.UserTeam,2024,4,16_000_000,[new(2026,6_000_000,3_000_000),new(2027,8_000_000,5_000_000)]){OriginalRookieYear=2024,LastChangedYear=2025};
        return(s with{Players=[p],Contracts=[c]},p,buyer,c);
    }
    [Fact] public void ClaimAssumesRemainingRookieContractAndCreditsOnlyTransferredGuarantees()
    {
        var(s,p,buyer,c)=Setup();var unrelated=new DeadCapCharge(s.UserTeam,2027,p.Football.PlayerId,123_456){CashAmount=12_345};s=s with{DeadCap=[unrelated]};
        var released=ProEconomy.Release(s,s.UserTeam,p.Football.PlayerId);var entry=Assert.Single(released.Waivers);
        var claimed=ProRoster.Claim(released,buyer,p.Football.PlayerId);
        Assert.Equal(released.Contracts,claimed.Contracts);Assert.Equal(released.DeadCap,claimed.DeadCap);
        var next=ProRoster.ResolveWaivers(claimed with{TransactionTick=entry.ExpiresAfterWeek});
        var contract=Assert.Single(next.Contracts,c=>c.Active);Assert.Equal(buyer,contract.Team);Assert.Equal(2024,contract.SignedYear);Assert.Equal(2027,ProContractInsights.PlayingEnd(contract));
        Assert.Equal(c.OriginalRookieYear,contract.OriginalRookieYear);Assert.Equal(c.LastChangedYear,contract.LastChangedYear);
        Assert.Equal(3_000_000,contract.Years.Single(y=>y.Year==2026).Salary);Assert.Equal(8_000_000,contract.Years.Single(y=>y.Year==2027).Salary);
        Assert.Equal(5_000_000,contract.Years.Single(y=>y.Year==2027).Guaranteed);Assert.All(contract.Years,y=>Assert.Equal(0,ProEconomy.BonusCharge(contract,y.Year)));
        Assert.Contains(unrelated,next.DeadCap);Assert.DoesNotContain(next.DeadCap,d=>d.ReleaseId==entry.ReleaseId&&d.Year==2027&&d.Amount==5_000_000);
        Assert.Equal(11_000_000,next.DeadCap.Where(d=>d.ReleaseId==entry.ReleaseId).Sum(d=>d.Amount));
        foreach(var year in new[]{2026,2027})Assert.Equal(ProFinance.Cash(s,s.UserTeam,year),ProFinance.Cash(next,s.UserTeam,year)+ProFinance.Cash(next,buyer,year));
    }
    [Fact] public void ClaimCostUsesExistingSalaryRatherThanMarketAndChecksFutureCap()
    {
        var(s,p,buyer,c)=Setup();s=s with{Phase=ProPhase.Preseason,Contracts=[c with{Years=[new(2026,1_000_000,0),new(2027,20_000_000,0)]}]};
        s=ProEconomy.Release(s,s.UserTeam,p.Football.PlayerId);
        s=s with{DeadCap=s.DeadCap.Append(new(buyer,2026,Guid.NewGuid(),s.SalaryCap-2_000_000)).ToList()};
        var claimed=ProRoster.Claim(s,buyer,p.Football.PlayerId);Assert.Contains(buyer,Assert.Single(claimed.Waivers).Claims);
        var blocked=s with{DeadCap=s.DeadCap.Append(new(buyer,2027,Guid.NewGuid(),ProFinance.Limit(s,buyer,2027)-10_000_000)).ToList()};
        var before=JsonSerializer.Serialize(blocked);Assert.Throws<InvalidOperationException>(()=>ProRoster.Claim(blocked,buyer,p.Football.PlayerId));Assert.Equal(before,JsonSerializer.Serialize(blocked));
    }
    [Fact] public void AwardRechecksPendingBidsAndPreservesOriginalDebtIfClaimFails()
    {
        var(s,p,buyer,_)=Setup();s=ProEconomy.Release(s,s.UserTeam,p.Football.PlayerId);s=ProRoster.Claim(s,buyer,p.Football.PlayerId);
        var debt=s.DeadCap;var entry=Assert.Single(s.Waivers);
        s=s with{TransactionTick=entry.ExpiresAfterWeek,Bids=[new(Guid.NewGuid(),buyer,1,s.SalaryCap,0,0)]};
        var next=ProRoster.ResolveWaivers(s);Assert.Equal("Free agent",next.Players.Single().Football.TeamName);Assert.Equal(debt,next.DeadCap);Assert.Empty(next.Waivers);
    }
    [Fact] public async Task SnapshotSurvivesRealSaveLoadAndUnclaimedContractsAreNotRewritten()
    {
        var(s,p,buyer,_)=Setup();s=ProEconomy.Release(s,s.UserTeam,p.Football.PlayerId);var root=Path.Combine(Path.GetTempPath(),"waiver-contract-"+Guid.NewGuid());
        try
        {
            var store=new ProSaveStore(root);await store.Save(s);s=await store.Load(s.Id);
            Assert.NotNull(Assert.Single(s.Waivers).ReleaseId);s=ProRoster.Claim(s,buyer,p.Football.PlayerId);
            var next=ProRoster.ResolveWaivers(s with{TransactionTick=Assert.Single(s.Waivers).ExpiresAfterWeek});Assert.Equal(buyer,next.Players.Single().Football.TeamName);Assert.Equal(2027,ProContractInsights.PlayingEnd(Assert.Single(next.Contracts,c=>c.Active)));
        }finally{if(Directory.Exists(root))Directory.Delete(root,true);}
        var(fresh,player,_,_)=Setup();var cut=ProEconomy.Release(fresh,fresh.UserTeam,player.Football.PlayerId);
        var cleared=ProRoster.ResolveWaivers(cut with{TransactionTick=Assert.Single(cut.Waivers).ExpiresAfterWeek});Assert.Equal(cut.DeadCap,cleared.DeadCap);Assert.Equal(cut.Contracts,cleared.Contracts);
    }
    [Fact] public void NonSalaryCashRemainsWithOriginalClubAndIsNotPaidTwice()
    {
        var(s,p,buyer,c)=Setup();c=c with{BonusCash=new(){[2026]=0,[2027]=2_000_000},Years=c.Years.Select(y=>y with{OtherCash=100_000,OtherCompensation=100_000}).ToList()};s=s with{Contracts=[c]};
        var cut=ProEconomy.Release(s,s.UserTeam,p.Football.PlayerId);var next=ProRoster.ResolveWaivers(ProRoster.Claim(cut,buyer,p.Football.PlayerId) with{TransactionTick=13});
        foreach(var year in new[]{2026,2027})Assert.Equal(ProFinance.Cash(s,s.UserTeam,year),ProFinance.Cash(next,s.UserTeam,year)+ProFinance.Cash(next,buyer,year));
        Assert.Equal(2_100_000,ProFinance.Cash(next,s.UserTeam,2027));Assert.Equal(8_000_000,ProFinance.Cash(next,buyer,2027));
    }
    [Fact] public void FormerTeamCannotClaimItsOwnWaivedContract()
    {
        var(s,p,_,_)=Setup();s=ProEconomy.Release(s,s.UserTeam,p.Football.PlayerId);Assert.Throws<InvalidOperationException>(()=>ProRoster.Claim(s,s.UserTeam,p.Football.PlayerId));
    }
    [Fact] public void ReviewedSourceAssignmentConservesScheduledCashAndAggregateCap()
    {
        var s=ProLeagueService.Create("Seattle Seahawks",sourcedContracts:true) with{SalaryCap=900_000_000,TeamCapAllocations=[],Contracts=[],DeadCap=[]};
        var p=s.Players.Single(p=>p.Football.FullName=="Jaxon Smith-Njigba") with{Experience=2};var buyer=s.League.Teams.First(t=>t.Name!=s.UserTeam).Name;
        var c=ProContractData.Load().Single(c=>c.PlayerId==p.Football.PlayerId);s=s with{Players=[p],Contracts=[c]};
        var cut=ProEconomy.Release(s,s.UserTeam,p.Football.PlayerId);var next=ProRoster.ResolveWaivers(ProRoster.Claim(cut,buyer,p.Football.PlayerId) with{TransactionTick=1});
        Assert.Equal(buyer,next.Players.Single().Football.TeamName);
        foreach(var year in Enumerable.Range(2026,9))Assert.Equal(ProFinance.Cash(s,s.UserTeam,year),ProFinance.Cash(next,s.UserTeam,year)+ProFinance.Cash(next,buyer,year));
        var before=Enumerable.Range(2026,9).Sum(y=>ProFinance.Budget(s,s.UserTeam,y).TotalCommitments);
        var after=Enumerable.Range(2026,9).Sum(y=>ProFinance.Budget(next,s.UserTeam,y).TotalCommitments+ProFinance.Budget(next,buyer,y).TotalCommitments);
        Assert.Equal(before,after);
        var ended=ProEconomy.Release(next with{Year=2028},buyer,p.Football.PlayerId,false);
        Assert.DoesNotContain(ended.DeadCap,d=>d.RetainedContract&&d.FundingYear==2029);
        var guaranteed=ProEconomy.Release(next with{Year=2029,Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.FreeAgency},buyer,p.Football.PlayerId,false);
        Assert.Equal(31_000_000,ProFinance.Cash(guaranteed,s.UserTeam,2029)+ProFinance.Cash(guaranteed,buyer,2029));
    }

}
