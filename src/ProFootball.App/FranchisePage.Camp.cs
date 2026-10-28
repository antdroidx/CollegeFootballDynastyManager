using DynastyManager.App;
using DynastyManager.Core.Simulation;
using ProFootball.Core;
namespace ProFootball.App;
public sealed partial class FranchisePage
{
    Task Camp()
    {
        var s=state!;body.Clear();body.Add(Action("‹ Front Office",()=>Open("Front Office")));body.Add(Text("Training camp & roster battles",25,true));
        var players=s.Players.Where(p=>p.Football.TeamName==s.UserTeam&&!p.Retired).ToArray();
        body.Add(Text($"Active {ProRoster.Active(s,s.UserTeam)} · Practice squad {players.Count(p=>p.PracticeSquad)} · Reserve {players.Count(p=>p.Reserve)}",14,true));
        body.Add(Text("Camp rosters allow 90 before final preseason cuts to 53. Compare your depth, promote eligible players and set starters before opening day.",12));
        var tools=new ActionRow();tools.Add(Action("Manage roster",()=>Open("Roster")));tools.Add(Action("Player development",()=>{analyticsSection=3;return Open("Analytics");}));body.Add(tools);
        body.Add(Text("Position battles",20,true));
        foreach(var group in players.Where(p=>!p.PracticeSquad&&!p.Reserve&&ProCareer.Controls(s,p.Football.Position)).GroupBy(p=>OffensiveLineService.Label(p.Football)).OrderBy(g=>g.Key))
        {
            var ranked=group.OrderBy(p=>p.Football.DepthChartOrder).ThenByDescending(p=>p.Football.OverallRating).ToArray();var leader=ranked[0];
            body.Add(Panel(group.Key+" · "+ranked.Length+" players",Table([("Player / action",185),("OVR",45),("Change",55),("Upside",60),("Availability",130)],ranked.Take(3).Select(p=>{
                var starter=p.Football.PlayerId==leader.Football.PlayerId;var injured=p.Football.CurrentInjury is {WeeksRemaining:>0};
                var choose=Action(starter?"First in depth":"Make starter",async()=>{await Update(x=>{ProCareer.RequirePlayer(x,p.Football.PlayerId);return ProDepthChart.MakeStarter(x,x.UserTeam,p.Football.PlayerId);});await Camp();});choose.IsEnabled=!starter&&!injured;
                return new View[]{new VerticalStackLayout{Spacing=3,Children={Link(p.Football.FullName,()=>Player(p)),choose}},Text(p.Football.OverallRating.ToString()),Text(ProDevelopment.Delta(p)?.ToString("+0;-0;0")??"—"),Text(ProDevelopment.Headroom(p)+" OVR"),Text(injured?"Injured":"Available")};
            }))));
        }
        return Task.CompletedTask;
    }
}
