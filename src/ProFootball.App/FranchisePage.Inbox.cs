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
        var s=state!;var available=s.Phase!=ProPhase.Offseason||s.OffseasonStep==OffseasonStep.Retention;
        body.Add(Text(available?"Negotiate eligible extensions now, or delegate to staff. New years follow the existing deal; current salary stays intact.":"Offseason negotiations resume at the retention stage.",12));
        var coordinator=ProCareer.Role(s)!=DynastyManager.Core.Models.StaffRole.HeadCoach;
        var auto=new Switch{IsToggled=s.AutoNegotiateExpiring,IsEnabled=s.Career?.Unemployed!=true};
        body.Add(new HorizontalStackLayout{Spacing=10,Children={auto,Text(coordinator?"Delegate my unit's expiring contracts":"Delegate expiring contracts on advancement",12)}});
        auto.Toggled+=async(_,e)=>{state=state! with{AutoNegotiateExpiring=e.Value};await Save();};
        var staff=Action("Ask staff to negotiate now",async()=>{state=await Task.Run(()=>ProRetention.Negotiate(state!,state!.UserTeam));await Save();await Retention();});staff.IsEnabled=available&&s.Career?.Unemployed!=true;body.Add(staff);
        var expiring=ProRetention.Expiring(s,s.UserTeam);body.Add(Text($"{expiring.Count} expiring contracts",18,true));
        foreach(var p in expiring)
        {
            var letGo=s.LetExpire.Contains(p.Football.PlayerId);var issue=ProRetention.ExtensionIssue(s,p);
            var negotiate=Action("Negotiate",async()=>{var terms=await ContractTerms(p,true);if(terms is {} o)await Manage("Retention extension",p.Football.PlayerId,0,x=>ProEconomy.Extend(x,x.UserTeam,p.Football.PlayerId,o.Years,o.Salary,o.Bonus));await Retention();},"pro-retention-negotiate-"+p.Football.PlayerId);
            negotiate.IsEnabled=available&&issue is null&&ProCareer.Controls(s,p.Football.Position);
            negotiate.BackgroundColor=DynastyTheme.Accent;negotiate.TextColor=DynastyTheme.IsLight?Colors.White:Color.FromArgb("#0B1726");
            var expire=Action(letGo?"Keep option":"Let expire",async()=>{state=state! with{LetExpire=letGo?state.LetExpire.Where(id=>id!=p.Football.PlayerId).ToList():state.LetExpire.Append(p.Football.PlayerId).Distinct().ToList()};await Save();await Retention();});expire.IsEnabled=ProCareer.Controls(s,p.Football.Position);expire.BorderWidth=1;expire.BorderColor=DynastyTheme.ControlEdge;
            var actions=new ActionRow();actions.Add(negotiate);actions.Add(expire);
            var contract=s.Contracts.Last(c=>c.Active&&c.PlayerId==p.Football.PlayerId);
            var card=new VerticalStackLayout{Spacing=8,Children={Link(p.Football.FullName,()=>Player(p)),Text($"{p.Football.Position} · OVR {p.Football.OverallRating} · Age {p.Age} · Ends {ProContractInsights.PlayingEnd(contract)}",12),Text($"Demand {Money(ProOfferTerms.AgentDemand(s,p,true).Salary)}/year · "+(letGo?"Will leave":"Undecided"),13,true),actions}};
            if(issue is not null)card.Add(Text(issue,12));
            body.Add(Panel("",card));
        }
        body.Add(Text("Franchise tags and restructures are available from each player’s contract actions. Staff respect cap limits and your coaching authority; some players may remain unresolved.",12));return Task.CompletedTask;
    }
}
