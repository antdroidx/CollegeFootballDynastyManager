using System.Text.Json;
using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public sealed class TerminationTimingTests
{
    static readonly ProFranchise Template=ProLeagueService.Create("Seattle Seahawks",sourcedContracts:true);
    static (ProFranchise S,ProContract C) Jsn()
    {
        var s=Template with{SalaryCap=900_000_000,TeamCapAllocations=[],DeadCap=[]};
        var p=s.Players.Single(p=>p.Football.FullName=="Jaxon Smith-Njigba");return(s with{Contracts=s.Contracts.Where(c=>c.PlayerId==p.Football.PlayerId).ToList()},s.Contracts.Single(c=>c.PlayerId==p.Football.PlayerId));
    }
    [Fact] public void ReviewedSourceSeparatesPaidBonusUnpaidGuaranteesAndUnexercisedOptions()
    {
        var(s,c)=Jsn();var before=s with{Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.FreeAgency};
        var pre=ProEconomy.Release(before,s.UserTeam,c.PlayerId,false);Assert.Equal(71_002_325,pre.DeadCap.Where(d=>d.Year==2026).Sum(d=>d.Amount));
        var post=ProEconomy.Release(s,s.UserTeam,c.PlayerId,false);Assert.Equal(43_002_325,post.DeadCap.Where(d=>d.Year==2026).Sum(d=>d.Amount));Assert.Equal(28_000_000,post.DeadCap.Where(d=>d.Year==2027).Sum(d=>d.Amount));
        Assert.Equal(36_500_000,ProFinance.Cash(post,s.UserTeam,2026));Assert.Equal(32_630_996,ProFinance.Cash(post,s.UserTeam,2027));Assert.Equal(0,ProFinance.Cash(post,s.UserTeam,2029));
        Assert.All(ProContractInsights.Release(s,c.PlayerId),r=>Assert.Equal(r.DeadMoney,post.DeadCap.Where(d=>d.Year==r.Year).Sum(d=>d.Amount)));
    }
    [Fact] public void ReportedVestingCreatesObligationOnlyWhilePlayerRemainsUnderContract()
    {
        var(s,c)=Jsn();var unvested=s with{Year=2027,Phase=ProPhase.RegularSeason,Week=18};var vested=unvested with{Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.Retention};
        Assert.Equal(0,ProBonusAccounting.NonBonusGuarantee(unvested,c,c.Years.Single(y=>y.Year==2028)));
        Assert.Equal(19_937_000,ProBonusAccounting.NonBonusGuarantee(vested,c,c.Years.Single(y=>y.Year==2028)));
        Assert.Equal(10_000_000,ProBonusAccounting.NonBonusGuarantee(vested,c,c.Years.Single(y=>y.Year==2029)));
        var cut=ProEconomy.Release(unvested,s.UserTeam,c.PlayerId,false);Assert.Equal(0,ProFinance.Cash(cut,s.UserTeam,2028));
        var late=ProEconomy.Release(vested,s.UserTeam,c.PlayerId,false);Assert.Equal(19_937_000,ProFinance.Cash(late,s.UserTeam,2028));Assert.Equal(10_000_000,ProFinance.Cash(late,s.UserTeam,2029));
    }
    [Fact] public void UnpaidConditionalBonusIsNotCountedTwiceWhenGuaranteedSalaryCompensatesIt()
    {
        var(s,c)=Jsn();s=s with{Year=2029,Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.FreeAgency};
        var cut=ProEconomy.Release(s,s.UserTeam,c.PlayerId,false);Assert.Equal(31_000_000,ProFinance.Cash(cut,s.UserTeam,2029));
        var paid=s with{OffseasonStep=OffseasonStep.Draft};var after=ProEconomy.Release(paid,s.UserTeam,c.PlayerId,false);Assert.Equal(31_000_000,ProFinance.Cash(after,s.UserTeam,2029));
        Assert.Equal(cut.DeadCap.Sum(d=>d.Amount),after.DeadCap.Sum(d=>d.Amount));
    }
    [Fact] public void EarlyDesignationHoldsCapUntilJune1AndEnforcesTwoPerTeam()
    {
        var(s,c)=Jsn();s=s with{Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.FreeAgency,Contracts=[],Players=s.Players.Take(3).Select(p=>p with{Football=p.Football with{TeamName=s.UserTeam}}).ToList()};
        s=s with{Contracts=s.Players.Select(p=>new ProContract(p.Football.PlayerId,s.UserTeam,2025,5,10_000_000,[new(2026,10_000_000,0),new(2027,10_000_000,0),new(2028,10_000_000,0),new(2029,10_000_000,0)])).ToList()};
        var player=s.Players[0].Football.PlayerId;var before=ProFinance.Budget(s,s.UserTeam).TotalCommitments;
        var first=ProEconomy.Release(s,s.UserTeam,player,false,true);Assert.Equal(before,ProFinance.Budget(first,s.UserTeam).TotalCommitments);Assert.Contains(first.DeadCap,d=>d.June1Hold);
        var june=ProTermination.ProcessJune1(first with{Phase=ProPhase.Preseason});Assert.Equal(before-10_000_000,ProFinance.Budget(june,s.UserTeam).TotalCommitments);Assert.DoesNotContain(june.DeadCap,d=>d.June1Hold);
        var second=ProEconomy.Release(first,s.UserTeam,s.Players[1].Football.PlayerId,false,true);Assert.Throws<InvalidOperationException>(()=>ProEconomy.Release(second,s.UserTeam,s.Players[2].Football.PlayerId,false,true));
        Assert.Throws<InvalidOperationException>(()=>ProEconomy.Release(s with{Year=2030},s.UserTeam,player,false,true));
    }
    [Fact] public void HistoricalDebtAndNegotiatedSourceDealsAreNeverMigratedAway()
    {
        var(s,c)=Jsn();var legacy=c with{BonusPayments=null,CashVesting=null,Years=c.Years.Select(y=>y with{GuaranteedOtherCash=0}).ToList()};
        var changed=legacy with{LastChangedYear=2026};var debt=new DeadCapCharge(s.UserTeam,2026,c.PlayerId,99_999);
        var migrated=ProContractData.MigrateAccounting(s with{Contracts=[changed],DeadCap=[debt]});Assert.Same(changed,Assert.Single(migrated.Contracts));Assert.Same(debt,Assert.Single(migrated.DeadCap));
        var eligible=ProContractData.MigrateAccounting(s with{Contracts=[legacy],DeadCap=[debt]});Assert.NotNull(Assert.Single(eligible.Contracts).BonusPayments);Assert.Same(debt,Assert.Single(eligible.DeadCap));
        Assert.All(c.Years,y=>Assert.Equal(ProEconomy.CapHit(legacy,y.Year),ProEconomy.CapHit(Assert.Single(eligible.Contracts),y.Year)));
    }
    [Fact] public void ConsecutiveDeficitsDoNotReuseTheSameProjectedRollover()
    {
        var(s,c)=Jsn();s=s with{Contracts=[],Players=[]};var team=s.UserTeam;
        var first=s.Year+1;var second=s.Year+2;
        var firstSalary=ProFinance.Limit(s,team,first)-ProCpuBudget.DepthReserve(s,team,first)+12_000_000;
        var secondSalary=ProFinance.Limit(s,team,second)-ProCpuBudget.DepthReserve(s,team,second)+12_000_000;
        s=s with{Contracts=[new(c.PlayerId,team,s.Year,1,0,[new(first,firstSalary,0),new(second,secondSalary,0)])]};
        Assert.Equal(24_000_000,ProCpuBudget.FutureRolloverNeed(s,team));
    }

}
