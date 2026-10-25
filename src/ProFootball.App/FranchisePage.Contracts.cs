using DynastyManager.App;
using ProFootball.Core;
namespace ProFootball.App;
public sealed partial class FranchisePage
{
    async Task<(int Years,long Salary,long Bonus)?> ContractTerms(ProPlayer player,bool extension=false)
    {
        var completion=new TaskCompletionSource<(int Years,long Salary,long Bonus)?>(TaskCreationOptions.RunContinuationsAsynchronously);var closing=false;
        var page=new ContentPage {Title="Contract negotiation",BackgroundColor=DynastyTheme.Background};
        var years=new Entry {Text="3",Keyboard=Keyboard.Numeric,AutomationId="contract-years"};
        var salary=new Entry {Text=Math.Max(1_000_000,ProEconomy.MarketSalary(player)).ToString(),Keyboard=Keyboard.Numeric,AutomationId="contract-salary"};
        var bonus=new Entry {Text="0",Keyboard=Keyboard.Numeric,AutomationId="contract-bonus"};
        var response=Text("",14,true);var preview=Text("",12);var submit=new Button {Text=extension?"Agree extension":"Submit offer",AutomationId="contract-submit"};
        var counter=new Button {Text="Use player's counter",AutomationId="contract-counter"};var cancel=new Button {Text="Cancel"};
        bool Terms(out int y,out long a,out long b) {y=0;a=b=0;return int.TryParse(years.Text,out y)&&long.TryParse(salary.Text,out a)&&long.TryParse(bonus.Text,out b)&&y is >=1 and <=5&&a is >=300_000 and <=500_000_000&&b>=0&&b<=a*y;}
        void Refresh()
        {
            submit.IsEnabled=Terms(out var y,out var a,out var b);
            if(!submit.IsEnabled){response.Text="Enter 1–5 years and dollar amounts. Salary must be at least $300,000; bonus cannot exceed total base salary.";return;}
            var result=ProNegotiation.Evaluate(player,y,a,b);response.Text=result.Message+$" Counter salary: ${result.CounterSalary:N0}/year.";
            preview.Text=$"Total base salary ${a*y:N0} + bonus ${b:N0} · Total cash ${a*y+b:N0}\n"+(extension?"Existing contract years remain. Signing bonus is prorated across up to five remaining years.":$"First-year cap estimate ${a+(b+y-1)/y:N0} · Team room ${ProEconomy.Room(state!,state!.UserTeam):N0}");
        }
        years.TextChanged+=(_,_)=>Refresh();salary.TextChanged+=(_,_)=>Refresh();bonus.TextChanged+=(_,_)=>Refresh();
        counter.Clicked+=(_,_)=>{if(Terms(out var y,out var a,out var b))salary.Text=ProNegotiation.Evaluate(player,y,a,b).CounterSalary.ToString();};
        cancel.Clicked+=async(_,_)=>{if(closing)return;closing=true;await Navigation.PopModalAsync();completion.TrySetResult(null);};
        submit.Clicked+=async(_,_)=>{if(closing)return;if(!Terms(out var y,out var a,out var b))return;closing=true;await Navigation.PopModalAsync();completion.TrySetResult((y,a,b));};
        page.Disappearing+=(_,_)=>page.Dispatcher.Dispatch(()=>{if(!closing&&!Navigation.ModalStack.Contains(page))completion.TrySetResult(null);});
        var stack=new VerticalStackLayout {Padding=16,Spacing=10,Children={Text("Contract negotiation",25,true),new HorizontalStackLayout {Spacing=10,Children={Portrait(player,48),Text(player.Football.FullName,20,true)}},Text(extension?"Extension · one completed contract change per season":"Free agent · offers compete when the week advances",12),Text("Years"),years,Text("Annual base salary · dollars"),salary,Text("Signing bonus · dollars"),bonus,preview,response,counter,submit,cancel}};
        page.Content=new ScrollView {Content=stack};Refresh();await Navigation.PushModalAsync(page);return await completion.Task;
    }
    async Task RestructureContract(ProPlayer p)
    {
        var c=state!.Contracts.LastOrDefault(c=>c.Active&&c.PlayerId==p.Football.PlayerId&&c.Team==state.UserTeam);
        if(c is null)return;
        var salary=c.Years.FirstOrDefault(y=>y.Year==state.Year)?.Salary??0;
        var value=await GameDialogs.PromptAsync(this,"Restructure contract",$"Convert current base salary to a guaranteed signing bonus, prorated over remaining years (maximum five). This moves cap charges to later seasons. Current salary ${salary:N0}; retain at least $1,000,000. Enter dollars:","Convert","Cancel",initialValue:"0",keyboard:Keyboard.Numeric);
        if(value is null)return;
        if(!long.TryParse(value,out var amount))throw new InvalidOperationException("Enter a whole-dollar amount.");
        await Manage("Contract restructure",p.Football.PlayerId,0,s=>ProNegotiation.Restructure(s,s.UserTeam,p.Football.PlayerId,amount));
    }
}
