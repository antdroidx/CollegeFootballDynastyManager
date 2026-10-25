using DynastyManager.App;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Microsoft.Maui.Controls.Shapes;
using ProFootball.Core;
namespace ProFootball.App;
// Thin host over the shared simulation, renderer, animation planner and sprite artwork.
public sealed class ProLivePage : ContentPage
{
    readonly GameDaySession session;readonly ProLeague league;readonly Func<Task> save;
    readonly Label score=new(){FontSize=21,FontAttributes=FontAttributes.Bold,HorizontalTextAlignment=TextAlignment.Center};
    readonly Label situation=new(){HorizontalTextAlignment=TextAlignment.Center};readonly Label commentary=new(){FontSize=14};
    readonly AbsoluteLayout field=new(){HeightRequest=260,BackgroundColor=Colors.DarkGreen};
    readonly GameFieldDrawable art;readonly GraphicsView turf;readonly Button next=new(){Text="Next snap"};readonly Button pause=new(){Text="Pause watch"};
    bool active,working,paused,saved,second;double speed=1;string? offense,defense;GameSnapEvent? last;
    readonly Label selected=new(){Text="CPU recommended calls"};
    public ProLivePage(GameDaySession session,ProLeague league,Func<Task> save)
    {
        this.session=session;this.league=league;this.save=save;BackgroundColor=DynastyTheme.Background;
        var home=league.Teams.Single(t=>t.Name==session.Game.HomeTeamName);
        art=new(){HomeName=home.Abbreviation,HomeInitial=home.Abbreviation,HomeColor=Color.FromArgb(home.Primary),HomeSecondaryColor=Color.FromArgb(home.Secondary),HashMarkFraction=session.Rules.HashMarkFraction,Weather=session.Weather};
        turf=new(){Drawable=art,InputTransparent=true};AbsoluteLayout.SetLayoutFlags(turf,Microsoft.Maui.Layouts.AbsoluteLayoutFlags.All);AbsoluteLayout.SetLayoutBounds(turf,new Rect(0,0,1,1));field.Add(turf);
        var body=new VerticalStackLayout{Padding=12,Spacing=10};body.Add(score);body.Add(situation);
        body.Add(new Border{Padding=5,Stroke=DynastyTheme.Accent,StrokeThickness=1,StrokeShape=new RoundRectangle{CornerRadius=12},BackgroundColor=DynastyTheme.Surface,Content=field});
        body.Add(selected);var controls=new ActionRow();controls.Add(next);controls.Add(pause);
        Button Button(string text,Func<Task> action){var b=new Button{Text=text};b.Clicked+=async(_,_)=>{if(working)return;working=true;try{await action();}finally{working=false;}};controls.Add(b);return b;}
        next.Clicked+=async(_,_)=>await Snap();pause.Clicked+=(_,_)=>{paused=!paused;pause.Text=paused?"Resume watch":"Pause watch";};
        Button("Choose offense",async()=>{var plays=PlaybookCatalog.OffensivePlays.ToArray();var pick=await GameDialogs.ActionSheetAsync(this,"Offensive play","Cancel",null,plays.Select(p=>p.Name+" · "+p.Formation).Append("Punt").Append("Field goal").ToArray());if(pick=="Punt")offense="special-punt";else if(pick=="Field goal")offense="special-field-goal";else offense=plays.FirstOrDefault(p=>p.Name+" · "+p.Formation==pick)?.Id;selected.Text="Offense: "+(pick??"CPU");});
        Button("Choose defense",async()=>{var plays=PlaybookCatalog.DefensivePlays.ToArray();var pick=await GameDialogs.ActionSheetAsync(this,"Defensive play","Cancel",null,plays.Select(p=>p.Name).ToArray());defense=plays.FirstOrDefault(p=>p.Name==pick)?.Id;selected.Text="Defense: "+(pick??"CPU");});
        Button("Instant replay",async()=>{if(last is not null)await Animate(last,true);});
        Button("Speed",async()=>{var pick=await GameDialogs.ActionSheetAsync(this,"Game and replay speed","Cancel",null,"0.5x","1x","2x");speed=pick=="0.5x"?.5:pick=="2x"?2:1;});
        Button("Timeout",()=>{GameDayEngine.TryCallTimeout(session,session.ControlledTeamName!);Update();return Task.CompletedTask;});
        Button("Second team",()=>{second=!second;GameDayCoachingService.UseSecondUnit(session,session.ControlledTeamName!,second);selected.Text=second?"Second team on field":"Starters on field";return Task.CompletedTask;});
        Button("Box score",async()=>{await GameDialogs.AlertAsync(this,"Live box score",$"{session.Game.AwayTeamName} {session.AwayScore} · {session.Game.HomeTeamName} {session.HomeScore} · {session.Events.Count} events","Close");});
        Button("Finish game",async()=>{paused=true;await Task.Run(()=>GameDayEngine.SimulateToEnd(session));await Commit();Update();});
        Button("Return to franchise",async()=>{if(!session.IsComplete&&!await GameDialogs.AlertAsync(this,"Leave game?","This unfinished game's snaps will be discarded. Your saved franchise remains unchanged.","Leave","Cancel"))return;active=false;await Navigation.PopModalAsync();});
        body.Add(controls);body.Add(commentary);Content=new ScrollView{Content=body};
        SizeChanged+=(_,_)=>field.HeightRequest=Math.Clamp((Width-36)/2.25,180,420);
        Loaded+=(_,_)=>{active=true;Update();if(session.Mode==GameDayMode.Watch)_=Watch();};
    }
    protected override bool OnBackButtonPressed(){if(working)return true;_=Close();return true;}
    async Task Close(){if(session.IsComplete||await GameDialogs.AlertAsync(this,"Leave game?","Unfinished snaps will be discarded.","Leave","Cancel")){active=false;await Navigation.PopModalAsync();}}
    async Task Watch(){while(active&&!session.IsComplete){if(!working&&!paused)await Snap();await Task.Delay(300);} }
    async Task Snap()
    {
        if(working||session.IsComplete)return;working=true;next.IsEnabled=false;
        try{var own=session.PossessionTeamName==session.ControlledTeamName;last=GameDayEngine.Step(session,session.Mode==GameDayMode.Play&&own?offense:null,session.Mode==GameDayMode.Play&&!own?defense:null);await Animate(last,false);Update();if(session.IsComplete)await Commit();}
        catch(Exception e){paused=true;await GameDialogs.AlertAsync(this,"Game paused",e.Message,"OK");}
        finally{working=false;next.IsEnabled=!session.IsComplete;}
    }
    async Task Commit(){if(saved||!session.IsComplete)return;await save();saved=true;}
    void Update(){string Short(string team)=>league.Teams.Single(t=>t.Name==team).Abbreviation;score.Text=$"{Short(session.Game.AwayTeamName)}  {session.AwayScore}  –  {session.HomeScore}  {Short(session.Game.HomeTeamName)}";situation.Text=session.IsComplete?"FINAL · Saved":$"Q{session.Quarter} {session.ClockSeconds/60}:{session.ClockSeconds%60:00} · {session.Down} & {session.Distance} · {GameDayEngine.BallOn(session,Short)}";art.YardLine=session.YardLine;art.Distance=session.Distance;turf.Invalidate();pause.IsVisible=session.Mode==GameDayMode.Watch;}
    async Task Animate(GameSnapEvent snap,bool replay)
    {
        foreach(var view in field.Children.Where(v=>v!=turf).ToArray())field.Remove(view);
        commentary.Text=(replay?"REPLAY · ":"")+(snap.Description??$"{snap.Outcome} · {snap.Yards} yards");
        art.YardLine=snap.StartYardLine;art.Distance=snap.Distance;turf.Invalidate();
        var movements=new List<(FootballSpriteView View,GameFieldPoint From,GameFieldPoint To,bool Carrier)>();
        var roles=new Dictionary<(bool,Position),int>();var contact=GameDayAnimationPlanner.ContactPoint(snap,1,snap.CarrierLane??.5);
        foreach(var id in snap.OffensePersonnel.Concat(snap.DefensePersonnel).Distinct())
        {
            if(!session.PlayersById.TryGetValue(id,out var p))continue;var attacking=snap.OffensePersonnel.Contains(id);var key=(attacking,p.Position);var role=roles.GetValueOrDefault(key);roles[key]=role+1;
            var lane=attacking?GameDayAnimationPlanner.AlignmentLane(p.Position,role):.15+(movements.Count%11)*.065;
            var offset=attacking?(p.Position==Position.QB?-5:p.Position==Position.RB?-7:-.5):2+(role%2)*3;
            var from=new GameFieldPoint(GameDayAnimationPlanner.ProjectFieldYardLine(snap.StartYardLine+offset,1),lane);
            var carrier=id==snap.BallCarrierId||id==snap.TargetId;var to=carrier?contact:attacking?new GameFieldPoint(GameDayAnimationPlanner.ProjectFieldYardLine(snap.StartYardLine+Math.Clamp(snap.Yards*.55,0,15)+offset,1),lane):GameDayAnimationPlanner.PursuitTarget(contact,role,id==snap.PrimaryDefenderId);
            var team=league.Teams.Single(t=>t.Name==p.TeamName);var sprite=new FootballSpriteView(Color.FromArgb(team.Primary),Color.FromArgb(team.Secondary),p.TeamName==session.Game.AwayTeamName,id.ToByteArray()[0]%4,p.JerseyNumber?.ToString()??p.Position.ToString());sprite.Highlight(carrier);
            field.Add(sprite);movements.Add((sprite,from,to,carrier));
        }
        var frames=30;for(var frame=0;frame<=frames;frame++)
        {if(!active)break;var progress=frame/(double)frames;foreach(var m in movements){var point=m.Carrier?GameDayAnimationPlanner.CarrierPoint(m.From,m.To,progress,75,snap.BrokeTackle):new GameFieldPoint(m.From.X+(m.To.X-m.From.X)*progress,m.From.Y+(m.To.Y-m.From.Y)*progress);AbsoluteLayout.SetLayoutBounds(m.View,new Rect(Math.Clamp(point.X*field.Width-14,0,Math.Max(0,field.Width-28)),Math.Clamp(point.Y*field.Height-14,0,Math.Max(0,field.Height-28)),28,28));m.View.Animate(progress,1,m.To.X-m.From.X,m.To.Y-m.From.Y,false,false);}await Task.Delay((int)(33/speed));}
    }
}
