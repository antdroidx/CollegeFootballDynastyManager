using DynastyManager.App;
using ProFootball.Core;
namespace ProFootball.App;
public sealed partial class FranchisePage
{
    void Inbox()
    {
        body.Add(Text("Franchise inbox",25,true));var notices=ProInbox.Build(state!);
        body.Add(Text("Current action items and recent GM decisions. Resolved alerts disappear; read status is saved.",12));
        body.Add(Action("Mark all read",async()=>{state=state! with{ReadNotificationIds=state.ReadNotificationIds.Concat(notices.Select(n=>n.Id)).Distinct().ToList()};await Save();Render();}));
        if(notices.Count==0)body.Add(Text("No current action items."));
        foreach(var notice in notices)
        {
            var n=notice;body.Add(Panel((state!.ReadNotificationIds.Contains(n.Id)?"":"● ")+n.Title,Text(n.Detail),Action("Review",async()=>{
                state=state! with{ReadNotificationIds=state.ReadNotificationIds.Append(n.Id).Distinct().ToList()};await Save();
                if(n.Id.StartsWith("trade-offer-"))await TradeOffers();
                else if(n.PlayerId is Guid id&&state.Players.FirstOrDefault(p=>p.Football.PlayerId==id) is {} player)await Player(player);
                else if(n.Id.StartsWith("options-"))await RookieOptions();else if(n.Id.StartsWith("retention-"))await Retention();else await Open(n.Section);
            })));
        }
    }
    Task Retention()
    {
        body.Clear();body.Add(Action("‹ Front Office",()=>Open("Front Office")));body.Add(Text("Expiring contracts & retention",25,true));
        var s=state!;var available=s.Phase==ProPhase.Offseason&&s.OffseasonStep==OffseasonStep.Retention;
        body.Add(Text(available?"Renew, tag or let players leave before you advance. Unresolved expiring contracts leave at the next offseason step.":"Retention decisions open when the season ends.",12));
        var auto=new Switch{IsToggled=s.AutoNegotiateExpiring};var row=new HorizontalStackLayout{Spacing=10,Children={auto,Text("Staff negotiate expiring contracts on advancement")}};auto.Toggled+=async(_,e)=>{state=state! with{AutoNegotiateExpiring=e.Value};await Save();};body.Add(row);
        var staff=Action("Ask staff to negotiate now",async()=>{state=await Task.Run(()=>ProRetention.Negotiate(state!,state!.UserTeam));await Save();await Retention();});staff.IsEnabled=available;body.Add(staff);
        var expiring=ProRetention.Expiring(s,s.UserTeam);body.Add(Text($"{expiring.Count} expiring contracts",18,true));
        body.Add(Table([("Pos",45),("Player",180),("OVR",50),("Age",45),("Demand / yr",100),("Decision",105),("Negotiate",100),("Release to FA",110)],expiring.Select(p=>{
            var letGo=s.LetExpire.Contains(p.Football.PlayerId);var negotiate=Action("Negotiate",async()=>{var terms=await ContractTerms(p,true);if(terms is {} o)await Manage("Retention extension",p.Football.PlayerId,0,x=>ProEconomy.Extend(x,x.UserTeam,p.Football.PlayerId,o.Years,o.Salary,o.Bonus));await Retention();});negotiate.IsEnabled=available&&ProCareer.Controls(s,p.Football.Position);
            var expire=Action(letGo?"Keep option":"Let expire",async()=>{state=state! with{LetExpire=letGo?state.LetExpire.Where(id=>id!=p.Football.PlayerId).ToList():state.LetExpire.Append(p.Football.PlayerId).Distinct().ToList()};await Save();await Retention();});expire.IsEnabled=available&&ProCareer.Controls(s,p.Football.Position);
            return new View[]{Text(p.Football.Position.ToString()),Link(p.Football.FullName,()=>Player(p)),Text(p.Football.OverallRating.ToString()),Text(p.Age.ToString()),Text(Money(ProOfferTerms.AgentDemand(s,p,true).Salary)),Text(letGo?"Will leave":"Undecided"),negotiate,expire};
        })));
        body.Add(Text("Franchise tags and restructures are available from each player’s contract actions. Staff respect cap limits and your coaching authority; some players may remain unresolved.",12));return Task.CompletedTask;
    }
}
