using DynastyManager.App;
using ProFootball.Core;
namespace ProFootball.App;
public sealed partial class FranchisePage
{
    async Task RookieOptions()
    {
        state=ProRookieOptions.Prepare(state!);await Save();var s=state!;
        body.Clear();body.Add(Action("‹ Front Office",()=>Open("Front Office")));body.Add(Text("Rookie fifth-year options",25,true));
        body.Add(Text("First-round deals last four seasons. Decide after season three, through the following draft. Exercising guarantees the fourth-year salary and the full fifth-year salary.",12));
        var auto=new Switch{IsToggled=s.AutoManageRookieOptions};
        auto.Toggled+=async(_,e)=>{state=state! with{AutoManageRookieOptions=e.Value};await Save();};
        body.Add(new HorizontalStackLayout{Spacing=10,Children={auto,Text("Delegate my unit’s rookie options to staff")}});
        var pending=ProRookieOptions.Pending(s,s.UserTeam);
        var staff=Action("Ask staff to decide now",async()=>{await Update(x=>ProRookieOptions.ManageCpu(x,true,x.UserTeam));await RookieOptions();});
        staff.IsEnabled=pending.Count>0&&s.Career?.Unemployed!=true;body.Add(staff);
        body.Add(Text($"{pending.Count} pending decisions",18,true));
        if(pending.Count==0)body.Add(Text("No rookie options are due now. Completed decisions appear below."));
        foreach(var p in pending)
        {
            var c=s.Contracts.Last(c=>c.Active&&c.PlayerId==p.Football.PlayerId);var r=ProRookieOptions.Origin(p,c)!.Value;var q=ProRookieOptions.Quote(s,p);
            var fourth=c.Years.Single(y=>y.Year==r+3);var controlled=ProCareer.Controls(s,p.Football.Position);
            var actions=new ActionRow();
            var exercise=Action("Exercise option",async()=>{
                if(!await GameDialogs.AlertAsync(this,"Exercise fifth-year option?",$"{p.Football.FullName}: {r+4} salary {Dollars(q.Salary)}, fully guaranteed. {r+3} salary {Dollars(fourth.Salary)} also becomes fully guaranteed. Existing bonus cap charges remain.","Exercise","Cancel"))return;
                await Manage("Rookie option",p.Football.PlayerId,0,x=>ProRookieOptions.Decide(x,x.UserTeam,p.Football.PlayerId,true));await RookieOptions();});
            var decline=Action("Decline option",async()=>{
                if(!await GameDialogs.AlertAsync(this,"Decline fifth-year option?",p.Football.FullName+" keeps the original four-year deal. This option decision is final; a separate extension remains possible.","Decline","Cancel"))return;
                await Manage("Decline rookie option",p.Football.PlayerId,0,x=>ProRookieOptions.Decide(x,x.UserTeam,p.Football.PlayerId,false));await RookieOptions();});
            exercise.IsEnabled=decline.IsEnabled=controlled;actions.Add(exercise);actions.Add(decline);
            body.Add(Panel(p.Football.FullName,PlayerCell(p),Text($"{p.Football.Position} · OVR {p.Football.OverallRating} · {q.Tier} price · "+(controlled?"Your decision":"Staff-managed unit")),
                Table([("Season",65),("Salary",120),("Guaranteed now",130),("If exercised",130)],new[]{
                    new View[]{Text((r+3).ToString()),Text(Dollars(fourth.Salary)),Text(Dollars(fourth.Guaranteed)),Text(Dollars(fourth.Salary))},
                    new View[]{Text((r+4).ToString()),Text(Dollars(q.Salary)),Text("—"),Text(Dollars(q.Salary))}}),
                Text($"{r+4} option salary adds {Dollars(q.Salary)} to cap commitments; existing bonus allocation {Dollars(ProEconomy.BonusCharge(c,r+4))}. Projected team room before option: {Dollars(ProFinance.Budget(s,s.UserTeam,r+4).Available)}.",12),actions));
        }
        var decisions=s.RookieOptionDecisions.Where(d=>d.Team==s.UserTeam).OrderByDescending(d=>d.DecisionYear).ToArray();
        if(decisions.Length>0)body.Add(Table([("Year",55),("Player",210),("Decision",100),("Fifth-year salary",130)],decisions.Select(d=>new View[]{Text(d.DecisionYear.ToString()),Text(s.Players.FirstOrDefault(p=>p.Football.PlayerId==d.PlayerId)?.Football.FullName??"Former player"),Text(d.Exercised?"Exercised":"Declined"),Text(d.Exercised?Dollars(d.Salary):"—")})));
    }
}
