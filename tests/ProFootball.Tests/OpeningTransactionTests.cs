using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public class OpeningTransactionTests
{
    [Theory][InlineData(false)][InlineData(true)]
    public void ConfirmedTradesMovePlayersAndCompensationWithContractAssignment(bool sourced)
    {
        var s=ProLeagueService.Create("Seattle Seahawks",sourcedContracts:sourced);
        Assert.Equal(ProOpeningTransactions.ObservedOn,s.OpeningTransactionsSnapshot);
        foreach(var trade in ProOpeningTransactions.Trades)
        {
            var from=s.League.Teams.Single(t=>t.Abbreviation==trade.From).Name;
            var to=s.League.Teams.Single(t=>t.Abbreviation==trade.To).Name;
            var p=Assert.Single(s.Players,p=>p.Football.FullName==trade.Player);
            Assert.Equal(to,p.Football.TeamName);
            Assert.Single(s.Contracts,c=>c.Active&&c.PlayerId==p.Football.PlayerId&&c.Team==to);
            Assert.DoesNotContain(s.Contracts,c=>c.Active&&c.PlayerId==p.Football.PlayerId&&c.Team==from);
            foreach(var pick in trade.Picks)Assert.Equal(from,Assert.Single(s.Picks,p=>p.Year==pick.Year&&p.Round==pick.Round&&p.OriginalTeam==to).Owner);
            Assert.Contains(s.Contracts,c=>!c.Active&&c.PlayerId==p.Football.PlayerId&&c.Team==from);
            Assert.True(ProFinance.Budget(s,to).TotalCommitments>=0);
        }
        Assert.Equal(672,s.Picks.Count);Assert.Equal(672,s.Picks.Select(p=>p.Id).Distinct().Count());
        Assert.Equal(DynastyManager.Core.Models.Position.DT,s.Players.Single(p=>p.Football.FullName=="DeForest Buckner").Football.Position);
    }
    [Fact]public void InventoryMaintenanceDoesNotReplayOpeningOwnership()
    {
        var s=ProLeagueService.Create("Seattle Seahawks");var p=s.Picks.Single(p=>p.Year==2027&&p.Round==3&&p.OriginalTeam=="Seattle Seahawks");
        s=s with{Picks=s.Picks.Select(x=>x.Id==p.Id?x with{Owner="Miami Dolphins"}:x).ToList()};
        var next=ProEconomy.EnsurePickInventory(s);Assert.Equal("Miami Dolphins",next.Picks.Single(x=>x.Id==p.Id).Owner);
    }
    [Fact]public async Task LoadingAnOlderNegotiatedSaveDoesNotApplyRealTrades()
    {
        var s=ProLeagueService.Create("Seattle Seahawks",sourcedContracts:true,applyOpeningTrades:false);
        var nelson=s.Players.Single(p=>p.Football.FullName=="Quenton Nelson");
        s=s with{Contracts=s.Contracts.Select(c=>c.Active&&c.PlayerId==nelson.Football.PlayerId?c with{LastChangedYear=s.Year}:c).ToList()};
        var before=System.Text.Json.JsonSerializer.Serialize(s.Contracts);var dir=Path.Combine(Path.GetTempPath(),"old-opening-"+Guid.NewGuid());
        try{var store=new ProSaveStore(dir);await store.Save(s);var next=await store.Load(s.Id);Assert.Null(next.OpeningTransactionsSnapshot);Assert.Equal("Indianapolis Colts",next.Players.Single(p=>p.Football.PlayerId==nelson.Football.PlayerId).Football.TeamName);Assert.Equal(before,System.Text.Json.JsonSerializer.Serialize(next.Contracts));Assert.Equal(s.Picks,next.Picks);Assert.Equal(s.DeadCap,next.DeadCap);}
        finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
    }

}
