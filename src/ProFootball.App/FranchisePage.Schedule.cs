using ProFootball.Core;
using DynastyManager.App;
namespace ProFootball.App;
public sealed partial class FranchisePage
{
    string scheduleTeam = "Your team"; int? scheduleWeek; ProPhase? schedulePhase;ProPhase? scheduleWeekPhase;
    Task OpenLeagueGames() { scheduleTeam="All teams"; scheduleWeek=state!.Week; schedulePhase=state.Phase;scheduleWeekPhase=state.Phase; return Open("Schedule"); }
    Task OpenTeamSchedule() { scheduleTeam="Your team"; scheduleWeek=null; schedulePhase=null;scheduleWeekPhase=null; return Open("Schedule"); }
    void Schedule()
    {
        var s=state!;if(schedulePhase==ProPhase.Offseason){schedulePhase=null;scheduleWeek=null;scheduleWeekPhase=null;} body.Add(Text("Season schedule",23,true));
        var teams=new[] { "Your team","All teams" }.Concat(s.League.Teams.Select(t=>t.Name)).ToArray();
        var team=new Picker { Title="Team",ItemsSource=teams,SelectedIndex=Math.Max(0,Array.IndexOf(teams,scheduleTeam)) };
        var phases=new[] { "All phases" }.Concat(Enum.GetNames<ProPhase>().Where(p=>p!="Offseason")).ToArray();
        var phase=new Picker { Title="Phase",ItemsSource=phases,SelectedIndex=schedulePhase is null ? 0 : Array.IndexOf(phases,schedulePhase.ToString()) };
        IReadOnlyList<ProScheduleWeek> weeks=[];var syncingWeeks=false;
        var week=new Picker {Title="Week",AutomationId="pro-schedule-week"};phase.AutomationId="pro-schedule-phase";
        void UpdateWeeks(){syncingWeeks=true;try{weeks=ProScheduleFilters.Weeks(s.Phase,s.Week,schedulePhase);week.ItemsSource=weeks.Select(w=>w.Label).ToArray();var selected=weeks.ToList().FindIndex(w=>w.Week==scheduleWeek&&w.Phase==(scheduleWeek.HasValue?(scheduleWeekPhase??schedulePhase):null));week.SelectedIndex=Math.Max(0,selected);if(selected<0){scheduleWeek=null;scheduleWeekPhase=null;}}finally{syncingWeeks=false;}}
        UpdateWeeks();
        var filters=new ActionRow(); filters.Add(team); filters.Add(phase); filters.Add(week); body.Add(filters);
        var list=new VerticalStackLayout(); body.Add(list);
        void Draw()
        {
            list.Clear();var selected=scheduleTeam=="Your team"?s.UserTeam:scheduleTeam;
            var games=s.Games.Where(g=>(selected=="All teams"||g.Football.InvolvesTeam(selected))&&(schedulePhase is null||g.Phase==schedulePhase)&&(!scheduleWeek.HasValue||(g.Football.Week==scheduleWeek&&g.Phase==(scheduleWeekPhase??schedulePhase)))).OrderBy(g=>g.Phase).ThenBy(g=>g.Football.Week).ToArray();
            list.Add(Text(games.Length+" games · "+(selected=="All teams"?"League":selected),12));
            var own=selected!="All teams";
            var columns=own?new (string,int)[] { ("Week",55),("Phase",95),("Opponent",200),("Venue",65),("Result",100),("Game center",230) }:new (string,int)[] { ("Week",55),("Phase",95),("Away",190),("Home",190),("Result",100),("Game center",230) };
            list.Add(SectionCard(null,null,Table(columns,games.Select(g=>
            {
                var f=g.Football;var actions=new ActionRow();
                if(f.HasPlayed) actions.Add(Link("Box score",()=>BoxScore(f)));
                else if(g.Phase==s.Phase&&f.Week==s.Week) { actions.Add(Action("Watch",()=>Live(g,DynastyManager.Core.Models.GameDayMode.Watch))); actions.Add(Action("Play",()=>Live(g,DynastyManager.Core.Models.GameDayMode.Play))); }
                else actions.Add(Text("Upcoming",12));
                var home=s.League.Teams.Single(t=>t.Name==f.HomeTeamName);var away=s.League.Teams.Single(t=>t.Name==f.AwayTeamName);
                var result=f.HasPlayed?$"{f.AwayScore}–{f.HomeScore}":"Upcoming";
                return new View[] { Text(f.Week.ToString()+(g.Phase==s.Phase&&f.Week==s.Week?" •":"")),Text(g.Phase.ToString(),12),own?TeamCell(f.HomeTeamName==selected?away:home):TeamCell(away),own?Text(f.HomeTeamName==selected?"Home":"Away"):TeamCell(home),Text(result),actions };
            }))));
        }
        team.SelectedIndexChanged+=(_,_)=>{scheduleTeam=(string)team.SelectedItem;Draw();};phase.SelectedIndexChanged+=(_,_)=>{schedulePhase=phase.SelectedIndex==0?null:Enum.Parse<ProPhase>((string)phase.SelectedItem);scheduleWeek=null;scheduleWeekPhase=null;UpdateWeeks();Draw();};week.SelectedIndexChanged+=(_,_)=>{if(syncingWeeks||week.SelectedIndex<0)return;var choice=weeks[week.SelectedIndex];scheduleWeek=choice.Week;scheduleWeekPhase=choice.Phase;Draw();};Draw();
    }
}