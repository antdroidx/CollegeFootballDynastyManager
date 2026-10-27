using DynastyManager.Core.Models;
using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public class PendingBidTests
{
    static (ProFranchise State,ProPlayer Player) Setup()
    {
        var s=ProLeagueService.Create("Seattle Seahawks");var p=s.Players.First(p=>p.Football.TeamName=="Free agent"&&!p.Retired);
        p=p with{Age=24,Experience=2,CreditedSeasons=2,RookieYear=2024,Football=p.Football with{OverallRating=55,PotentialRating=55}};
        return(s with{Contracts=[],DeadCap=[],Bids=[],Players=s.Players.Select(x=>x.Football.PlayerId==p.Football.PlayerId?p:x).ToList()},p);
    }
    [Fact] public void QuoteMatchesActualSignedChargesAcrossMinimumEscalationAndBonusRemainders()
    {
        var(s,p)=Setup();var bid=new FreeAgentBid(p.Football.PlayerId,s.UserTeam,4,1_250_000,1_000_003,0);
        var pending=ProEconomy.Offer(s,s.UserTeam,p.Football.PlayerId,bid.Years,bid.AnnualSalary,bid.Bonus);
        var signed=ProEconomy.Sign(s,bid);var c=Assert.Single(signed.Contracts);
        Assert.Equal(1_500_001,ProEconomy.Pending(pending,s.UserTeam));
        Assert.Equal(0,ProEconomy.BidCharge(s,bid,s.Year-1));Assert.Equal(0,ProEconomy.BidCharge(s,bid,s.Year+4));
        foreach(var year in Enumerable.Range(s.Year,4))Assert.Equal(ProEconomy.CapHit(c,year),ProEconomy.PendingForYear(pending,s.UserTeam,year));
        Assert.Equal(1_305_000,c.Years.Single(y=>y.Year==s.Year+2).Salary);Assert.Equal(1_350_000,c.Years.Single(y=>y.Year==s.Year+3).Salary);
        Assert.Equal(0,ProEconomy.PendingForYear(pending,s.UserTeam,s.Year,p.Football.PlayerId));
    }
    [Fact] public void FutureGmReviewIncludesTheActualEscalatingBidCharge()
    {
        var(s,p)=Setup();s=s with{Career=s.Career! with{Role=StaffRole.OffensiveCoordinator}};
        p=p with{Football=p.Football with{Position=Position.WR}};s=s with{Players=s.Players.Select(x=>x.Football.PlayerId==p.Football.PlayerId?p:x).ToList()};
        var own=s.Players.First(x=>x.Football.TeamName==s.UserTeam&&x.Football.Position==Position.QB);
        var year=s.Year+2;var ceiling=(long)(ProFinance.Limit(s,s.UserTeam,year)*.55);
        s=s with{Contracts=[new(own.Football.PlayerId,s.UserTeam,s.Year,4,0,[new(year,ceiling-1_270_000,0)])]};
        var next=ProManagement.Submit(s,"Minimum-priced offer",[p.Football.PlayerId],0,x=>ProEconomy.Offer(x,x.UserTeam,p.Football.PlayerId,4,1_250_000,0));
        Assert.False(Assert.Single(next.GmDecisions).Approved);Assert.Empty(next.Bids);Assert.Same(s.Contracts,next.Contracts);
        Assert.Contains(year.ToString(),next.GmDecisions[0].Reason);
    }
}
