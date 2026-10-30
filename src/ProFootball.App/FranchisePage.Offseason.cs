using DynastyManager.App;
using ProFootball.Core;
namespace ProFootball.App;
public sealed partial class FranchisePage
{
    string? guidedOffseasonKey;
    bool openingOffseason;
    string OffseasonKey()=>state!.Id+"/"+state.Year+"/"+state.Phase+"/"+state.OffseasonStep;
    async Task OpenOffseasonStage()
    {
        if(openingOffseason||state?.Phase!=ProPhase.Offseason||section!="Offseason")return;
        openingOffseason=true;loading.IsVisible=true;
        try{switch(state.OffseasonStep){case OffseasonStep.Retention:await Retention();break;case OffseasonStep.FreeAgency:await FreeAgents();break;case OffseasonStep.Draft:await Draft();break;default:await Camp();break;}await scroll.ScrollToAsync(0,0,false);}
        catch(Exception e){body.Clear();body.Add(Text("Offseason stage could not open.",20,true));body.Add(Action("Overview",()=>Open("Overview")));body.Add(Action("Retry stage",OpenOffseasonStage));await GameDialogs.AlertAsync(this,"Unable to continue",e.Message,"OK");}
        finally{openingOffseason=false;loading.IsVisible=false;}
    }
    void OffseasonGuide()
    {
        if(state?.Phase!=ProPhase.Offseason||section!="Offseason"||body.Children.OfType<View>().Any(v=>v.AutomationId=="pro-offseason-guide"))return;
        if(body.Children.FirstOrDefault() is Button back&&back.Text=="‹ Front Office")body.RemoveAt(0);
        var s=state;var title=ProOffseasonFlow.Title(s.OffseasonStep);
        var guide=new VerticalStackLayout{AutomationId="pro-offseason-guide",Spacing=6,Children={Text($"{s.Year} offseason · {title}",22,true),Text("1 Retention  ›  2 Free agency  ›  3 Draft  ›  4 Training camp",12)}};
        var nav=new ActionRow();nav.Add(Action("Overview",()=>Open("Overview")));nav.Add(Action("Front Office",()=>Open("Front Office")));nav.Add(Action("Save",Save));nav.Add(Action("Settings",Options));guide.Add(nav);body.Insert(0,guide);
        if(s.OffseasonStep==OffseasonStep.Draft&&ProRookieOptions.Pending(s,s.UserTeam).Any())body.Add(Action("Rookie option decisions",RookieOptions));
        var issue=ProOffseasonFlow.Blocker(s);if(issue is not null)body.Add(Text(issue,13,true));
        var next=Action(ProOffseasonFlow.Continue(s.OffseasonStep),Advance,"pro-offseason-next");next.IsEnabled=issue is null;next.BackgroundColor=DynastyTheme.Accent;next.TextColor=DynastyTheme.IsLight?Colors.White:Color.FromArgb("#0B1726");body.Add(next);
    }
    View FreeAgencyAssistanceControls()
    {
        var toggle=new Switch{IsToggled=state!.AutoManageFreeAgency,AutomationId="pro-auto-free-agency"};toggle.Toggled+=async(_,e)=>{state=state! with{AutoManageFreeAgency=e.Value};await Save();};
        var ask=Action("Ask GM to propose offers",async()=>{state=ProFreeAgencyAssistance.Propose(state!);await Save();await FreeAgents();},"pro-assist-free-agency");
        return new VerticalStackLayout{Spacing=6,Children={new HorizontalStackLayout{Spacing=8,Children={toggle,Text("GM assistance · propose offers on advancement",12)}},Text("Targets roster needs within your cap and coaching authority. Does not cut or trade your players. Offers compete when you advance.",12),ask}};
    }
}
