using DynastyManager.App;
using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
using Microsoft.Maui.Controls.Shapes;
using ProFootball.Core;
namespace ProFootball.App;
public sealed class FranchisePage : ContentPage
{
    readonly ProSaveStore store=new(System.IO.Path.Combine(FileSystem.AppDataDirectory,"pro-franchises-v1"));
    readonly VerticalStackLayout body=new(){Padding=16,Spacing=12};
    ProFranchise? state; bool busy; string section="Overview";
    public FranchisePage(){Title="Pro Football Franchise Manager";BackgroundColor=DynastyTheme.Background;Content=new ScrollView{Content=body};Loaded+=async(_,_)=>await Library();}
    static Label Text(string text,int size=14,bool bold=false)=>new(){Text=text,FontSize=size,FontAttributes=bold?FontAttributes.Bold:FontAttributes.None,TextColor=DynastyTheme.Text};
    Button Action(string text,Func<Task> action,string? id=null)
    {var b=new Button{Text=text,AutomationId=id,BackgroundColor=DynastyTheme.Panel,TextColor=DynastyTheme.Text};b.Clicked+=async(_,_)=>{if(busy)return;busy=true;b.IsEnabled=false;try{await action();}catch(Exception e){await GameDialogs.AlertAsync(this,"Unable to continue",e.Message,"OK");}finally{busy=false;b.IsEnabled=true;}};return b;}
    async Task Library()
    {
        state=null;body.Clear();body.Add(Text("PRO FOOTBALL",28,true));body.Add(Text("Franchise Manager",24,true));body.Add(Text("Foundation preview · "+AppInfo.VersionString));
        body.Add(Text("Build your franchise across a fictional 32-team professional league."));
        body.Add(Action("New franchise",Create,"pro-new"));
        body.Add(Action("Appearance",async()=>{var choice=await GameDialogs.ActionSheetAsync(this,"Appearance","Cancel",null,"Dark","Light");if(choice is "Dark" or "Light"){Preferences.Set("app-theme",choice);Application.Current!.Resources=DynastyTheme.CreateResources();Application.Current.UserAppTheme=DynastyTheme.RequestedTheme;BackgroundColor=DynastyTheme.Background;await Library();}}));
        body.Add(Text("Your franchises",19,true));
        foreach(var id in store.Slots().ToArray())
        {try{var save=await store.Load(id);body.Add(Action($"{save.Name} · {save.UserTeam} · {save.Year} {save.Phase} W{save.Week}",async()=>{state=await store.Load(id);section="Overview";Render();}));}catch(Exception){body.Add(Text("Unavailable save · "+id));}}
        body.Add(Text("First playable foundation. Contracts, salary cap, trades, draft and advanced roster transactions are planned for later development.",12));
    }
    Task Create()
    {
        body.Clear();body.Add(Text("Start a franchise",24,true));var name=new Entry{Placeholder="Franchise name",Text="My franchise",AutomationId="pro-name"};body.Add(name);
        var league=ProLeagueService.Load();var picker=new Picker{Title="Choose your team",ItemsSource=league.Teams.Select(t=>t.Name).ToList(),SelectedIndex=0,AutomationId="pro-team"};body.Add(picker);
        body.Add(Text("32 fictional teams · 53 active players · 16 practice players · 17 regular-season games"));
        body.Add(Action("Start franchise",async()=>{state=await Task.Run(()=>ProLeagueService.Create((string)picker.SelectedItem,name.Text));await store.Save(state);Render();},"pro-start"));body.Add(Action("Cancel",Library));return Task.CompletedTask;
    }
    async Task Save(){if(state is not null)await store.Save(state);}
    void Render()
    {
        if(state is null)return;body.Clear();Title="Pro Football Franchise Manager";
        var team=state.League.Teams.Single(t=>t.Name==state.UserTeam);
        body.Add(new Border{Padding=16,StrokeThickness=0,StrokeShape=new RoundRectangle{CornerRadius=14},BackgroundColor=Color.FromArgb(team.Primary),Content=new VerticalStackLayout{Children={new Label{Text=team.Abbreviation+"  "+state.UserTeam,TextColor=Colors.White,FontSize=23,FontAttributes=FontAttributes.Bold},new Label{Text=$"{state.Year} · {state.Phase} · Week {state.Week}",TextColor=Colors.White}}}});
        var nav=new ActionRow();foreach(var tab in new[]{"Overview","Schedule","Standings","Roster","History"}){var target=tab;nav.Add(Action(tab,()=>{section=target;Render();return Task.CompletedTask;}));}body.Add(nav);
        if(section=="Overview") Overview();else if(section=="Schedule")Schedule();else if(section=="Standings")Standings();else if(section=="Roster")Roster();else History();
        var actions=new ActionRow();actions.Add(Action("Save franchise",async()=>{await Save();await GameDialogs.AlertAsync(this,"Saved","Your franchise and rolling backups are saved.","OK");}));actions.Add(Action("Save & exit",async()=>{await Save();await Library();}));body.Add(actions);
    }
    void Overview()
    {
        var s=state!;var standing=ProStandings.Build(s).Single(r=>r.Team.Name==s.UserTeam);body.Add(Text($"{standing.Wins}–{standing.Losses}–{standing.Ties} · {standing.Team.Conference} {standing.Team.Division}",20,true));
        if(s.Phase==ProPhase.Offseason){body.Add(Text("Champions · "+s.Champion,22,true));body.Add(Text("This preview ends after the championship. Offseason transactions and year-to-year player development arrive in later phases."));return;}
        var own=s.Games.FirstOrDefault(g=>g.Phase==s.Phase&&g.Football.Week==s.Week&&g.Football.InvolvesTeam(s.UserTeam));
        if(own is not null)body.Add(GameCard(own));else body.Add(Text("Your team has a bye this week."));
        body.Add(Action("This week's league games",()=>{section="Schedule";Render();return Task.CompletedTask;},"pro-week-games"));
        body.Add(Action("Advance week",async()=>{if(!await GameDialogs.AlertAsync(this,"Advance week?","Simulate all remaining games this week and save the results?","Advance","Cancel"))return;var next=await Task.Run(()=>ProLeagueService.Advance(state!));state=next;await Save();Render();},"pro-advance"));
    }
    View GameCard(ProGame game)
    {
        var g=game.Football;var stack=new VerticalStackLayout{Spacing=8};stack.Add(Text(g.AwayTeamName+" at "+g.HomeTeamName,17,true));
        if(g.HasPlayed){stack.Add(Text($"Final · {g.AwayScore}–{g.HomeScore}"));stack.Add(Action("Box score",()=>BoxScore(g)));}
        else if(game.Phase==state!.Phase&&g.Week==state.Week)
        {var row=new ActionRow();row.Add(Action("Watch",()=>Live(game,GameDayMode.Watch),"pro-watch"));row.Add(Action("Play",()=>Live(game,GameDayMode.Play),"pro-play"));row.Add(Action("Simulate",async()=>{var session=ProLeagueService.Start(state!,game,GameDayMode.Simulate);await Task.Run(()=>GameDayEngine.SimulateToEnd(session));state=ProLeagueService.Finish(state!,session);await Save();Render();},"pro-sim"));stack.Add(row);}
        else stack.Add(Text(game.Phase+" · Week "+g.Week));
        return new Border{Padding=12,StrokeThickness=0,BackgroundColor=DynastyTheme.Surface,StrokeShape=new RoundRectangle{CornerRadius=12},Content=stack};
    }
    void Schedule()
    {
        var filters=new ActionRow();foreach(var phase in new[]{ProPhase.Preseason,ProPhase.RegularSeason,ProPhase.Playoffs}){var target=phase;filters.Add(Action(phase.ToString(),()=>{RenderSchedule(target);return Task.CompletedTask;}));}body.Add(filters);RenderSchedule(state!.Phase);
    }
    readonly VerticalStackLayout schedule=new(){Spacing=10};
    void RenderSchedule(ProPhase phase)
    {
        if(schedule.Parent is null)body.Add(schedule);schedule.Clear();
        var week=new Picker{Title="Week",ItemsSource=state!.Games.Where(g=>g.Phase==phase).Select(g=>g.Football.Week).Distinct().Order().Select(w=>$"Week {w}"+(phase==state.Phase&&w==state.Week?" · Current":"")).ToList()};
        var own=new Switch{IsToggled=true};schedule.Add(new HorizontalStackLayout{Spacing=8,Children={own,Text("Your games only")}});schedule.Add(week);var list=new VerticalStackLayout{Spacing=8};schedule.Add(list);
        void Draw(){list.Clear();var selected=week.SelectedIndex+1;foreach(var game in state.Games.Where(g=>g.Phase==phase&&g.Football.Week==selected&&(!own.IsToggled||g.Football.InvolvesTeam(state.UserTeam))))list.Add(GameCard(game));if(list.Count==0)list.Add(Text("No games in this selection."));}
        week.SelectedIndexChanged+=(_,_)=>Draw();own.Toggled+=(_,_)=>Draw();week.SelectedIndex=phase==state.Phase?Math.Max(0,state.Week-1):0;Draw();
    }
    void Standings()
    {body.Add(Text("Standings",23,true));foreach(var group in ProStandings.Build(state!).GroupBy(r=>r.Team.Conference+" · "+r.Team.Division)){body.Add(Text(group.Key,17,true));foreach(var row in group)body.Add(Text($"{row.Team.Name}   {row.Wins}–{row.Losses}–{row.Ties}   {row.Percentage:P0}   Diff {row.Differential}"));}body.Add(Text("Foundation tiebreaks: percentage, division record, conference record, opponent strength, differential. Full official tiebreak chains are planned.",12));}
    void Roster()
    {body.Add(Text("Active roster · 53",22,true));foreach(var group in state!.Players.Where(p=>p.Football.TeamName==state.UserTeam).GroupBy(p=>p.PracticeSquad)){body.Add(Text(group.Key?"Practice squad · 16":"Active players",18,true));foreach(var p in group.OrderBy(p=>p.Football.Position).ThenBy(p=>p.Football.DepthChartOrder))body.Add(Action($"{p.Football.Position}  {p.Football.FullName} · {p.Football.OverallRating} OVR",()=>GameDialogs.AlertAsync(this,p.Football.FullName,$"{p.Football.Position} · Age {p.Age} · Experience {p.Experience}\nOVR {p.Football.OverallRating} · Potential {p.Football.PotentialRating}","Close")));}}
    void History(){body.Add(Text("Franchise history",22,true));foreach(var season in state!.History)body.Add(Text($"{season.Year} Champions · {season.Champion}"));if(state.History.Count==0)body.Add(Text("Season records appear after the championship."));}
    Task BoxScore(ScheduledGame game)=>GameDialogs.AlertAsync(this,"Box score",$"{game.AwayTeamName} {game.AwayScore} · {game.HomeTeamName} {game.HomeScore}\nTotal yards {game.AwayStats?.TotalYards} / {game.HomeStats?.TotalYards}\nTurnovers {game.AwayStats?.Turnovers} / {game.HomeStats?.Turnovers}","Close");
    async Task Live(ProGame game,GameDayMode mode)
    {
        var controlled=state!.UserTeam;
        if(mode==GameDayMode.Play&&!game.Football.InvolvesTeam(controlled)){controlled=await GameDialogs.ActionSheetAsync(this,"Choose team to control","Cancel",null,game.Football.AwayTeamName,game.Football.HomeTeamName);if(controlled is null or "Cancel")return;}
        var session=ProLeagueService.Start(state,game,mode,controlled);
        await Navigation.PushModalAsync(new ProLivePage(session,state.League,async()=>{state=ProLeagueService.Finish(state!,session);await Save();Render();}));
    }
}
