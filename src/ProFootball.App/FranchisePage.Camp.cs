using DynastyManager.App;
using DynastyManager.Core.Simulation;
using DynastyManager.Core.Models;
using ProFootball.Core;
namespace ProFootball.App;
public sealed partial class FranchisePage
{
    string campPosition="All positions";
    Task Camp()
    {
        var s=state!;body.Clear();body.Add(Action("‹ Front Office",()=>Open("Front Office")));body.Add(Text("Training camp",25,true));
        body.Add(Text($"Active {ProRoster.Active(s,s.UserTeam)}/90 · Reserve {s.Players.Count(p=>p.Football.TeamName==s.UserTeam&&p.Reserve)} · Season cap room {Money(ProCapDisplay.Room(s,s.UserTeam))}",14,true));
        if(s.UnsignedRookies.Any(r=>r.Team==s.UserTeam))body.Add(Action("Sign rookie contracts",async()=>{state=ProDraft.SignRookies(state!,state!.UserTeam);await Save();await Camp();}));
        body.Add(Text("Set starters and review position battles. Final cuts to 53 and cap compliance are required before regular-season play.",12));
        var tools=new ActionRow();tools.Add(Action("Organize depth chart",DepthChart));tools.Add(Action("Roster",()=>Open("Roster")));tools.Add(Action("Free agents",FreeAgents));tools.Add(Action("Player development",()=>{analyticsSection=3;return Open("Analytics");}));body.Add(tools);
        var choices=new[]{"All positions"}.Concat(PositionLabels.Names).ToArray();var position=new Picker{AutomationId="pro-camp-position",ItemsSource=choices,SelectedIndex=Array.IndexOf(choices,campPosition)};body.Add(CompactFilters.Create((position,160)));var rows=new ContentView();body.Add(rows);
        void Draw()
        {
            var players=s.Players.Where(p=>p.Football.TeamName==s.UserTeam&&ProDepthChart.Active(p)&&ProCareer.Controls(s,p.Football.Position)&&(position.SelectedIndex==0||p.Football.Position.DisplayName()==choices[position.SelectedIndex])).OrderBy(p=>ProDepthChart.AssignedCode(s,p)).ThenBy(p=>p.Football.DepthChartOrder).ToArray();
            rows.Content=Table([("Pos",45),("Depth",48),("Player",170),("OVR",45),("Δ",45),("Growth",75),("Status",130),("Action",105)],players.Select(p=>{
                var code=ProDepthChart.AssignedCode(s,p);var depth=ProDepthChart.AtPosition(s,s.UserTeam,code).ToList().FindIndex(x=>x.Football.PlayerId==p.Football.PlayerId)+1;
                var available=ProReserve.MedicallyAvailable(s,p);var starter=Action(depth==1?"Starter":"Make starter",async()=>{await Update(x=>{ProCareer.RequirePlayer(x,p.Football.PlayerId);return ProDepthChart.MakeStarter(x,x.UserTeam,p.Football.PlayerId);});await Camp();});starter.IsEnabled=depth>1&&available;starter.FontSize=11;starter.MinimumHeightRequest=44;starter.BackgroundColor=DynastyTheme.Accent;starter.TextColor=DynastyTheme.IsLight?Colors.White:Color.FromArgb("#0B1726");
                return new View[]{Text(ProDepthChart.Label(code)),Text(depth.ToString()),Link(p.Football.FullName,()=>Player(p)),Text(p.Football.OverallRating.ToString()),RatingDelta(p),Text(ProDevelopment.Headroom(p)+" OVR"),Text(available?"Available":ProReserve.Availability(s,p),11),starter};
            }));
        }
        position.SelectedIndexChanged+=(_,_)=>{if(position.SelectedIndex>=0)campPosition=choices[position.SelectedIndex];Draw();};Draw();OffseasonGuide();return Task.CompletedTask;
    }
}
