using DynastyManager.App;
using ProFootball.Core;
namespace ProFootball.App;
public sealed partial class FranchisePage
{
    async Task<(int Years,long Salary,long Bonus)?> ContractTerms(ProPlayer player,bool extension=false)
    {
        var completion=new TaskCompletionSource<(int Years,long Salary,long Bonus)?>(TaskCreationOptions.RunContinuationsAsynchronously);var closing=false;
        var page=new ContentPage {Title="Contract negotiation",BackgroundColor=DynastyTheme.Background};
        var demand=ProOfferTerms.AgentDemand(state!,player,extension);
        var years=new Stepper {Minimum=1,Maximum=5,Increment=1,Value=demand.Years,AutomationId="contract-years"};
        var salary=new Stepper {Minimum=ProOfferTerms.RoundUp(ProFinance.Minimum(state!,player,practice:false)),Maximum=500_000_000,Increment=ProOfferTerms.MoneyStep,Value=demand.Salary,AutomationId="contract-salary"};
        var bonus=new Stepper {Minimum=0,Maximum=2_500_000_000,Increment=ProOfferTerms.MoneyStep,Value=demand.Bonus,AutomationId="contract-bonus"};
        View Control(string title,Stepper step,bool money)
        {
            var value=Text(money?Money((long)step.Value):step.Value.ToString("0"),20,true);
            step.ValueChanged+=(_,e)=>value.Text=money?Money((long)e.NewValue):e.NewValue.ToString("0");
            SemanticProperties.SetDescription(step,title+(money?", increments of $250,000":", increments of one year"));
            var row=new Grid {ColumnSpacing=12,ColumnDefinitions=new(){new(GridLength.Star),new(GridLength.Auto)}};row.Add(value,0,0);row.Add(step,1,0);
            return new VerticalStackLayout {Spacing=4,Children={Text(title),row}};
        }
        var annualPreview=new ContentView{IsVisible=false};
        var details=new Button{Text="Show yearly cap breakdown",AutomationId="contract-yearly-details"};details.Clicked+=(_,_)=>{annualPreview.IsVisible=!annualPreview.IsVisible;details.Text=annualPreview.IsVisible?"Hide yearly cap breakdown":"Show yearly cap breakdown";};
        var response=Text("",14,true);var preview=Text("",12);var submit=new Button {Text=extension?"Agree extension":"Submit offer",AutomationId="contract-submit",BackgroundColor=DynastyTheme.Accent,TextColor=DynastyTheme.Background};
        var counter=new Button {Text="Use player's counter",AutomationId="contract-counter"};var cancel=new Button {Text="Cancel"};
        bool Terms(out int y,out long a,out long b) {y=(int)years.Value;a=(long)salary.Value;b=(long)bonus.Value;return y is >=1 and <=5&&a is >=500_000 and <=500_000_000&&b>=0&&b<=a*y;}
        void Refresh()
        {
            submit.IsEnabled=Terms(out var y,out var a,out var b)&&a>=ProFinance.Minimum(state!,player,practice:false);
            if(!submit.IsEnabled){response.Text="Bonus cannot exceed total base salary. Reduce the bonus or increase salary/years.";return;}
            var projection=ProContractInsights.ProjectOffer(state!,player,y,a,b,extension);annualPreview.Content=AnnualContractTable(projection);
            var first=extension?ProContractInsights.PlayingEnd(state!.Contracts.Last(c=>c.Active&&c.PlayerId==player.Football.PlayerId))+1:state!.Year;var total=projection.Years.Where(v=>v.Year>=first).Sum(v=>v.Salary);
            var result=ProNegotiation.Evaluate(player,y,a,b,Math.Max(ProFinance.Minimum(state!,player,practice:false),ProEconomy.MarketSalary(state!,player)));response.Text=result.Message+$" Counter salary: ${ProOfferTerms.RoundUp(result.CounterSalary):N0}/year.";
            preview.Text=$"New-term base salary ${total:N0} + bonus ${b:N0} · New cash ${total+b:N0}\n"+(extension?"Existing contract years remain. New bonus starts in the applicable league year and is prorated over up to five years.":$"First-year cap estimate ${a+(b+y-1)/y:N0} · Season cap space ${ProCapDisplay.Room(state!,state!.UserTeam):N0}");
        }
        years.ValueChanged+=(_,_)=>Refresh();salary.ValueChanged+=(_,_)=>Refresh();bonus.ValueChanged+=(_,_)=>Refresh();
        counter.Clicked+=(_,_)=>{if(Terms(out var y,out var a,out var b))salary.Value=Math.Clamp(ProOfferTerms.RoundUp(ProNegotiation.Evaluate(player,y,a,b,Math.Max(ProFinance.Minimum(state!,player,practice:false),ProEconomy.MarketSalary(state!,player))).CounterSalary),(long)salary.Minimum,500_000_000);};
        cancel.Clicked+=async(_,_)=>{if(closing)return;closing=true;await Navigation.PopModalAsync();completion.TrySetResult(null);};
        submit.Clicked+=async(_,_)=>{if(closing)return;if(!Terms(out var y,out var a,out var b))return;closing=true;await Navigation.PopModalAsync();completion.TrySetResult((y,a,b));};
        page.Disappearing+=(_,_)=>page.Dispatcher.Dispatch(()=>{if(!closing&&!Navigation.ModalStack.Contains(page))completion.TrySetResult(null);});
        var presets=new ActionRow();
        foreach(var (label,factor) in new[]{("Low offer",.85),("Agent demand",1d),("Strong offer",1.15)})
        {var pick=new Button{Text=label};pick.Clicked+=(_,_)=>{years.Value=demand.Years;salary.Value=Math.Clamp(ProOfferTerms.RoundUp((long)(demand.Salary*factor)),(long)salary.Minimum,(long)salary.Maximum);bonus.Value=Math.Min(ProOfferTerms.RoundUp((long)(demand.Bonus*factor)),(long)salary.Value*(int)years.Value);Refresh();};presets.Add(pick);}
        var current=state!.Contracts.LastOrDefault(c=>c.Active&&c.PlayerId==player.Football.PlayerId);
        var currentSummary=current is null?"Current deal: free agent":$"Current deal: {Money(ProContractInsights.AverageCash(current))}/year · through {ProContractInsights.PlayingEnd(current)} · {state.Year} cap {Money(ProEconomy.CapHit(current,state.Year))}";
        var offer=new VerticalStackLayout {Spacing=10,Children={Text(currentSummary,13,true),Text($"Agent asks {Money(demand.Salary)}/year + {Money(demand.Bonus)} signing bonus · {demand.Years} years",13,true),presets,Control("Years",years,false),Control("Annual base salary · $250K steps",salary,true),Control("Signing bonus · $250K steps",bonus,true),preview,response,submit,counter,details,annualPreview}};

        var tab=new Picker {Title="Contract view",ItemsSource=new[]{"Offer","Current deal","Position market"},SelectedIndex=0,AutomationId="contract-view"};
        var body=new ContentView {Content=offer};tab.SelectedIndexChanged+=(_,_)=>body.Content=tab.SelectedIndex switch {1=>ContractDetails(player,state!.Contracts.LastOrDefault(c=>c.Active&&c.PlayerId==player.Football.PlayerId)),2=>ContractComparisons(player),_=>offer};
        var stack=new VerticalStackLayout {Padding=16,Spacing=10,Children={Text("Contract negotiation",25,true),new HorizontalStackLayout {Spacing=10,Children={Portrait(player,48),Text(player.Football.FullName,20,true)}},Text(extension?"Extension · one completed contract change per season":"Free agent · offers compete when the week advances",12),tab,body,cancel}};
        page.Content=new ScrollView {Content=stack};Refresh();await Navigation.PushModalAsync(page);return await completion.Task;
    }
    async Task RestructureContract(ProPlayer p)
    {
        var c=state!.Contracts.LastOrDefault(c=>c.Active&&c.PlayerId==p.Football.PlayerId&&c.Team==state.UserTeam);
        if(c is null)return;
        var changeYear=ProFinance.ContractChangeYear(state!);
        var salary=c.Years.FirstOrDefault(y=>y.Year==changeYear)?.Salary??0;
        var minimum=ProFinance.Minimum(state!,p,changeYear,practice:false);
        var maximum=(salary-minimum)/ProOfferTerms.MoneyStep*ProOfferTerms.MoneyStep;
        if(maximum<ProOfferTerms.MoneyStep)throw new InvalidOperationException("No salary can be converted while retaining the league minimum.");
        var completion=new TaskCompletionSource<long?>(TaskCreationOptions.RunContinuationsAsynchronously);var closing=false;
        var page=new ContentPage {Title="Restructure contract",BackgroundColor=DynastyTheme.Background};
        var step=new Stepper {Minimum=0,Maximum=maximum,Increment=ProOfferTerms.MoneyStep,Value=Math.Max(ProOfferTerms.MoneyStep,maximum/2/ProOfferTerms.MoneyStep*ProOfferTerms.MoneyStep),AutomationId="restructure-amount"};
        var amountLabel=Text(Money((long)step.Value),22,true);step.ValueChanged+=(_,e)=>amountLabel.Text=Money((long)e.NewValue);
        var convert=new Button {Text="Convert salary to bonus"};var cancel=new Button {Text="Cancel"};
        async Task Close(long? amount){if(closing)return;closing=true;await Navigation.PopModalAsync();completion.TrySetResult(amount);}
        convert.Clicked+=async(_,_)=>await Close((long)step.Value);cancel.Clicked+=async(_,_)=>await Close(null);
        page.Disappearing+=(_,_)=>page.Dispatcher.Dispatch(()=>{if(!closing&&!Navigation.ModalStack.Contains(page))completion.TrySetResult(null);});
        page.Content=new VerticalStackLayout {Padding=16,Spacing=12,Children={Text("Restructure contract",25,true),Text(p.Football.FullName,20,true),Text($"Convert base salary to guaranteed bonus in $250K steps. Current salary {Money(salary)}; retain at least {Money(minimum)}. Proration moves cap charges into remaining years (maximum five).",12),amountLabel,step,convert,cancel}};
        await Navigation.PushModalAsync(page);var amount=await completion.Task;if(amount is null||amount==0)return;
        await Manage("Contract restructure",p.Football.PlayerId,0,x=>ProNegotiation.Restructure(x,x.UserTeam,p.Football.PlayerId,amount.Value));
    }
}
