using DynastyManager.App;
using ProFootball.Core;
namespace ProFootball.App;
public sealed partial class FranchisePage
{
    async Task TradeOffers()
    {
        state=ProTradeAi.RefreshOffers(state!);await Save();var s=state!;
        body.Clear();body.Add(Action("‹ Front Office",()=>Open("Front Office")));body.Add(Text("Trade offers",25,true));
        body.Add(Text("CPU offers for your listed players. Review the player, future picks and cap impact before accepting. Offers expire after two advances; counters are limited to two.",12));
        var offers=ProTradeAi.OpenOffers(s);body.Add(Text($"{offers.Count} open offers",18,true));
        if(offers.Count==0)body.Add(Text("List a player on the trading block. Teams review the market as weeks advance."));
        body.Add(Action("Trading block",TradingBlock));
        foreach(var o in offers)
        {
            var p=s.Players.Single(p=>p.Football.PlayerId==o.PlayerId);var buyer=s.League.Teams.Single(t=>t.Name==o.Buyer);
            var picks=o.PickIds.Select(id=>s.Picks.Single(p=>p.Id==id)).ToArray();
            ProFranchise? preview=null;string? issue=null;
            try{preview=ProTradeAi.Execute(s,o.Buyer,o.Seller,o.PlayerId,o.PickIds);}catch(InvalidOperationException e){issue=e.Message;}
            var actions=new ActionRow();
            var accept=Action("Accept offer",async()=>
            {
                if(await GameDialogs.AlertAsync(this,"Accept trade offer?",$"Trade {p.Football.FullName} to {o.Buyer} for "+string.Join(", ",picks.Select(x=>$"{x.Year} round {x.Round}"))+"? This transfers the player and listed picks if the cap and roster review succeeds.","Accept","Cancel"))
                {await Update(x=>ProTradeAi.Respond(x,o.Id,true));await TradeOffers();}
            });accept.IsEnabled=preview is not null;if(!accept.IsEnabled)accept.Opacity=.45;actions.Add(accept);
            var counter=Action(o.Counters>=2?"Counters exhausted":"Ask for more",async()=>{await Update(x=>ProTradeAi.Respond(x,o.Id,false,true));await TradeOffers();});counter.IsEnabled=o.Counters<2;if(!counter.IsEnabled)counter.Opacity=.45;actions.Add(counter);
            actions.Add(Action("Decline offer",async()=>{await Update(x=>ProTradeAi.Respond(x,o.Id,false));await TradeOffers();}));
            var views=new List<View>{TeamCell(buyer),PlayerCell(p),Text($"You send {p.Football.FullName} · {p.Football.Position} · OVR {p.Football.OverallRating}"),Text("You receive",14,true),
                Table([("Year",55),("Round",55),("Original team",160)],picks.Select(x=>new View[]{Text(x.Year.ToString()),Text(x.Round.ToString()),Text(x.OriginalTeam,12)}))};
            if(preview is not null)
            {
                views.Add(Text($"Your available cap: {Money(ProEconomy.Room(s,s.UserTeam))} → {Money(ProEconomy.Room(preview,s.UserTeam))}",12));
                var added=preview.DeadCap.Where(d=>d.Team==s.UserTeam&&d.Year==s.Year).Sum(d=>d.Amount)-s.DeadCap.Where(d=>d.Team==s.UserTeam&&d.Year==s.Year).Sum(d=>d.Amount);
                views.Add(Text($"Current-year retained salary / bonus cap charges: {Dollars(added)}",12));
            }
            if(issue is not null)views.Add(Text(issue,12));
            views.Add(Text($"{Math.Max(0,o.ExpiresTick-s.TransactionTick)} advances remaining · Counters {o.Counters}/2",12));
            if(o.Response.Length>0)views.Add(Text(o.Response,12,true));views.Add(actions);body.Add(Panel("Offer from "+buyer.Name,views.ToArray()));
        }
        var history=s.TradeOffers.Where(o=>o.Status!=ProTradeOfferStatus.Open).TakeLast(10).Reverse().ToArray();
        if(history.Length>0){body.Add(Text("Recent offer decisions",18,true));body.Add(Table([("Year",50),("Team",170),("Player",160),("Result",90)],history.Select(o=>new View[]{Text(o.Year.ToString()),Text(o.Buyer,12),Text(s.Players.FirstOrDefault(p=>p.Football.PlayerId==o.PlayerId)?.Football.FullName??"Former player",12),Text(o.Status.ToString(),12)})));}
    }
}
