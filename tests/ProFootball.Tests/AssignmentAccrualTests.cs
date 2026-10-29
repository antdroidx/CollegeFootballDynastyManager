using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public sealed class AssignmentAccrualTests
{
    static (ProFranchise S,Guid Id,string Buyer) Setup(int week)
    {
        var s=ProLeagueService.Create("Seattle Seahawks") with{Phase=ProPhase.RegularSeason,Week=week,SalaryCap=900_000_000,TeamCapAllocations=[],Contracts=[],DeadCap=[]};
        var p=s.Players.First(p=>p.Football.TeamName==s.UserTeam&&!p.PracticeSquad&&!p.Reserve) with{Experience=2,Football=s.Players.First(p=>p.Football.TeamName==s.UserTeam&&!p.PracticeSquad&&!p.Reserve).Football with{CurrentInjury=null}};
        var c=new ProContract(p.Football.PlayerId,s.UserTeam,2025,1,0,[new(2026,6_000_000,0)]);
        return(s with{Players=[p],Contracts=[c]},p.Football.PlayerId,s.League.Teams.First(t=>t.Name!=s.UserTeam).Name);
    }
    [Fact] public void SameWeekWaiverReleaseDoesNotEarnTheBuyersSalaryRetroactively()
    {
        var(s,id,buyer)=Setup(10);var cut=ProEconomy.Release(s,s.UserTeam,id);
        var assigned=ProRoster.ResolveWaivers(ProRoster.Claim(cut,buyer,id) with{TransactionTick=1});
        var released=ProEconomy.Release(assigned,buyer,id,false);
        Assert.Equal(0,ProFinance.Cash(released,buyer,2026));Assert.Equal(3_000_000,ProFinance.Cash(released,s.UserTeam,2026));
    }
    [Fact] public void AssignedSalaryAccruesOnlyForWeeksSinceTheClaim()
    {
        var(s,id,buyer)=Setup(10);var cut=ProEconomy.Release(s,s.UserTeam,id);
        var assigned=ProRoster.ResolveWaivers(ProRoster.Claim(cut,buyer,id) with{TransactionTick=1});
        var released=ProEconomy.Release(assigned with{Week=11},buyer,id,false);
        Assert.Equal(333_333,ProFinance.Cash(released,buyer,2026));
    }
    [Fact] public void SameWeekTradeAndReleaseDoNotDuplicateEarnedSalary()
    {
        var(s,id,buyer)=Setup(8);var traded=ProTrade.Propose(s,s.UserTeam,buyer,[id],[],[],[]);
        var released=ProEconomy.Release(traded,buyer,id,false);
        Assert.Equal(0,ProFinance.Cash(released,buyer,2026));
        Assert.Equal((long)(6_000_000*ProFinance.EarnedFraction(s)),ProFinance.Cash(released,s.UserTeam,2026));
    }
    [Fact] public void RepeatedAssignmentsKeepOriginalTermsAndSplitOnlyElapsedWeeks()
    {
        var(s,id,buyer)=Setup(8);var third=s.League.Teams.First(t=>t.Name!=s.UserTeam&&t.Name!=buyer).Name;
        var first=ProTrade.Propose(s,s.UserTeam,buyer,[id],[],[],[]);
        var second=ProTrade.Propose(first with{Week=9},buyer,third,[id],[],[],[]);
        var released=ProEconomy.Release(second,third,id,false);
        Assert.Equal(0,ProFinance.Cash(released,third,2026));Assert.Equal(333_333,ProFinance.Cash(released,buyer,2026));
        Assert.Equal(2025,second.Contracts.Single(c=>c.Active).SignedYear);
        Assert.Equal((long)(6_000_000*8/18d),ProFinance.Cash(released,s.UserTeam,2026)+ProFinance.Cash(released,buyer,2026));
    }
    [Fact] public void GuaranteedUnpaidOptionIsAssumedByTradeBuyerRatherThanPaidTwice()
    {
        var(s,id,buyer)=Setup(1);s=s with{Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.FreeAgency};
        var c=s.Contracts.Single() with{BonusYears=5,AccountingVersion=1,BonusPayments=[new(2026,10_000_000,4,true)],BonusCash=new(){[2026]=10_000_000},Years=Enumerable.Range(2026,4).Select(y=>new ContractYear(y,2_000_000,0){ProratedBonus=2_500_000}).ToList()};
        s=s with{Contracts=[c]};var moved=ProTrade.Propose(s,s.UserTeam,buyer,[id],[],[],[]);
        Assert.Equal(0,ProFinance.Cash(moved,s.UserTeam,2026));Assert.Equal(12_000_000,ProFinance.Cash(moved,buyer,2026));
        Assert.Empty(moved.DeadCap);Assert.True(Assert.Single(moved.Contracts,x=>x.Active).BonusPayments!.Single().GuaranteedAtSigning);
        var cut=ProEconomy.Release(moved,buyer,id,false);Assert.Equal(10_000_000,ProFinance.Cash(cut,buyer,2026));Assert.Equal(0,ProFinance.Cash(cut,s.UserTeam,2026));
    }
    [Fact] public async Task AssignmentAccrualSurvivesTheRealSaveLoaderWithoutChangingBookedDebt()
    {
        var(s,id,buyer)=Setup(8);var moved=ProTrade.Propose(s,s.UserTeam,buyer,[id],[],[],[]);
        var root=Path.Combine(Path.GetTempPath(),"salary-assignment-"+Guid.NewGuid());
        try
        {
            var store=new ProSaveStore(root);await store.Save(moved);var loaded=await store.Load(moved.Id);
            Assert.Equal(moved.DeadCap,loaded.DeadCap);Assert.Equal(moved.Contracts.Single(c=>c.Active).SalaryAssignmentFraction,loaded.Contracts.Single(c=>c.Active).SalaryAssignmentFraction);
            Assert.Equal(0,ProFinance.Cash(ProEconomy.Release(loaded,buyer,id,false),buyer,2026));
        }finally{if(Directory.Exists(root))Directory.Delete(root,true);}
    }

}
