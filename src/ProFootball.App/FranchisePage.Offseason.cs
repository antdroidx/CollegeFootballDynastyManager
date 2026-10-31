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
        var guide=new VerticalStackLayout{AutomationId="pro-offseason-guide",Spacing=6,Children={Text($"{s.Year} offseason · {title}",22,true),StageTimeline(s.OffseasonStep)}};
        var nav=new ActionRow();nav.Add(Action("Overview",()=>Open("Overview")));nav.Add(Action("Front Office",()=>Open("Front Office")));nav.Add(Action("Save",Save));nav.Add(Action("Settings",Options));guide.Add(nav);body.Insert(0,guide);
        if(s.OffseasonStep==OffseasonStep.Draft&&ProRookieOptions.Pending(s,s.UserTeam).Any())body.Add(Action("Rookie option decisions",RookieOptions));
        var issue=ProOffseasonFlow.Blocker(s);if(issue is not null)body.Add(Text(issue,13,true));
        var next=Action(ProOffseasonFlow.Continue(s.OffseasonStep),Advance,"pro-offseason-next");next.IsEnabled=issue is null;next.BackgroundColor=DynastyTheme.Accent;next.TextColor=DynastyTheme.IsLight?Colors.White:Color.FromArgb("#0B1726");body.Add(next);
    }
    static View StageTimeline(OffseasonStep step)
    {
        var steps=new[]{("Retention",OffseasonStep.Retention),("Free agency",OffseasonStep.FreeAgency),("Draft",OffseasonStep.Draft),("Roster cuts",OffseasonStep.RosterCuts)};
        var row=new HorizontalStackLayout{Spacing=0};
        for(var i=0;i<steps.Length;i++)
        {
            var (name,stage)=steps[i];var isDone=(int)step>(int)stage;var isNow=step==stage;
            var chip=new Border{Padding=new Thickness(8,3),StrokeShape=new Microsoft.Maui.Controls.Shapes.RoundRectangle{CornerRadius=10},BackgroundColor=isNow?DynastyTheme.Accent:(isDone?(DynastyTheme.IsLight?Color.FromArgb("#BFD9D5"):Color.FromArgb("#294B48")):Colors.Transparent)};
            chip.Content=new Label{Text=name,FontSize=11,FontAttributes=isNow?FontAttributes.Bold:FontAttributes.None,TextColor=isNow?(DynastyTheme.IsLight?Colors.White:Color.FromArgb("#0B1726")):DynastyTheme.Text};
            row.Children.Add(chip);
            if(i<steps.Length-1)row.Children.Add(new Label{Text="›",FontSize=11,TextColor=DynastyTheme.Muted,VerticalTextAlignment=TextAlignment.Center,Margin=new Thickness(4,0)});
        }
        return row;
    }
    View FreeAgencyAssistanceControls()
    {
        var toggle=new Switch{IsToggled=state!.AutoManageFreeAgency,AutomationId="pro-auto-free-agency"};toggle.Toggled+=async(_,e)=>{state=state! with{AutoManageFreeAgency=e.Value};await Save();};
        var ask=Action("Ask GM to propose offers",async()=>{state=ProFreeAgencyAssistance.Propose(state!);await Save();await FreeAgents();},"pro-assist-free-agency");
        return new VerticalStackLayout{Spacing=6,Children={new HorizontalStackLayout{Spacing=8,Children={toggle,Text("GM assistance · propose offers on advancement",12)}},Text("Targets roster needs within your cap and coaching authority. Does not cut or trade your players. Offers compete when you advance.",12),ask}};
    }
}



