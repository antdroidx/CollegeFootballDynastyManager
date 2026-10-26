using DynastyManager.App;
using ProFootball.Core;
namespace ProFootball.App;
public sealed partial class FranchisePage
{
    static string Dollars(long value)=>"$"+value.ToString("N0");
    View CapSummary(ProFranchise s)
    {
        var b=ProFinance.Budget(s,s.UserTeam);
        return Panel("Salary cap",Text("≈ "+Money(ProCapInitialization.RoundForDisplay(b.Available))+" available",25,true),
            Text("Adjusted team cap "+Dollars(b.AdjustedCap),16,true),
            Text("Base league cap "+Dollars(b.BaseCap)+" · Rollover "+Dollars(b.Rollover)),Text("Other adjustments "+Dollars(b.OtherAdjustments)),
            Text("Player commitments "+Dollars(b.PlayerCommitments)+" · Other cap charges "+Dollars(b.OtherCapCharges)),Text("Dead money "+Dollars(b.DeadMoney)+" · Rollover reserved "+Dollars(b.RolloverReserved)),
            Text("Available cap "+Dollars(b.Available)+" · Pending bids "+Dollars(ProEconomy.Pending(s,s.UserTeam)),12));
    }
    View AnnualContractTable(ProContract c)=>Table([("Year",55),("Base salary",120),("Bonus / year",120),("Other cap",115),("Cap hit",125),("Guaranteed base",135)],ProContractInsights.Seasons(c).Select(y=>new View[]{Text(y.Year.ToString()),Text(Dollars(y.Salary)),Text(Dollars(y.Bonus)),Text(Dollars(y.Other)),Text(Dollars(y.CapHit),14,true),Text(Dollars(y.Guaranteed))}));
    View ContractDetails(ProPlayer p,ProContract? c)
    {
        if(c is null)return Panel("Contract",Text("No active contract"));
        return Panel("Contract",Text(ProContractInsights.Satisfaction(state!,p)+" · Average annual cash "+Dollars(ProContractInsights.AverageCash(c)),16,true),
            AnnualContractTable(c),Text("Cap hit = base salary + allocated bonus + other cap charges. Guaranteed base is part of salary, not an additional charge. A signing bonus is paid up front and spread across contract years for cap purposes.",12));
    }
    View ContractComparisons(ProPlayer p)
    {
        var peers=ProContractInsights.Comparables(state!,p);
        return Panel("Position market · "+p.Football.Position,Text("Expected annual pay "+Dollars(ProOfferTerms.AgentDemand(state!,p).Salary)+(peers.Count==0?"": " · Comparable average "+Dollars((long)peers.Average(x=>(double)x.AverageCash))),14,true),
            peers.Count==0?Text("No comparable contracts available."):Table([("Player",180),("Team",180),("OVR",50),("Average / year",130),("Current cap",125)],peers.Select(x=>new View[]{Text(x.Name),Text(x.Team),Text(x.Overall.ToString()),Text(Dollars(x.AverageCash)),Text(Dollars(x.CurrentCap))})),Text("Comparisons use the same position and players within five overall points.",12));
    }
    async Task<bool> ConfirmRelease(ProPlayer p)
    {
        var result=new TaskCompletionSource<bool>();var closing=false;var page=new ContentPage{BackgroundColor=DynastyTheme.Background};
        var rows=ProContractInsights.Release(state!,p.Football.PlayerId);var release=new Button{Text="Release player",BackgroundColor=DynastyTheme.Panel,TextColor=DynastyTheme.Text};var cancel=new Button{Text="Cancel",BackgroundColor=DynastyTheme.Panel,TextColor=DynastyTheme.Text};
        async Task Close(bool value){if(closing)return;closing=true;await Navigation.PopModalAsync();result.TrySetResult(value);}
        release.Clicked+=async(_,_)=>await Close(true);cancel.Clicked+=async(_,_)=>await Close(false);
        page.Disappearing+=(_,_)=>page.Dispatcher.Dispatch(()=>{if(!closing&&!Navigation.ModalStack.Contains(page))result.TrySetResult(false);});
        var c=state!.Contracts.LastOrDefault(c=>c.Active&&c.PlayerId==p.Football.PlayerId);
        page.Content=new ScrollView{Content=new VerticalStackLayout{Padding=16,Spacing=12,Children={Text("Release "+p.Football.FullName+"?",24,true),ContractDetails(p,c),
            Panel("Cap impact of release",Table([("Season",65),("Keep: cap hit",130),("Release: dead cap",150),("Cap saved",130)],rows.Select(y=>new View[]{Text(y.Year.ToString()),Text(Dollars(y.Before)),Text(Dollars(y.DeadMoney)),Text(Dollars(y.Savings),14,true)}))),
            Text("Unallocated signing bonus accelerates into this season. Remaining guaranteed base stays charged in its scheduled season. Negative savings means the release costs cap space. Waivers apply where eligible.",12),new HorizontalStackLayout{Spacing=12,Children={cancel,release}}}}};
        await Navigation.PushModalAsync(page);return await result.Task;
    }
}
