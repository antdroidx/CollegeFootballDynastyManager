using DynastyManager.App;
using ProFootball.Core;
namespace ProFootball.App;
public sealed partial class FranchisePage
{
    async Task SimulatePhase()
    {
        var phase=state!.Phase;var year=state.Year;
        if(phase==ProPhase.Offseason)return;
        var label=phase==ProPhase.Preseason?"preseason":phase==ProPhase.RegularSeason?"regular season":"playoffs";
        if(!await GameDialogs.AlertAsync(this,"Simulate "+label+"?","Simulate remaining games and normal weekly decisions. Progress saves each week and stops at the next phase or an action you must resolve.","Simulate","Cancel"))return;
        var cancelled=false;var status=Text("Preparing simulation…",18,true);var stop=new Button{Text="Stop after this week"};stop.Clicked+=(_,_)=>{cancelled=true;stop.IsEnabled=false;status.Text="Stopping after this week…";};
        var page=new ContentPage{Title="Simulating "+label,BackgroundColor=DynastyTheme.Background,Content=new VerticalStackLayout{Padding=24,Spacing=16,Children={status,new ActivityIndicator{IsRunning=true,Color=DynastyTheme.Accent},stop}}};
        page.Disappearing+=(_,_)=>cancelled=true;
        await Navigation.PushModalAsync(page);
        try
        {
            for(var i=0;i<32&&!cancelled&&state!.Year==year&&state.Phase==phase;i++)
            {
                status.Text=$"Simulating {label} · Week {state.Week}";await Task.Yield();
                var before=state;state=await Task.Run(()=>ProLeagueService.Advance(before));await Save();
                if(state.Phase==before.Phase&&state.Week==before.Week)
                {
                    if(ProSeasonEligibility.Issue(state,state.UserTeam) is {} issue)await GameDialogs.AlertAsync(page,"Action needed",issue,"OK");
                    break;
                }
            }
        }
        finally{if(Navigation.ModalStack.Contains(page))await Navigation.PopModalAsync();Render();}
    }
}
