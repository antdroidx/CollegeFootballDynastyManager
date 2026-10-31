using System.Text.Json;
using DynastyManager.Core.Models;
using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public class MarketTests
{
    static readonly ProPlayer Template=ProLeagueService.Snapshot().Players[0];
    static ProPlayer Player(Position position,int overall,int? code=null)
    {
        var p=Template;return p with{Football=p.Football with{Position=position,OverallRating=overall,NcaaPositionCode=code}};
    }
    [Fact] public void EveryPositionHasMonotoneBoundedPricingAcrossTheRatingScale()
    {
        foreach(var pos in Enum.GetValues<Position>())
        {
            var previous=0L;
            for(var rating=1;rating<=99;rating++)
            {
                var price=ProEconomy.MarketSalary(Player(pos,rating));Assert.True(price>=previous,$"{pos}/{rating}");Assert.InRange(price,300_000,70_000_000);previous=price;
            }
        }
    }
    [Fact] public void EliteAndBackupMarketsDifferByPositionAndKickingRole()
    {
        Assert.InRange(ProEconomy.MarketSalary(Player(Position.QB,95)),40_000_000,65_000_000);
        Assert.InRange(ProEconomy.MarketSalary(Player(Position.WR,95)),25_000_000,45_000_000);
        Assert.InRange(ProEconomy.MarketSalary(Player(Position.DE,95)),25_000_000,50_000_000);
        Assert.True(ProEconomy.MarketSalary(Player(Position.QB,75))<12_000_000);
        Assert.True(ProEconomy.MarketSalary(Player(Position.P,85,20))<5_000_000);
        Assert.True(ProEconomy.MarketSalary(Player(Position.K,85,19))>ProEconomy.MarketSalary(Player(Position.P,85,20)));
        Assert.InRange(ProEconomy.MarketSalary(Player(Position.K,99,19)),1_000_000,8_000_000);
    }
    [Fact] public void CapGrowthScalesTheCommonMarketWithoutUsingTeamCarryover()
    {
        var s=ProLeagueService.Create("Seattle Seahawks");var p=Player(Position.WR,90);var price=ProEconomy.MarketSalary(s,p);
        Assert.Equal(price*2,ProEconomy.MarketSalary(s with{SalaryCap=s.SalaryCap*2},p));
        Assert.Equal(price,ProEconomy.MarketSalary(s with{TeamCapAllocations=[]},p));
        Assert.True(ProNegotiation.Evaluate(p,3,price,0,price).Accepted);Assert.False(ProNegotiation.Evaluate(p,3,price/3,0,price).Accepted);
    }
    [Fact] public void CpuDoesNotBorrowMoneyNeededForUnfilledFutureRosterPlaces()
    {
        var s=ProLeagueService.Create("Seattle Seahawks");var p=s.Players.First(p=>p.Football.TeamName==s.UserTeam&&p.Experience>=6&&!p.PracticeSquad);
        var c=new ProContract(p.Football.PlayerId,s.UserTeam,s.Year-1,3,0,[new(s.Year,90_000_000,0),new(s.Year+1,30_000_000,0)]);
        s=s with{SalaryCap=100_000_000,TeamCapAllocations=[],Contracts=[c]};
        Assert.True(ProCpuBudget.DepthReserve(s,s.UserTeam,s.Year+1)>75_000_000);
        Assert.Same(s,ProCpuBudget.Relieve(s,s.UserTeam,40_000_000));Assert.Equal(0,c.LastChangedYear);
    }
    [Fact] public async Task NegotiatedSaveIsNotRepricedWhenMarketCurvesChange()
    {
        var s=ProLeagueService.Create("Seattle Seahawks");var c=s.Contracts[0] with{Estimated=false,LastChangedYear=s.Year,AccountingVersion=1,Years=[new(s.Year,7_123_456,2_000_003)]};
        s=s with{Contracts=[c]};var root=Path.Combine(Path.GetTempPath(),"pro-market-save-"+Guid.NewGuid());
        try
        {
            var store=new ProSaveStore(root);await store.Save(s);var loaded=await store.Load(s.Id);
            Assert.Equal(JsonSerializer.Serialize(s.Contracts),JsonSerializer.Serialize(loaded.Contracts));
        }
        finally{if(Directory.Exists(root))Directory.Delete(root,true);}
    }
    [Fact] public void FixedRookieAndPracticeDealsDoNotInflateAgentComparables()
    {
        var s=ProLeagueService.Create("Seattle Seahawks");var player=Player(Position.WR,85);var rookie=player with{RookieYear=s.Year,Experience=0,Football=player.Football with{PlayerId=Guid.NewGuid()}};
        var practice=rookie with{PracticeSquad=true,RookieYear=s.Year-5,Experience=5,Football=rookie.Football with{PlayerId=Guid.NewGuid()}};
        s=s with{Players=[player,rookie,practice],Contracts=[]};var before=ProOfferTerms.AgentDemand(s,player);
        var offered=s with{Contracts=new[]{rookie,practice}.Select(p=>new ProContract(p.Football.PlayerId,s.UserTeam,s.Year,4,0,[new(s.Year,200_000_000,0)]){Estimated=false,ReportedAnnualValue=200_000_000}).ToList()};
        Assert.Equal(before,ProOfferTerms.AgentDemand(offered,player));
    }
}
