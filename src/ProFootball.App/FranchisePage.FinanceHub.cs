using DynastyManager.App;
using ProFootball.Core;
namespace ProFootball.App;
public sealed partial class FranchisePage
{
    Task FinanceHub()
    {
        body.Clear();body.Add(Action("‹ Front Office",()=>Open("Front Office")));var s=state!;var team=s.UserTeam;body.Add(Text("Financial overview",25,true));
        var limit=ProFinance.Limit(s,team);var floor=ProFinance.Floor(s,team);
        body.Add(Cards(Panel("Salary cap",Text(Dollars(limit),24,true),Text("League cap "+Dollars(s.SalaryCap)+" · Rollover "+Dollars(limit-s.SalaryCap)),Text("Committed "+Dollars(ProEconomy.Used(s,team))+" · Available "+Dollars(ProEconomy.Room(s,team)))),Panel("Cash spending",Text(Dollars(floor.Paid),24,true),Text("Minimum over tracked spending window "+Dollars(floor.Required)),Text("Window ends "+floor.End+" · Cash spending and cap charges are separate."))));
        body.Add(Text("Future cap plan",20,true));
        var forecasts=Enumerable.Range(s.Year,4).Select(year=>(Year:year,Cap:ProFinance.Limit(s,team,year),Used:s.Contracts.Where(c=>c.Team==team).Sum(c=>ProEconomy.CapHit(c,year))+s.DeadCap.Where(d=>d.Team==team&&d.Year==year).Sum(d=>d.Amount))).ToArray();
        body.Add(Table([("Year",60),("Cap / projected",145),("Committed",135),("Available",135)],forecasts.Select(x=>new View[]{Text(x.Year.ToString()),Text(Dollars(x.Cap)),Text(Dollars(x.Used)),Text(Dollars(x.Cap-x.Used))})));
        body.Add(FootballBarChart.Create(forecasts.Select(x=>(x.Year.ToString(),x.Used/1_000_000d)),"0.0"));
        body.Add(Panel("How cap and cash work",Text("A signing bonus is paid up front but its cap charge is spread across contract years. The spending floor uses cash paid over a multi-year window, not annual cap usage. Cash-floor settlements do not create cap charges. Unused cap can carry into the next year.",12)));
        var carry=new Stepper{Minimum=0,Maximum=100,Increment=25,Value=s.CarryoverPercentage};var label=Text("Carry over "+s.CarryoverPercentage+"% of unused cap",16,true);
        carry.ValueChanged+=async(_,e)=>{state=state! with{CarryoverPercentage=(int)e.NewValue};label.Text="Carry over "+(int)e.NewValue+"% of unused cap";await Save();};body.Add(Panel("Rollover election",label,carry,Text("Applied when you finish retention and advance into the new league year.",12)));
        body.Add(Panel("League economy",Text("Next-year projected cap "+Dollars(ProFinance.ForecastCap(s,s.Year+1))+" · "+ProFinance.Growth(s,s.Year+1).ToString("+0.0%;-0.0%")+" revenue-driven change"),Text("New contract expectations grow with the league cap. Minimum salaries also depend on credited seasons.",12)));
        var ledger=s.FinancialHistory.Where(h=>h.Team==team).OrderByDescending(h=>h.Year).ToArray();
        if(ledger.Length>0)body.Add(Panel("Financial history",Table([("Year",60),("League cap",130),("Rollover in",125),("Cap spent",130),("Cash spent",130),("Rollover out",130),("Floor settlement",140)],ledger.Select(h=>new View[]{Text(h.Year.ToString()),Text(Dollars(h.BaseCap)),Text(Dollars(h.CarryIn)),Text(Dollars(h.CapSpent)),Text(Dollars(h.CashSpent)),Text(Dollars(h.CarryOut)),Text(Dollars(h.FloorPayment))}))));
        body.Add(Action("Review expiring contracts",Retention));return Task.CompletedTask;
    }
}
