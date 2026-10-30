using DynastyManager.Core.Models;
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
        body.Add(Table([("Player",140),("Actions",180),("Demand / yr",95),("OVR",45),("Pos",40),("Age",40),("Ends",55),("Decision / eligibility",210)],expiring.Select(p=>
        {
            var letGo=s.LetExpire.Contains(p.Football.PlayerId);var issue=ProRetention.ExtensionIssue(s,p);
            var negotiate=Action("Negotiate",async()=>{var terms=await ContractTerms(p,true);if(terms is {} o)await Manage("Retention extension",p.Football.PlayerId,0,x=>ProEconomy.Extend(x,x.UserTeam,p.Football.PlayerId,o.Years,o.Salary,o.Bonus));await Retention();},"pro-retention-negotiate-"+p.Football.PlayerId);
            negotiate.IsEnabled=available&&issue is null&&ProCareer.Controls(s,p.Football.Position);
            negotiate.BackgroundColor=DynastyTheme.Accent;negotiate.TextColor=DynastyTheme.IsLight?Colors.White:Color.FromArgb("#0B1726");
            var expire=Action(letGo?"Keep option":"Let expire",async()=>{state=state! with{LetExpire=letGo?state.LetExpire.Where(id=>id!=p.Football.PlayerId).ToList():state.LetExpire.Append(p.Football.PlayerId).Distinct().ToList()};await Save();await Retention();});expire.IsEnabled=ProCareer.Controls(s,p.Football.Position);expire.BorderWidth=1;expire.BorderColor=DynastyTheme.ControlEdge;
            foreach(var button in new[]{negotiate,expire}){button.FontSize=11;button.Padding=new Thickness(6,4);button.WidthRequest=80;button.MinimumHeightRequest=44;button.BorderWidth=1;button.BorderColor=DynastyTheme.ControlEdge;}
            // A distinct surface plus a visible edge keeps the secondary action visible on either stripe.
            expire.BackgroundColor=DynastyTheme.Surface;expire.TextColor=DynastyTheme.Text;
            var actions=new HorizontalStackLayout{Spacing=6,Children={negotiate,expire}};
            var contract=s.Contracts.Last(c=>c.Active&&c.PlayerId==p.Football.PlayerId);
            var decision=Text(issue??(letGo?"Will leave":"Undecided"),12);
            if(issue is not null){var explain=new TapGestureRecognizer();explain.Tapped+=async(_,_)=>await GameDialogs.AlertAsync(this,p.Football.FullName,issue,"OK");decision.GestureRecognizers.Add(explain);SemanticProperties.SetDescription(decision,issue);}
            return new View[]{Link(p.Football.FullName,()=>Player(p)),actions,Text(Money(ProOfferTerms.AgentDemand(s,p,true).Salary)),Text(p.Football.OverallRating.ToString()),Text(p.Football.Position.DisplayName()),Text(p.Age.ToString()),Text(ProContractInsights.PlayingEnd(contract).ToString()),decision};
        })));
        body.Add(Text("Franchise tags and restructures are available from each player’s contract actions. Staff respect cap limits and your coaching authority; some players may remain unresolved.",12));return Task.CompletedTask;
    }
}
