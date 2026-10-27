using ProFootball.Core;
using DynastyManager.Core.Models;
using Xunit;
namespace ProFootball.Tests;
public sealed class FinanceRefinementTests
{
    static ProFranchise New()=>ProLeagueService.Create("Seattle Seahawks");
    [Fact] public void RookieAndFreshDealsAreProtectedAndLegacyConcernsClear()
    {
        var s=New();var p=s.Players.First(p=>p.Football.TeamName==s.UserTeam);
        var rookie=p with{Experience=2,RookieYear=s.Year-2,DraftOverall=10,ContractConcernWeeks=12,TradeRequested=true};
        var c=new ProContract(p.Football.PlayerId,s.UserTeam,s.Year-2,4,0,[new(s.Year,1_000_000,0)]);
        Assert.True(ProContractInsights.RookieDeal(s,rookie,c));Assert.False(ProContractInsights.ConcernEligible(s,rookie,c));
        var veteran=p with{Experience=8,RookieYear=s.Year-8};Assert.False(ProContractInsights.ConcernEligible(s,veteran,c with{SignedYear=s.Year}));
        var clean=ProMorale.NormalizeContracts(s with{Players=[rookie],Contracts=[c],ContractMoraleVersion=0});
        Assert.False(clean.Players[0].TradeRequested);Assert.Equal(0,clean.Players[0].ContractConcernWeeks);
    }
    [Fact] public void RolloverIsCreditedOnceAndFloorPaymentDoesNotConsumeCap()
    {
        var s=New() with{Contracts=[],DeadCap=[],CarryoverPercentage=50};var closed=ProFinance.CloseYear(s);
        var own=closed.FinancialHistory.Single(h=>h.Team==s.UserTeam);
        Assert.Equal(ProFinance.Limit(s,s.UserTeam)/2,own.CarryOut);Assert.Equal(0,own.FloorPayment);Assert.Equal(0,own.CapSpent);
        Assert.Equal(32,ProFinance.CloseYear(closed).FinancialHistory.Count);
        var next=closed with{Year=s.Year+1,SalaryCap=ProFinance.ForecastCap(s,s.Year+1)};
        Assert.Equal(next.SalaryCap+own.CarryOut,ProFinance.Limit(next,s.UserTeam));
        Assert.Equal(ProFinance.Growth(s,2029),ProFinance.Growth(s with{Week=4},2029));
    }
    [Fact] public void RestructureConservesCashAndTotalCapAndRetainsLegalSalary()
    {
        var s=New();var p=s.Players.First(p=>p.Football.TeamName==s.UserTeam&&!p.PracticeSquad);
        var c=new ProContract(p.Football.PlayerId,s.UserTeam,s.Year-1,3,3_000_000,[new(s.Year-1,8_000_000,0),new(s.Year,8_000_000,0),new(s.Year+1,8_000_000,0)]);
        s=s with{Contracts=[c]};var cash=ProFinance.Cash(s,s.UserTeam,s.Year);var before=Enumerable.Range(s.Year,2).Sum(y=>ProEconomy.CapHit(c,y));
        var changed=ProNegotiation.Restructure(s,s.UserTeam,p.Football.PlayerId,4_000_000);var revised=changed.Contracts[0];
        Assert.Equal(cash,ProFinance.Cash(changed,s.UserTeam,s.Year));Assert.Equal(before,Enumerable.Range(s.Year,2).Sum(y=>ProEconomy.CapHit(revised,y)));
        Assert.True(ProEconomy.CapHit(revised,s.Year)<ProEconomy.CapHit(c,s.Year));Assert.Throws<InvalidOperationException>(()=>ProNegotiation.Restructure(changed,s.UserTeam,p.Football.PlayerId,250_000));
        Assert.Throws<InvalidOperationException>(()=>ProNegotiation.Restructure(s,s.UserTeam,p.Football.PlayerId,8_000_000-ProFinance.Minimum(s,p,practice:false)+1));
    }
    [Fact] public void ReleasePreviewMatchesAnnualDeadCapAndPreservesEarnedCash()
    {
        var s=New() with{Phase=ProPhase.RegularSeason,Week=10};var p=s.Players.First(p=>p.Football.TeamName==s.UserTeam);
        var c=new ProContract(p.Football.PlayerId,s.UserTeam,s.Year,3,6_000_000,[new(s.Year,8_000_000,1_000_000),new(s.Year+1,8_000_000,2_000_000),new(s.Year+2,8_000_000,0)]);
        s=s with{Contracts=[c]};var preview=ProContractInsights.Release(s,p.Football.PlayerId);var released=ProEconomy.Release(s,s.UserTeam,p.Football.PlayerId);
        Assert.All(preview,row=>Assert.Equal(row.DeadMoney,released.DeadCap.Where(d=>d.Year==row.Year).Sum(d=>d.Amount)));
        Assert.Equal(10_000_000,preview[0].DeadMoney);Assert.Equal(2_000_000,preview[1].DeadMoney);
        Assert.Equal(10_000_000,ProFinance.Cash(released,s.UserTeam,s.Year));
    }
    [Fact] public void FuturePickProjectionTracksStrengthAndOriginalTeam()
    {
        var s=New();var a=s.League.Teams[0].Name;var b=s.League.Teams[1].Name;
        s=s with{Players=s.Players.Select(p=>p.Football.TeamName==a||p.Football.TeamName==b?p with{Age=25,Football=p.Football with{OverallRating=p.Football.TeamName==a?45:95,PotentialRating=p.Football.TeamName==a?45:95}}:p).ToList()};
        var pa=s.Picks.First(p=>p.Year==s.Year+1&&p.Round==1&&p.OriginalTeam==a);var pb=s.Picks.First(p=>p.Year==s.Year+1&&p.Round==1&&p.OriginalTeam==b);
        Assert.True(ProTrade.PickValue(s,pa)>ProTrade.PickValue(s,pb));Assert.Equal(ProPickProjection.Slot(s,pa),ProPickProjection.Slot(s,pa with{Owner=b}));
        Assert.Equal(pa.Slot,ProPickProjection.Slot(s with{Year=pa.Year,Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.Draft},pa));
    }
    [Fact] public void TransactionsRetainHistoryAndAllProTeamsHaveDistinctPlayers()
    {
        var s=New() with{Transactions=["2025 W8 · Legacy move"],TransactionLog=[]};
        for(var i=0;i<300;i++)s=ProTransactions.Add(s,"Move "+i);
        Assert.Equal(301,s.TransactionLog.Count);Assert.Equal(2025,s.TransactionLog[0].Year);Assert.Null(s.TransactionLog[0].Phase);
        var picks=ProHonors.SelectAllPros(s.Players);Assert.Equal(picks.Count,picks.Select(x=>x.Player.Football.PlayerId).Distinct().Count());Assert.Contains(picks,p=>p.Team==2);
    }
    [Fact] public void PhotoReferenceColorsApplyButNeverOverwriteCustomAppearance()
    {
        var s=New();var matched=s.Players.First(p=>p.AppearanceOrigin?.StartsWith("http")==true);
        Assert.NotNull(PortraitAppearance.ValidSkinColor(matched.Football.Appearance?.SkinColorHex));
        var edited=matched with{AppearanceOrigin="custom",Football=matched.Football with{Appearance=new(){SkinTone=6}}};
        Assert.Equal(edited,ProAppearanceData.Apply(s with{Players=[edited]}).Players[0]);
        var legacy=edited with{AppearanceOrigin=null};Assert.Equal(legacy,ProAppearanceData.Apply(s with{Players=[legacy]}).Players[0]);
    }
    [Fact] public void CpuUsesRestructureBeforeCuttingCoreStarter()
    {
        var s=New();var p=s.Players.First(p=>p.Football.TeamName==s.UserTeam&&p.Experience>=4&&!p.PracticeSquad);
        p=p with{Experience=8,RookieYear=s.Year-8};var c=new ProContract(p.Football.PlayerId,s.UserTeam,s.Year-1,3,3_000_000,[new(s.Year-1,30_000_000,0),new(s.Year,30_000_000,0),new(s.Year+1,30_000_000,0)]);
        s=s with{Players=[p],Contracts=[c],TeamCapAllocations=[],SalaryCap=150_000_000};var before=ProFinance.Cash(s,s.UserTeam,s.Year);
        var repaired=ProCpu.MakeRoom(s,s.UserTeam,125_000_000);
        Assert.True(ProEconomy.Room(repaired,s.UserTeam)>=125_000_000);Assert.Equal(s.UserTeam,repaired.Players[0].Football.TeamName);Assert.Empty(repaired.DeadCap);Assert.Equal(before,ProFinance.Cash(repaired,s.UserTeam,s.Year));
    }
    [Fact] public void SourcedFiveSeasonsCanFinishDraftsAndRepairCpuBudgets()
    {
        var s=ProLeagueService.Create("Seattle Seahawks",sourcedContracts:true) with{Id=Guid.Parse("53120000-0000-0000-0000-000000000001"),AutoManageRookieOptions=true};
        for(var season=0;season<5;season++)
        {
            s=ProCpu.Manage(s,true);
            for(var tick=0;tick<26&&s.Phase!=ProPhase.Offseason;tick++){
                if(s.Phase==ProPhase.Preseason&&s.Week==3)s=ProCpu.Trim(s,s.UserTeam);
                s=ProLeagueService.Advance(s);
            }
            Assert.Equal(ProPhase.Offseason,s.Phase);s=ProOffseason.Advance(s);
            Assert.InRange(s.Prospects.Average(p=>p.Football.OverallRating),55,75);
            s=ProOffseason.Advance(s);s=ProDraft.AutoUntilUser(s,true);s=ProOffseason.Advance(s);s=ProCpu.Trim(s,s.UserTeam);s=ProOffseason.Advance(s);s=ProCpu.Manage(s,true);
            Assert.Empty(s.UnsignedRookies);Assert.All(s.League.Teams,t=>{Assert.True(ProEconomy.Room(s,t.Name)>=0,t.Name);Assert.InRange(ProRoster.Active(s,t.Name),45,90);Assert.True(ProRoster.CountForLimit(s,t.Name)<=90);});
            Assert.Equal((season+1)*32,s.FinancialHistory.Count);
        }
    }
}
