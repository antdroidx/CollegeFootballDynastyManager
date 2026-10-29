using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public class NegotiationTests
{
    [Fact] public void LowOfferGetsCounterAndCannotSign()
    {
        var s=ProLeagueService.Create("Seattle Seahawks");var p=s.Players.First(p=>p.Football.TeamName=="Free agent"&&!s.Waivers.Any(w=>w.PlayerId==p.Football.PlayerId));
        var market=Math.Max(1_000_000,ProEconomy.MarketSalary(p));var response=ProNegotiation.Evaluate(p,3,300_000,0);
        Assert.False(response.Accepted);Assert.True(response.CounterSalary>300_000);
        Assert.Same(s,ProEconomy.Sign(s,new(p.Football.PlayerId,s.UserTeam,3,300_000,0,s.Week)));
        Assert.True(ProNegotiation.Evaluate(p,3,market,0).Accepted);
    }
    [Fact] public void RestructureConservesCashAndTotalCapAndLocksSeason()
    {
        var s=ProLeagueService.Create("Seattle Seahawks");var p=s.Players.First(p=>p.Football.TeamName==s.UserTeam&&!p.PracticeSquad&&!p.Reserve);
        var contract=new ProContract(p.Football.PlayerId,s.UserTeam,s.Year,3,3_000_001,Enumerable.Range(0,3).Select(i=>new ContractYear(s.Year+i,10_000_000,5_000_000)).ToList());
        s=s with{Contracts=s.Contracts.Where(c=>c.PlayerId!=p.Football.PlayerId).Append(contract).ToList()};
        var changed=ProNegotiation.Restructure(s,s.UserTeam,p.Football.PlayerId,6_000_001);var c=changed.Contracts.Single(c=>c.PlayerId==p.Football.PlayerId);
        Assert.Equal(contract.Years.Sum(y=>y.Salary)+contract.SigningBonus,c.Years.Sum(y=>y.Salary)+c.SigningBonus);
        Assert.Equal(Enumerable.Range(s.Year,3).Sum(y=>ProEconomy.CapHit(contract,y)),Enumerable.Range(s.Year,3).Sum(y=>ProEconomy.CapHit(c,y)));
        Assert.True(ProEconomy.CapHit(c,s.Year)<ProEconomy.CapHit(contract,s.Year));
        Assert.Throws<InvalidOperationException>(()=>ProNegotiation.Restructure(changed,s.UserTeam,p.Football.PlayerId,1_000_000));
        Assert.Throws<InvalidOperationException>(()=>ProEconomy.Extend(changed,s.UserTeam,p.Football.PlayerId,1,50_000_000));
    }
    [Fact] public void AcceptedExtensionCannotRepeatWithinSeason()
    {
        var s=ProLeagueService.Create("Seattle Seahawks");var c=s.Contracts.First(c=>c.Team==s.UserTeam&&c.Years.Count==1);var p=s.Players.Single(p=>p.Football.PlayerId==c.PlayerId);
        var changed=ProEconomy.Extend(s,s.UserTeam,p.Football.PlayerId,1,ProOfferTerms.RoundUp(Math.Max(ProFinance.Minimum(s,p,ProContractInsights.PlayingEnd(c)+1,practice:false),ProEconomy.MarketSalary(s,p))));
        Assert.Throws<InvalidOperationException>(()=>ProEconomy.Extend(changed,s.UserTeam,p.Football.PlayerId,1,50_000_000));
    }
}
