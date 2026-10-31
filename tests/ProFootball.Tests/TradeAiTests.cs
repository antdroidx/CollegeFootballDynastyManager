using System.Text.Json;
using DynastyManager.Core.Models;
using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public class TradeAiTests
{
    static (ProFranchise State,ProPlayer Player) Setup(bool cpuSeller=false)
    {
        var s=ProLeagueService.Create("Seattle Seahawks");
        s=s with{SalaryCap=2_000_000_000,TeamCapAllocations=[],Players=s.Players.Select(p=>p with{Football=p.Football with{OverallRating=55,PotentialRating=55}}).ToList()};
        var seller=cpuSeller?s.League.Teams.First(t=>t.Name!=s.UserTeam&&ProRoster.Active(s,t.Name)>45&&s.Players.Count(p=>p.Football.TeamName==t.Name&&!p.PracticeSquad&&!p.Reserve&&p.Football.Position==Position.WR)>5).Name:s.UserTeam;
        var player=s.Players.First(p=>p.Football.TeamName==seller&&!p.Retired&&!p.Reserve&&!p.PracticeSquad&&p.Football.Position==Position.WR&&p.Football.CurrentInjury is null);
        player=player with{Age=25,TradeRequested=cpuSeller,Football=player.Football with{OverallRating=82,PotentialRating=82,DepthChartOrder=2}};
        s=s with{Players=s.Players.Select(p=>p.Football.PlayerId==player.Football.PlayerId?player:p).ToList(),TradeBlock=[player.Football.PlayerId]};
        return(s,player);
    }
    static (ProFranchise State,ProCpuTradeOffer Offer) Offer()
    {
        var(s,_)=Setup();var next=ProTradeAi.Process(s);return(next,Assert.Single(next.TradeOffers));
    }
    [Fact] public void CpuOfferNeverSilentlyMovesUserAssetsAndMarketTickIsIdempotent()
    {
        var(s,p)=Setup();var next=ProTradeAi.Process(s);var offer=Assert.Single(next.TradeOffers);
        Assert.Equal(s.UserTeam,offer.Seller);Assert.Equal(p.Football.PlayerId,offer.PlayerId);
        Assert.Same(s.Players,next.Players);Assert.Same(s.Contracts,next.Contracts);Assert.Equal(s.Picks,next.Picks);Assert.Empty(next.TradeHistory);
        Assert.All(offer.PickIds,id=>Assert.Contains(s.Picks,x=>x.Id==id&&x.Owner==offer.Buyer&&x.Year>s.Year&&x.Year<=s.Year+3));
        var price=ProTradeMarket.PackageValue(s,[],offer.PickIds,s.UserTeam);var value=ProTrade.PlayerValue(s,p,offer.Buyer);
        Assert.InRange(price,value*1.05,value*1.30);Assert.Same(next,ProTradeAi.Process(next));
        if(Environment.GetEnvironmentVariable("PRO_TRADE_UI_FIXTURE") is string output)
        {
            var fixtures=new[]{next with{Id=Guid.NewGuid(),Name="Synthetic trade QA accept",Games=[]},next with{Id=Guid.NewGuid(),Name="Synthetic trade QA decline",Games=[]}};
            using var file=File.Create(output);using var gzip=new System.IO.Compression.GZipStream(file,System.IO.Compression.CompressionLevel.SmallestSize);
            JsonSerializer.Serialize(gzip,fixtures,new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.CamelCase});
        }
    }
    [Fact] public void AcceptedOfferTransfersOncePreservesTotalBonusDebtAndStartsCpuUpgrade()
    {
        var(s,o)=Offer();var before=Enumerable.Range(s.Year,8).Sum(y=>s.League.Teams.Sum(t=>ProFinance.Budget(s with{Phase=ProPhase.RegularSeason},t.Name,y).TotalCommitments));
        var next=ProTradeAi.Respond(s,o.Id,true);Assert.Equal(ProTradeOfferStatus.Accepted,next.TradeOffers[0].Status);
        var player=next.Players.Single(p=>p.Football.PlayerId==o.PlayerId);Assert.Equal(o.Buyer,player.Football.TeamName);Assert.Equal(1,player.Football.DepthChartOrder);
        Assert.All(o.PickIds,id=>Assert.Equal(s.UserTeam,next.Picks.Single(p=>p.Id==id).Owner));Assert.Single(next.TradeHistory);Assert.Equal(s.TransactionTick,next.TradeHistory[0].TransactionTick);
        Assert.Equal(before,Enumerable.Range(s.Year,8).Sum(y=>next.League.Teams.Sum(t=>ProFinance.Budget(next with{Phase=ProPhase.RegularSeason},t.Name,y).TotalCommitments)));
        Assert.Throws<InvalidOperationException>(()=>ProTradeAi.Respond(next,o.Id,true));
    }
    [Fact] public void ExpiryAndChangedOwnershipNeverMoveAssets()
    {
        var(s,o)=Offer();var expired=ProTradeAi.RefreshOffers(s with{TransactionTick=o.ExpiresTick+1});Assert.Equal(ProTradeOfferStatus.Expired,expired.TradeOffers[0].Status);
        Assert.Throws<InvalidOperationException>(()=>ProTradeAi.Respond(expired,o.Id,true));Assert.Same(s.Players,expired.Players);
        var changed=s with{Picks=s.Picks.Select(p=>p.Id==o.PickIds[0]?p with{Owner=s.UserTeam}:p).ToList()};Assert.Equal(ProTradeOfferStatus.Expired,ProTradeAi.RefreshOffers(changed).TradeOffers[0].Status);
        Assert.Throws<InvalidOperationException>(()=>ProTradeAi.Respond(changed,o.Id,true));
    }
    [Fact] public void CountersAreLimitedAndDeclinesDoNotImmediatelyRegenerate()
    {
        var(s,o)=Offer();var counter=ProTradeAi.Respond(s,o.Id,false,true);counter=ProTradeAi.Respond(counter,o.Id,false,true);
        Assert.Equal(2,counter.TradeOffers[0].Counters);Assert.Same(s.Players,counter.Players);Assert.Same(s.Picks,counter.Picks);
        Assert.Throws<InvalidOperationException>(()=>ProTradeAi.Respond(counter,o.Id,false,true));
        var declined=ProTradeAi.Respond(counter,o.Id,false);var next=ProTradeAi.Process(declined with{TransactionTick=s.TransactionTick+1});
        Assert.DoesNotContain(next.TradeOffers,x=>x.Status==ProTradeOfferStatus.Open);Assert.Equal(ProTradeOfferStatus.Declined,next.TradeOffers[0].Status);
        var loaded=JsonSerializer.Deserialize<ProFranchise>(JsonSerializer.Serialize(next))!;Assert.Equal(JsonSerializer.Serialize(next.TradeOffers),JsonSerializer.Serialize(loaded.TradeOffers));Assert.Equal(next.LastTradeMarketTick,loaded.LastTradeMarketTick);
    }
    [Fact] public async Task SavedOfferRetainsAuthorityAndCanBeAcceptedAfterReload()
    {
        var(s,o)=Offer();var root=Path.Combine(Path.GetTempPath(),"pro-trade-offer-"+Guid.NewGuid());
        try
        {
            var store=new ProSaveStore(root);await store.Save(s);var loaded=await store.Load(s.Id);
            Assert.Single(ProTradeAi.OpenOffers(loaded));var result=ProTradeAi.Respond(loaded,o.Id,true);
            await store.Save(result);var again=await store.Load(s.Id);
            Assert.Equal(ProTradeOfferStatus.Accepted,again.TradeOffers[0].Status);Assert.Equal(o.Buyer,again.Players.Single(p=>p.Football.PlayerId==o.PlayerId).Football.TeamName);
            Assert.All(o.PickIds,id=>Assert.Equal(s.UserTeam,again.Picks.Single(p=>p.Id==id).Owner));
        }
        finally{if(Directory.Exists(root))Directory.Delete(root,true);}
    }
    [Fact] public void DeadlineStageAndCoordinatorUnitRestrictionsApply()
    {
        var(s,p)=Setup();Assert.Empty(ProTradeAi.Process(s with{Career=s.Career! with{Role=StaffRole.DefensiveCoordinator}}).TradeOffers);
        Assert.Empty(ProTradeAi.Process(s with{Phase=ProPhase.RegularSeason,Week=10}).TradeOffers);
        Assert.Empty(ProTradeAi.Process(s with{Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.Draft}).TradeOffers);
        var offered=ProTradeAi.Process(s);var changed=offered with{Career=offered.Career! with{Role=StaffRole.DefensiveCoordinator}};
        Assert.Throws<InvalidOperationException>(()=>ProTradeAi.Respond(changed,offered.TradeOffers[0].Id,true));
        Assert.Equal(s.UserTeam,p.Football.TeamName);
    }
    [Fact] public void CpuToCpuTradeRetainsSellerDepthAndCannotImmediatelyFlipPlayer()
    {
        var(s,p)=Setup(true);var next=ProTradeAi.Process(s);var trade=Assert.Single(next.TradeHistory);
        Assert.DoesNotContain(s.UserTeam,new[]{trade.From,trade.To});var moved=next.Players.Single(x=>x.Football.PlayerId==p.Football.PlayerId);
        Assert.NotEqual(p.Football.TeamName,moved.Football.TeamName);Assert.Equal(1,moved.Football.DepthChartOrder);
        Assert.True(ProRoster.Active(next,p.Football.TeamName)>=45);Assert.Equal(0,ProCpu.Need(next,p.Football.TeamName,p.Football.Position));
        var picks=next.Picks.Where(x=>x.Owner!=s.UserTeam&&x.Owner!=moved.Football.TeamName).Take(1).ToArray();
        Assert.Throws<InvalidOperationException>(()=>ProTradeAi.Execute(next,picks[0].Owner,moved.Football.TeamName,moved.Football.PlayerId,[picks[0].Id]));
    }
    static ProFranchise FullBuyer(ProFranchise s,string buyer)
    {
        var seed=s.Players.First(p=>p.Football.TeamName==buyer);var players=s.Players.ToList();var contracts=s.Contracts.ToList();
        while(players.Count(p=>p.Football.TeamName==buyer&&!p.Retired&&!p.Reserve)<90)
        {
            var id=Guid.NewGuid();players.Add(seed with{PracticeSquad=true,Football=seed.Football with{PlayerId=id,OverallRating=10,DepthChartOrder=99}});
            contracts.Add(new(id,buyer,s.Year,1,0,[new(s.Year,0,0)]));
        }
        return s with{Players=players,Contracts=contracts};
    }
    [Fact] public void FullBuyerCanAtomicallyReleaseLegalSurplusAndAcquireUpgrade()
    {
        var(s,o)=Offer();s=FullBuyer(s,o.Buyer);var next=ProTradeAi.Respond(s,o.Id,true);
        Assert.Equal(90,ProRoster.CountForLimit(next,o.Buyer));Assert.Single(next.TradeHistory);
        var released=s.Players.Where(p=>p.Football.TeamName==o.Buyer&&next.Players.Single(x=>x.Football.PlayerId==p.Football.PlayerId).Football.TeamName=="Free agent").ToArray();
        Assert.Single(released);Assert.True(released[0].PracticeSquad);Assert.Equal(10,released[0].Football.OverallRating);
        Assert.True(ProEconomy.Room(next,o.Buyer)>=0);Assert.Equal(1,next.Players.Single(p=>p.Football.PlayerId==o.PlayerId).Football.DepthChartOrder);
    }
    [Theory] [InlineData(Position.OL,9,5)] [InlineData(Position.K,19,20)] [InlineData(Position.P,20,19)]
    public void CpuCannotSellItsOnlySpecificLineOrSpecialistRole(Position position,int targetCode,int otherCode)
    {
        var(s,p)=Setup(true);var seller=p.Football.TeamName;p=p with{Football=p.Football with{Position=position,NcaaPositionCode=targetCode}};
        s=s with{Players=s.Players.Select(x=>x.Football.PlayerId==p.Football.PlayerId?p:x.Football.TeamName==seller&&x.Football.Position==position
            ?position.IsSpecialist()?x with{Football=x.Football with{Position=Position.TE,NcaaPositionCode=4}}:x with{Football=x.Football with{NcaaPositionCode=otherCode}}
            :x).ToList()};
        var buyer=s.League.Teams.First(t=>t.Name!=s.UserTeam&&t.Name!=seller).Name;var pick=s.Picks.First(x=>x.Owner==buyer);
        var error=Assert.Throws<InvalidOperationException>(()=>ProTradeAi.Execute(s,buyer,seller,p.Football.PlayerId,[pick.Id]));
        Assert.Contains("cannot spare",error.Message);
    }
    [Fact] public void FutureBudgetFailureRollsBackPreparedBuyerCutAndEveryAsset()
    {
        var(s,o)=Offer();var buyer=o.Buyer;s=FullBuyer(s,buyer);
        s=s with{DeadCap=[new(buyer,s.Year+1,Guid.NewGuid(),2_000_000_000)]};
        var original=JsonSerializer.Serialize(s);
        var error=Assert.Throws<InvalidOperationException>(()=>ProTradeAi.Execute(s,o.Buyer,o.Seller,o.PlayerId,o.PickIds));
        Assert.Contains("planning budget",error.Message);
        Assert.Equal(original,JsonSerializer.Serialize(s));Assert.Empty(s.TradeHistory);
    }
    [Fact] public void PickPackagesRespectMaximumSpendAndThreeYearWindow()
    {
        var(s,_)=Setup();var buyer=s.League.Teams.First(t=>t.Name!=s.UserTeam).Name;
        Assert.Empty(ProTradeMarket.CounterPicks(s,buyer,100,[],99,s.Year+1,s.Year+3));
        var package=ProTradeMarket.CounterPicks(s,buyer,300,[],390,s.Year+1,s.Year+3);Assert.NotEmpty(package);
        Assert.InRange(ProTradeMarket.PackageValue(s,[],package,s.UserTeam),300,390);Assert.InRange(package.Count,1,3);
    }
}
