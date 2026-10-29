using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public class TradeMarketTests
{
    [Fact] public void ListingPersistsAndUnlistingPreservesOtherPlayers()
    {
        var s=ProLeagueService.Create("Seattle Seahawks");var p=s.Players.First(p=>p.Football.TeamName==s.UserTeam&&!p.Retired);
        var listed=ProTradeMarket.List(s,s.UserTeam,p.Football.PlayerId,true);
        Assert.Empty(s.TradeBlock);Assert.Contains(p.Football.PlayerId,listed.TradeBlock);
        var restored=System.Text.Json.JsonSerializer.Deserialize<ProFranchise>(System.Text.Json.JsonSerializer.Serialize(listed))!;
        Assert.Contains(p.Football.PlayerId,restored.TradeBlock);
        Assert.DoesNotContain(p.Football.PlayerId,ProTradeMarket.List(restored,s.UserTeam,p.Football.PlayerId,false).TradeBlock);
    }
    [Fact] public void MarketRefreshIsStableAndCpuListsOnlySurplus()
    {
        var s=ProTradeMarket.Refresh(ProLeagueService.Create("Seattle Seahawks"));Assert.NotEmpty(s.TradeBlock);
        Assert.Equal(s.TradeBlock,ProTradeMarket.Refresh(s).TradeBlock);
        Assert.All(s.TradeBlock,id=>{var p=s.Players.Single(p=>p.Football.PlayerId==id);Assert.NotEqual(s.UserTeam,p.Football.TeamName);Assert.True(p.Football.DepthChartOrder>1);Assert.Equal(0,ProCpu.Need(s,p.Football.TeamName,p.Football.Position));});
    }
    [Fact] public void DeadlineAndOwnershipApplyToListings()
    {
        var s=ProLeagueService.Create("Seattle Seahawks");var own=s.Players.First(p=>p.Football.TeamName==s.UserTeam);var other=s.Players.First(p=>p.Football.TeamName!=s.UserTeam);
        Assert.Throws<InvalidOperationException>(()=>ProTradeMarket.List(s,s.UserTeam,other.Football.PlayerId,true));
        Assert.Throws<InvalidOperationException>(()=>ProTradeMarket.List(s with{Phase=ProPhase.RegularSeason,Week=10},s.UserTeam,own.Football.PlayerId,true));
    }
    [Fact] public void CounterUsesOwnedAvailablePicksAndCoversGap()
    {
        var s=ProLeagueService.Create("Seattle Seahawks");var first=s.Picks.First(p=>p.Owner==s.UserTeam);var picks=ProTradeMarket.CounterPicks(s,s.UserTeam,25,[first.Id]);
        Assert.NotEmpty(picks);Assert.DoesNotContain(first.Id,picks);
        Assert.All(picks,id=>Assert.Contains(s.Picks,p=>p.Id==id&&p.Owner==s.UserTeam&&ProTrade.Available(s,p)));
        Assert.True(ProTradeMarket.PackageValue(s,[],picks,s.UserTeam)>=25);
        Assert.Empty(ProTradeMarket.CounterPicks(s,s.UserTeam,1_000_000,[]));
    }
}
