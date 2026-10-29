using DynastyManager.App;
using ProFootball.Core;
namespace ProFootball.App;
public sealed partial class FranchisePage
{
    Task FinanceHub()
    {
        body.Clear();body.Add(Action("‹ Front Office",()=>Open("Front Office")));var s=state!;var team=s.UserTeam;body.Add(Text("Financial overview",25,true));
        var limit=ProFinance.Limit(s,team);var floor=ProFinance.Floor(s,team);
        body.Add(Cards(CapSummary(s),Panel("Projected cash spending",Text(Dollars(floor.Paid),24,true),Text("Minimum over tracked spending window "+Dollars(floor.Required)),Text("Window ends "+floor.End+" · Cash spending and cap charges are separate."))));
        var budget=ProCapDisplay.Budget(s,team);
        body.Add(Table([("Cap component",210),("Exact dollars",155)],new (string,long)[]{("Base league cap",budget.BaseCap),("Prior-year rollover",budget.Rollover),("Other adjustments",budget.OtherAdjustments),("Adjusted team cap",budget.AdjustedCap),("Salary counted",budget.CommittedSalary),("Allocated bonus",budget.AllocatedBonus),("Other cap charges",budget.OtherCapCharges),("Dead money",budget.DeadMoney),("Rollover reserved",budget.RolloverReserved),("Season cap space",budget.Available)}.Select(x=>new View[]{Text(x.Item1),Text(Dollars(x.Item2))})));

        body.Add(Text("Future cap plan",20,true));
        var forecasts=Enumerable.Range(s.Year,4).Select(year=>(Year:year,Cap:ProFinance.Limit(s,team,year),Used:ProCapDisplay.Budget(s,team,year).TotalCommitments)).ToArray();
        body.Add(Table([("Year",60),("Cap / projected",145),("Committed",135),("Available",135)],forecasts.Select(x=>new View[]{Text(x.Year.ToString()),Text(Dollars(x.Cap)),Text(Dollars(x.Used)),Text(Dollars(x.Cap-x.Used))})));
        body.Add(FootballBarChart.Create(forecasts.Select(x=>(x.Year.ToString(),x.Used/1_000_000d)),"0.0"));
        body.Add(Panel("How cap and cash work",Text("A signing bonus is paid up front but its cap charge is spread across contract years. The spending floor uses cash paid over a multi-year window, not annual cap usage. Cash-floor settlements do not create cap charges. Unused cap can carry into the next year.",12)));
        var measured=s.RolloverElections.Any(e=>e.Team==team&&e.Year==s.Year);
        var carry=new Stepper{Minimum=0,Maximum=100,Increment=25,Value=s.CarryoverPercentage,IsEnabled=!measured};var label=Text("Carry over "+s.CarryoverPercentage+"% of unused cap",16,true);
        carry.ValueChanged+=async(_,e)=>{state=state! with{CarryoverPercentage=(int)e.NewValue};label.Text="Carry over "+(int)e.NewValue+"% of unused cap";await Save();};body.Add(Panel("Rollover election",label,carry,Text("Measured after your final regular-season game. Reserved room is credited in the next league year.",12)));
        body.Add(Panel("League economy",Text("Next-year projected cap "+Dollars(ProFinance.ForecastCap(s,s.Year+1))+" · "+ProFinance.Growth(s,s.Year+1).ToString("+0.0%;-0.0%")+" revenue-driven change"),Text("New contract expectations grow with the league cap. Minimum salaries also depend on credited seasons.",12)));
        var ledger=s.FinancialHistory.Where(h=>h.Team==team).OrderByDescending(h=>h.Year).ToArray();
        if(ledger.Length>0)body.Add(Panel("Financial history",Table([("Year",60),("League cap",130),("Rollover in",125),("Adjustments",125),("Cap spent",130),("Cash spent",130),("Rollover out",130),("Floor settlement",140)],ledger.Select(h=>new View[]{Text(h.Year.ToString()),Text(Dollars(h.BaseCap)),Text(Dollars(h.CarryIn)),Text(Dollars(h.OtherAdjustments)),Text(Dollars(h.CapSpent)),Text(Dollars(h.CashSpent)),Text(Dollars(h.CarryOut)),Text(Dollars(h.FloorPayment))}))));
        body.Add(Action("Review expiring contracts",Retention));return Task.CompletedTask;
    }
}
