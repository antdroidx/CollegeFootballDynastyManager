using DynastyManager.Core.Models;
using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public class GmCommitmentTests
{
    static (ProFranchise State,ProPlayer Qb,ProPlayer Receiver) Setup()
    {
        var s=ProLeagueService.Create("Seattle Seahawks",role:StaffRole.OffensiveCoordinator,preserveLegacyPracticeForAudit:true);
        var qb=s.Players.First(p=>p.Football.TeamName==s.UserTeam&&p.Football.Position==Position.QB);
        var receiver=s.Players.First(p=>p.Football.TeamName==s.UserTeam&&p.Football.Position==Position.WR);
        s=s with{Contracts=[new(qb.Football.PlayerId,s.UserTeam,s.Year,1,0,[new(s.Year,1_000_000,0)]),new(receiver.Football.PlayerId,s.UserTeam,s.Year,1,0,[new(s.Year+1,100_000_000,0)])]};
        return(s,qb,receiver);
    }
    [Fact] public void FuturePickOverpaymentIsRejectedAtomicallyEvenWhenCapCostsDoNotIncrease()
    {
        var (s,qb,_) = Setup();
        var other = s.League.Teams.First(t => t.Name != s.UserTeam).Name;
        var sent = s.Picks.Where(p => p.Owner == s.UserTeam && p.Round == 1).Select(p => p.Id).ToHashSet();
        var result = ProManagement.Submit(s, "Trade recommendation", [qb.Football.PlayerId], 0,
            x => x with { Picks = x.Picks.Select(p => sent.Contains(p.Id) ? p with { Owner = other } : p).ToList() }, sharedPicks: true);
        Assert.False(Assert.Single(result.GmDecisions).Approved);
        Assert.Contains("draft capital", result.GmDecisions[0].Reason);
        Assert.Same(s.Picks, result.Picks); Assert.Same(s.Players, result.Players); Assert.Same(s.Contracts, result.Contracts);
    }
    [Fact] public void FuturePickReviewAcceptsAnAssetImprovementAndDoesNotTreatDraftSelectionAsTrade()
    {
        var (s,qb,_) = Setup();
        var sent = s.Picks.Where(p => p.Owner == s.UserTeam).MinBy(p => ProTrade.PickValue(s,p))!;
        var received = s.Picks.Where(p => p.Owner != s.UserTeam).MaxBy(p => ProTrade.PickValue(s,p))!;
        var result = ProManagement.Submit(s, "Trade recommendation", [qb.Football.PlayerId], 0,
            x => x with { Picks = x.Picks.Select(p => p.Id == sent.Id ? p with { Owner = received.Owner } : p.Id == received.Id ? p with { Owner = s.UserTeam } : p).ToList() }, sharedPicks: true);
        Assert.True(Assert.Single(result.GmDecisions).Approved);
        Assert.Equal(s.UserTeam, result.Picks.Single(p => p.Id == received.Id).Owner);
        var unchanged = ProManagement.Submit(s, "Draft recommendation", [qb.Football.PlayerId], 0, x => x, sharedPicks: true);
        Assert.True(Assert.Single(unchanged.GmDecisions).Approved);
    }
    [Fact] public void FutureExtensionIsReviewedAndRejectedWithoutApplyingSideEffects()
    {
        var(s,qb,_)=Setup();var calls=0;
        var result=ProManagement.Submit(s,"Extend",[qb.Football.PlayerId],0,x=>{calls++;return ProEconomy.Extend(x,x.UserTeam,qb.Football.PlayerId,1,100_000_000);});
        Assert.Equal(1,calls);Assert.Same(s.Contracts,result.Contracts);Assert.Same(s.Transactions,result.Transactions);
        var decision=Assert.Single(result.GmDecisions);Assert.False(decision.Approved);Assert.Contains((s.Year+1).ToString(),decision.Reason);
        Assert.Contains("future shared cap",decision.Reason);
    }
    [Fact] public void PendingMultiyearOffersReserveFutureUnitBudget()
    {
        var(s,_,receiver)=Setup();var free=s.Players.First(p=>p.Football.TeamName=="Free agent"&&p.Football.Position==Position.RB&&!s.Waivers.Any(w=>w.PlayerId==p.Football.PlayerId));
        s=s with{Contracts=s.Contracts.Select(c=>c.PlayerId==receiver.Football.PlayerId?c with{Years=[new(s.Year+1,150_000_000,0)]}:c).ToList()};
        var result=ProManagement.Submit(s,"Offer",[free.Football.PlayerId],40_000_000,x=>ProEconomy.Offer(x,x.UserTeam,free.Football.PlayerId,3,40_000_000,0));
        Assert.False(Assert.Single(result.GmDecisions).Approved);Assert.Empty(result.Bids);Assert.Same(s.Players,result.Players);
    }
    [Fact] public void InheritedFutureOverageCanBeReducedWithoutIncreasingIt()
    {
        var(s,_,receiver)=Setup();s=s with{Contracts=s.Contracts.Select(c=>c.PlayerId==receiver.Football.PlayerId?c with{Years=[new(s.Year+1,ProFinance.Limit(s,s.UserTeam,s.Year+1)-20_000_000,0)]}:c).ToList()};
        var result=ProManagement.Submit(s,"Reduce",[receiver.Football.PlayerId],0,x=>x with{Contracts=x.Contracts.Select(c=>c.PlayerId==receiver.Football.PlayerId?c with{Years=[new(s.Year+1,170_000_000,0)]}:c).ToList()});
        Assert.True(Assert.Single(result.GmDecisions).Approved);Assert.Equal(170_000_000,result.Contracts.Sum(c=>ProEconomy.CapHit(c,s.Year+1)));
    }
    [Fact] public void BonusHorizonOutsideSalaryRowsIsStillReserved()
    {
        var(s,qb,_)=Setup();s=s with{Contracts=s.Contracts.Where(c=>c.PlayerId==qb.Football.PlayerId).ToList()};
        var result=ProManagement.Submit(s,"Bonus commitment",[qb.Football.PlayerId],0,x=>x with{Contracts=x.Contracts.Select(c=>c with{SigningBonus=400_000_000,BonusYears=2,Years=c.Years.Select(y=>y with{ProratedBonus=0}).ToList()}).ToList()});
        var decision=Assert.Single(result.GmDecisions);Assert.False(decision.Approved);Assert.Contains((s.Year+1).ToString(),decision.Reason);Assert.Same(s.Contracts,result.Contracts);
    }
    [Fact] public void OtherUnitsCommitmentsRemainProtectedWhenOwnUnitIsWithinAllocation()
    {
        var(s,_,_)=Setup();var other=s.Players.First(p=>p.Football.TeamName==s.UserTeam&&p.Football.Position==Position.CB);
        s=s with{Contracts=s.Contracts.Append(new(other.Football.PlayerId,s.UserTeam,s.Year,1,0,[new(s.Year+1,ProFinance.Limit(s,s.UserTeam,s.Year+1)-20_000_000,0)])).ToList()};
        var free=s.Players.First(p=>p.Football.TeamName=="Free agent"&&p.Football.Position==Position.RB&&!s.Waivers.Any(w=>w.PlayerId==p.Football.PlayerId));
        var result=ProManagement.Submit(s,"Offer",[free.Football.PlayerId],35_000_000,x=>ProEconomy.Offer(x,x.UserTeam,free.Football.PlayerId,3,35_000_000,0));
        var decision=Assert.Single(result.GmDecisions);Assert.False(decision.Approved);Assert.Contains("shared team commitments",decision.Reason);Assert.Empty(result.Bids);
    }
    [Fact] public void CoordinatorCanReleaseOwnOverBudgetContractToRepairAllocation()
    {
        var(s,qb,_)=Setup();s=s with{Contracts=[new(qb.Football.PlayerId,s.UserTeam,s.Year,1,0,[new(s.Year,200_000_000,0)])]};
        var result=ProManagement.Submit(s,"Release",[qb.Football.PlayerId],0,x=>ProEconomy.Release(x,x.UserTeam,qb.Football.PlayerId,false));
        Assert.True(Assert.Single(result.GmDecisions).Approved);Assert.Equal("Free agent",result.Players.Single(p=>p.Football.PlayerId==qb.Football.PlayerId).Football.TeamName);
        Assert.Equal(0,ProEconomy.Used(result,s.UserTeam));
    }
    [Fact] public void HeadCoachRetainsWholeTeamAllocation()
    {
        var(s,qb,_)=Setup();s=s with{Career=s.Career! with{Role=StaffRole.HeadCoach}};
        var result=ProManagement.Submit(s,"Extend",[qb.Football.PlayerId],0,x=>ProEconomy.Extend(x,x.UserTeam,qb.Football.PlayerId,1,100_000_000));
        Assert.Empty(result.GmDecisions);Assert.Contains(result.Contracts,c=>c.PlayerId==qb.Football.PlayerId&&c.Years.Any(y=>y.Year==s.Year+1));
    }
}
