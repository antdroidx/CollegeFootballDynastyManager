using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
namespace ProFootball.Core;

public enum ProTradeOfferStatus { Open, Accepted, Declined, Expired }
public sealed record ProCpuTradeOffer(Guid Id,int Year,int CreatedTick,int ExpiresTick,string Buyer,string Seller,Guid PlayerId,List<Guid> PickIds)
{
    public ProTradeOfferStatus Status {get;init;}
    public int Counters {get;init;}
    public string Response {get;init;}="";
}

/// <summary>CPU discovery uses the existing trade transaction; previews and failed buyer cuts are immutable.</summary>
public static class ProTradeAi
{
    static bool Available(ProFranchise s,ProPlayer p)=>!p.Retired&&!p.Reserve&&!p.PracticeSquad&&p.Football.CurrentInjury is not {WeeksRemaining:>0}
        &&s.Contracts.Any(c=>c.Active&&c.PlayerId==p.Football.PlayerId&&ProContractInsights.PlayingEnd(c)>=s.Year)
        &&!s.UnsignedRookies.Any(r=>r.PlayerId==p.Football.PlayerId);
    public static bool Window(ProFranchise s)=>ProTradeMarket.IsOpen(s)&&(s.Phase!=ProPhase.Offseason||s.OffseasonStep==OffseasonStep.FreeAgency);
    static bool Recent(ProFranchise s,Guid id)=>s.TradeHistory.Any(h=>h.Year==s.Year&&h.TransactionTick>=0&&s.TransactionTick-h.TransactionTick<4&&(h.GivenPlayers.Contains(id)||h.ReceivedPlayers.Contains(id)));
    static string Role(ProPlayer p)=>(p.Football.Position==Position.OL||p.Football.Position.IsSpecialist())&&p.Football.NcaaPositionCode is int code?$"{p.Football.Position}/{code}":OffensiveLineService.Label(p.Football);
    static bool SafeDeparture(ProFranchise s,ProPlayer p)
    {
        if(p.PracticeSquad)return true;
        var next=s with{Players=s.Players.Where(x=>x.Football.PlayerId!=p.Football.PlayerId).ToList()};
        return ProRoster.Active(next,p.Football.TeamName)>=45&&ProCpu.Need(next,p.Football.TeamName,p.Football.Position)==0
            &&next.Players.Any(x=>x.Football.TeamName==p.Football.TeamName&&!x.PracticeSquad&&!x.Reserve&&!x.Retired&&Role(x)==Role(p));
    }
    static bool Upgrade(ProFranchise s,ProPlayer p,string buyer)
    {
        var role=Role(p);
        var best=s.Players.Where(x=>x.Football.TeamName==buyer&&!x.Retired&&!x.PracticeSquad&&!x.Reserve&&Role(x)==role)
            .Select(x=>x.Football.OverallRating).DefaultIfEmpty(0).Max();
        return p.Football.OverallRating>=best+3;
    }
    static IReadOnlyList<Guid> Package(ProFranchise s,ProPlayer p,string buyer,double minimum=0)
    {
        var value=ProTrade.PlayerValue(s,p,buyer);
        return ProTradeMarket.CounterPicks(s,buyer,Math.Max(value*1.05,minimum),[],value*1.30,s.Year+1,s.Year+3);
    }
    static long Pending(ProFranchise s,string team,int year)=>ProEconomy.PendingForYear(s,team,year);
    static void FutureBudget(ProFranchise before,ProFranchise after,string team)
    {
        var years=after.Contracts.Where(c=>c.Active&&c.Team==team).SelectMany(c=>c.Years.Select(y=>y.Year))
            .Concat(after.Contracts.Where(c=>c.Active&&c.Team==team).SelectMany(c=>Enumerable.Range(before.Year,Math.Max(0,c.SignedYear+c.BonusYears-before.Year))))
            .Concat(after.Bids.Where(b=>b.Team==team).SelectMany(b=>Enumerable.Range(after.Year,b.Years)))
            .Concat(after.DeadCap.Where(d=>d.Team==team).Select(d=>d.Year)).Where(y=>y>=before.Year).Append(before.Year).Distinct();
        // Camp exclusions cannot fund a trade that makes opening-day or future commitments illegal.
        var fullBefore=before with{Phase=ProPhase.RegularSeason};var fullAfter=after with{Phase=ProPhase.RegularSeason};
        foreach(var year in years)
        {
            var cost=ProFinance.Budget(fullAfter,team,year).TotalCommitments+Pending(after,team,year);
            var ceiling=Math.Max(ProFinance.Limit(before,team,year),ProFinance.Budget(fullBefore,team,year).TotalCommitments+Pending(before,team,year));
            if(cost>ceiling)throw new InvalidOperationException($"The {year} commitments exceed the team's planning budget.");
        }
    }
    public static ProFranchise Execute(ProFranchise s,string buyer,string seller,Guid playerId,IReadOnlyList<Guid> picks)
    {
        if(!Window(s))throw new InvalidOperationException("The trade market is closed at this stage.");
        var p=s.Players.Single(x=>x.Football.PlayerId==playerId&&x.Football.TeamName==seller);
        if(buyer==s.UserTeam||buyer==seller||!Available(s,p)||Recent(s,playerId)||!Upgrade(s,p,buyer))throw new InvalidOperationException("This player is unavailable for a CPU purchase.");
        if(seller!=s.UserTeam&&!SafeDeparture(s,p))throw new InvalidOperationException("The seller cannot spare this role or roster place.");
        if(picks.Count==0||picks.Any(id=>!s.Picks.Any(x=>x.Id==id&&x.Owner==buyer&&x.Year>s.Year&&x.Year<=s.Year+3&&ProTrade.Available(s,x))))
            throw new InvalidOperationException("The offered future picks are no longer available.");
        var value=ProTrade.PlayerValue(s,p,buyer);var price=picks.Sum(id=>ProTrade.PickValue(s,s.Picks.Single(x=>x.Id==id)));
        if(price>value*1.30)throw new InvalidOperationException("The CPU will not pay this much draft capital.");
        var prepared=s;
        if(ProRoster.CountForLimit(s,buyer)>=ProRoster.Limit(s))
        {
            var cut=s.Players.Where(x=>x.Football.TeamName==buyer&&!x.Retired&&!x.Reserve&&(x.PracticeSquad||x.Football.DepthChartOrder>1)&&SafeDeparture(s,x))
                .OrderBy(x=>x.Football.OverallRating).ThenBy(x=>ProEconomy.DeadOnRelease(s,x.Football.PlayerId)).FirstOrDefault();
            if(cut is null)throw new InvalidOperationException("The buyer cannot safely clear a roster place.");
            prepared=ProEconomy.Release(s,buyer,cut.Football.PlayerId);
        }
        var next=ProTrade.Propose(prepared,buyer,seller,[],[playerId],picks,[]);
        FutureBudget(s,next,buyer);FutureBudget(s,next,seller);
        return ProDepthChart.MakeCpuStarter(next,buyer,playerId);
    }
    static bool CanExecute(ProFranchise s,string buyer,ProPlayer p,IReadOnlyList<Guid> picks)
    {
        try{Execute(s,buyer,p.Football.TeamName,p.Football.PlayerId,picks);return true;}catch(InvalidOperationException){return false;}
    }
    static bool Current(ProFranchise s,ProCpuTradeOffer o)=>o.Status==ProTradeOfferStatus.Open&&o.Year==s.Year&&s.TransactionTick<=o.ExpiresTick&&Window(s)
        &&o.Seller==s.UserTeam&&s.Career?.Unemployed!=true&&s.TradeBlock.Contains(o.PlayerId)
        &&s.Players.Any(p=>p.Football.PlayerId==o.PlayerId&&p.Football.TeamName==o.Seller&&Available(s,p)&&ProCareer.Controls(s,p.Football.Position))
        &&o.PickIds.All(id=>s.Picks.Any(p=>p.Id==id&&p.Owner==o.Buyer&&ProTrade.Available(s,p)));
    public static IReadOnlyList<ProCpuTradeOffer> OpenOffers(ProFranchise s)=>s.TradeOffers.Where(o=>Current(s,o)).ToArray();
    public static ProFranchise RefreshOffers(ProFranchise s)=>s with{TradeOffers=s.TradeOffers.Select(o=>o.Status==ProTradeOfferStatus.Open&&!Current(s,o)?o with{Status=ProTradeOfferStatus.Expired,Response="This offer is no longer available."}:o).ToList()};
    public static ProFranchise Respond(ProFranchise s,Guid id,bool accept,bool counter=false)
    {
        s=RefreshOffers(s);var offer=s.TradeOffers.Single(o=>o.Id==id);
        if(!Current(s,offer))throw new InvalidOperationException("This offer has expired or its assets changed.");
        ProFranchise Replace(ProFranchise state,ProCpuTradeOffer updated)=>state with{TradeOffers=state.TradeOffers.Select(o=>o.Id==id?updated:o).ToList()};
        if(counter)
        {
            if(offer.Counters>=2)throw new InvalidOperationException("The CPU has finished negotiations; accept or decline the standing offer.");
            var p=s.Players.Single(p=>p.Football.PlayerId==offer.PlayerId);var price=offer.PickIds.Sum(id=>ProTrade.PickValue(s,s.Picks.Single(p=>p.Id==id)));
            var picks=Package(s,p,offer.Buyer,price*1.06+1);
            var improved=picks.Count>0&&CanExecute(s,offer.Buyer,p,picks);
            return Replace(s,offer with{Counters=offer.Counters+1,PickIds=improved?picks.ToList():offer.PickIds,Response=improved?"The CPU increased its offer.":"The CPU will keep its current offer."});
        }
        if(!accept)return Replace(s,offer with{Status=ProTradeOfferStatus.Declined,Response="Offer declined."});
        var next=ProManagement.Submit(s,"CPU trade offer",[offer.PlayerId],0,x=>Execute(x,offer.Buyer,offer.Seller,offer.PlayerId,offer.PickIds),sharedPicks:true);
        if(next.TradeHistory.Count==s.TradeHistory.Count)return next;
        return Replace(next,offer with{Status=ProTradeOfferStatus.Accepted,Response="Trade completed."});
    }
    public static ProFranchise Process(ProFranchise s)
    {
        if(s.LastTradeMarketTick==s.TransactionTick)return s;
        s=RefreshOffers(s with{LastTradeMarketTick=s.TransactionTick});
        if(!Window(s))return s;
        s=ProTradeMarket.Refresh(ProEconomy.EnsurePickInventory(s));
        var used=new HashSet<string>();var completed=0;
        var candidates=s.Players.Where(p=>s.TradeBlock.Contains(p.Football.PlayerId)&&Available(s,p)&&!Recent(s,p.Football.PlayerId))
            .OrderBy(p=>p.Football.TeamName==s.UserTeam?0:1).ThenByDescending(p=>p.TradeRequested).ThenByDescending(p=>p.Football.OverallRating).ThenBy(p=>p.Football.PlayerId).Take(24).ToArray();
        foreach(var p in candidates)
        {
            var seller=p.Football.TeamName;
            if(completed>=2&&seller!=s.UserTeam)continue;
            if(used.Contains(seller))continue;
            if(seller==s.UserTeam)
            {
                if(s.Career?.Unemployed==true||!ProCareer.Controls(s,p.Football.Position)||s.TradeOffers.Count(o=>o.Status==ProTradeOfferStatus.Open)>=3
                    ||s.TradeOffers.Any(o=>o.PlayerId==p.Football.PlayerId&&o.Year==s.Year&&s.TransactionTick-o.CreatedTick<4))continue;
            }
            else if(!SafeDeparture(s,p))continue;
            foreach(var buyer in s.League.Teams.Where(t=>t.Name!=seller&&t.Name!=s.UserTeam&&!used.Contains(t.Name))
                .OrderByDescending(t=>ProCpu.Need(s,t.Name,p.Football.Position)).ThenBy(t=>t.Id).Take(6))
            {
                if(!Upgrade(s,p,buyer.Name))continue;
                var picks=Package(s,p,buyer.Name);if(picks.Count==0||!CanExecute(s,buyer.Name,p,picks))continue;
                if(seller==s.UserTeam)
                {
                    var offer=new ProCpuTradeOffer(Guid.NewGuid(),s.Year,s.TransactionTick,s.TransactionTick+2,buyer.Name,seller,p.Football.PlayerId,picks.ToList());
                    s=s with{TradeOffers=s.TradeOffers.Append(offer).TakeLast(100).ToList()};
                }
                else{s=Execute(s,buyer.Name,seller,p.Football.PlayerId,picks);completed++;used.Add(seller);used.Add(buyer.Name);}
                break;
            }
        }
        return RefreshOffers(s);
    }
}
