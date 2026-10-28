using DynastyManager.App;
using ProFootball.Core;
namespace ProFootball.App;
public sealed partial class FranchisePage
{
    static string Dollars(long value)=>"$"+value.ToString("N0");
    View CapSummary(ProFranchise s)
    {
        var b=ProFinance.Budget(s,s.UserTeam);
        var details=new VerticalStackLayout{Spacing=6,IsVisible=false};
        details.Children.Add(Text("Adjusted team cap "+Dollars(b.AdjustedCap),16,true));
        details.Children.Add(Text("Base league cap "+Dollars(b.BaseCap)+" / Rollover "+Dollars(b.Rollover),12));
        details.Children.Add(Text("Other adjustments "+Dollars(b.OtherAdjustments),12));
        details.Children.Add(Text("Player commitments "+Dollars(b.PlayerCommitments),12));
        details.Children.Add(Text("Other cap charges "+Dollars(b.OtherCapCharges)+" / Dead money "+Dollars(b.DeadMoney),12));
        details.Children.Add(Text("Rollover reserved "+Dollars(b.RolloverReserved)+" / Offseason salary excluded "+Dollars(b.OffseasonSalaryExcluded),12));
        details.Children.Add(Text("International practice salary excluded "+Dollars(b.InternationalPracticeSalaryExcluded),12));
        details.Children.Add(Text("Available cap "+Dollars(b.Available)+" / Pending bids "+Dollars(ProEconomy.Pending(s,s.UserTeam)),12));
        var toggle=new Button{Text="Show cap breakdown",AutomationId="pro-cap-breakdown",BackgroundColor=DynastyTheme.Panel,TextColor=DynastyTheme.Text};
        toggle.Clicked+=(_,_)=>{details.IsVisible=!details.IsVisible;toggle.Text=details.IsVisible?"Hide cap breakdown":"Show cap breakdown";};
        var seasonRoom=ProFinance.Budget(s with{Phase=ProPhase.RegularSeason},s.UserTeam).Available;
        return Panel("Salary cap",Text(Money(ProCapInitialization.RoundForDisplay(b.Available))+" available",25,true),
            Text(s.Year+" budget "+Money(b.AdjustedCap)+" / Dead money "+Money(b.DeadMoney),14),
            Text("Used "+Money(b.TotalCommitments)+" / Pending offers "+Money(ProEconomy.Pending(s,s.UserTeam)),12),
            Text(b.OffseasonSalaryExcluded>0?"Season-start room "+Money(seasonRoom):"Available for roster moves",12),toggle,details);
    }
    View AnnualContractTable(ProContract c)=>Table([("Year",110),("Base salary",120),("Bonus / year",120),("Other cap",115),("Cap hit",125),("Guaranteed base",135)],ProContractInsights.Seasons(c).Select(y=>new View[]{Text(y.Year+(c.Years.Single(row=>row.Year==y.Year).IsVoid?" · Void":"")),Text(Dollars(y.Salary)),Text(Dollars(y.Bonus)),Text(Dollars(y.Other)),Text(Dollars(y.CapHit),14,true),Text(Dollars(y.Guaranteed))}));
    View ContractDetails(ProPlayer p,ProContract? c)
    {
        if(c is null)return Panel("Contract",Text("No active contract"));
        var details=new VerticalStackLayout{Spacing=8,IsVisible=false};
        details.Children.Add(AnnualContractTable(c));
        if(c.BonusPayments is {Count:>0})details.Children.Add(Panel("Bonus payments",c.BonusPayments.Select(b=>Text(b.Year+": "+Dollars(b.Amount)+(b.GuaranteedAtSigning?" · Guaranteed":" · Option payment"),12)).ToArray()));
        if(c.CashVesting is {Count:>0})details.Children.Add(Panel("Future guarantees",c.CashVesting.Select(v=>Text(v.PaymentYear+" pay: "+Dollars(v.GuaranteedCash)+" guaranteed after the "+v.AfterSeason+" season if retained",12)).ToArray()));
        details.Children.Add(Text("Void years affect the salary cap but do not keep the player under contract. Bonus allocations are cap charges, not extra yearly pay.",12));
        var toggle=new Button{Text="Show accounting details",BackgroundColor=DynastyTheme.Panel,TextColor=DynastyTheme.Text};
        toggle.Clicked+=(_,_)=>{details.IsVisible=!details.IsVisible;toggle.Text=details.IsVisible?"Hide accounting details":"Show accounting details";};
        var playing=c.Years.Where(y=>!y.IsVoid&&y.Year>=state!.Year).OrderBy(y=>y.Year).ToArray();
        return Panel("Contract",Text(ProContractInsights.Satisfaction(state!,p)+" · Playing contract through "+ProContractInsights.PlayingEnd(c),16,true),
            Text(c.ReportedAnnualValue is long apy?"Contract value / year "+Dollars(apy):"Average annual pay "+Dollars(ProContractInsights.AverageCash(c))),
            Table([("Season",65),("Pay",125),("Cap hit",125),("Guaranteed pay",140)],playing.Select(y=>new View[]{Text(y.Year.ToString()),Text(Dollars(ProContractInsights.AnnualCash(c,y))),Text(Dollars(ProEconomy.CapHit(c,y.Year)),14,true),Text(Dollars(ProBonusAccounting.TotalGuarantee(state!,c,y)))})),toggle,details);
    }
    View ContractComparisons(ProPlayer p)
    {
        var peers=ProContractInsights.Comparables(state!,p);
        return Panel("Position market · "+p.Football.Position,Text("Expected annual pay "+Dollars(ProOfferTerms.AgentDemand(state!,p).Salary)+(peers.Count==0?"": " · Comparable average "+Dollars((long)peers.Average(x=>(double)x.AverageCash))),14,true),
            peers.Count==0?Text("No comparable contracts available."):Table([("Player",180),("Team",180),("OVR",50),("Average / year",130),("Current cap",125)],peers.Select(x=>new View[]{Text(x.Name),Text(x.Team),Text(x.Overall.ToString()),Text(Dollars(x.AverageCash)),Text(Dollars(x.CurrentCap))})),Text("Comparisons use the same position and players within five overall points.",12));
    }
    async Task<(bool Release,bool PostJune1)> ConfirmRelease(ProPlayer p)
    {
        var result=new TaskCompletionSource<(bool Release,bool PostJune1)>();var closing=false;var page=new ContentPage{BackgroundColor=DynastyTheme.Background};
        var june=new Switch{IsEnabled=ProTermination.CanDesignate(state!,state!.UserTeam,p.Football.PlayerId),AutomationId="pro-june1-release"};
        var preview=new ContentView();
        void Draw(){var rows=ProContractInsights.Release(state!,p.Football.PlayerId,june.IsToggled);preview.Content=Panel("Cap impact of release",Table([("Season",65),("Keep: cap hit",130),("Release: dead cap",150),("Cap saved",130),("Season cash",130)],rows.Select(y=>new View[]{Text(y.Year.ToString()),Text(Dollars(y.Before)),Text(Dollars(y.DeadMoney)),Text(Dollars(y.Savings),14,true),Text(Dollars(y.SeasonCash))})));}
        june.Toggled+=(_,_)=>Draw();Draw();
        var release=new Button{Text="Release player",BackgroundColor=DynastyTheme.Panel,TextColor=DynastyTheme.Text};var cancel=new Button{Text="Cancel",BackgroundColor=DynastyTheme.Panel,TextColor=DynastyTheme.Text};
        async Task Close(bool value){if(closing)return;closing=true;await Navigation.PopModalAsync();result.TrySetResult((value,value&&june.IsToggled));}
        release.Clicked+=async(_,_)=>await Close(true);cancel.Clicked+=async(_,_)=>await Close(false);
        page.Disappearing+=(_,_)=>page.Dispatcher.Dispatch(()=>{if(!closing&&!Navigation.ModalStack.Contains(page))result.TrySetResult((false,false));});
        var c=state!.Contracts.LastOrDefault(c=>c.Active&&c.PlayerId==p.Football.PlayerId);
        page.Content=new ScrollView{Content=new VerticalStackLayout{Padding=16,Spacing=12,Children={Text("Release "+p.Football.FullName+"?",24,true),ContractDetails(p,c),
            new HorizontalStackLayout{Spacing=10,Children={june,Text("Use a June 1 designation · maximum two per team",12)}},preview,
            Text(june.IsEnabled?"Season cash includes amounts already paid this year. A designation holds current cap space until preseason, when June 1 savings take effect. Guaranteed pay is assessed immediately; its cash payments retain their schedule.":"Season cash includes amounts already paid this year. Bonus charges follow the current calendar. Guaranteed pay is assessed immediately; its cash payments retain their schedule.",12),new HorizontalStackLayout{Spacing=12,Children={cancel,release}}}}};
        await Navigation.PushModalAsync(page);return await result.Task;
    }
}
