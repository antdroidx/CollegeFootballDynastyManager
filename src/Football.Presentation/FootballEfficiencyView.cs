using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
namespace DynastyManager.App;

public static class FootballEfficiencyView
{
    static Label Text(string s)=>new(){Text=s,TextColor=DynastyTheme.Text,FontSize=13};
    public static View Create(IEnumerable<ScheduledGame> source,string userTeam)
    {
        var rows=FootballEfficiency.Build(source);var body=new VerticalStackLayout{Spacing=10};
        body.Add(new Label{Text="Advanced efficiency",FontSize=21,FontAttributes=FontAttributes.Bold,TextColor=DynastyTheme.Text});
        if(rows.Count==0){body.Add(Text("Comparisons appear after regular-season games are recorded."));return body;}
        var team=new Picker{Title="Compare team",ItemsSource=rows.Select(x=>x.Team).ToArray(),SelectedIndex=Math.Max(0,rows.ToList().FindIndex(x=>x.Team==userTeam)),AutomationId="efficiency-team"};
        var metrics=new(string Name,Func<TeamEfficiency,double?> Value,bool Higher,string Format)[]{
            ("Adjusted net efficiency",x=>x.Net,true,"+0.0;-0.0;0.0"),("Adjusted offense",x=>x.Offense,true,"+0.0;-0.0;0.0"),("Adjusted defense",x=>x.Defense,true,"+0.0;-0.0;0.0"),
            ("Yards / play",x=>x.YardsPerPlay,true,"0.00"),("Explosive plays %",x=>x.ExplosiveRate,true,"0.0"),("Sack rate allowed %",x=>x.SackRate,false,"0.0"),("Turnovers / play %",x=>x.TurnoverRate,false,"0.0"),
            ("Success · recorded snaps %",x=>x.SuccessRate,true,"0.0"),("Third-down conversion %",x=>x.ThirdDownRate,true,"0.0")};
        var metric=new Picker{Title="Efficiency metric",ItemsSource=metrics.Select(m=>m.Name).ToArray(),SelectedIndex=0,AutomationId="efficiency-metric"};
        var content=new VerticalStackLayout{Spacing=10};
        void Draw()
        {
            content.Clear();if(team.SelectedIndex<0||metric.SelectedIndex<0)return;var own=rows[team.SelectedIndex];var m=metrics[metric.SelectedIndex];var available=rows.Where(x=>m.Value(x).HasValue).ToArray();
            content.Add(Text($"{own.Games} games · {own.Plays:N0} plays · {own.ContextPlays:N0} context snaps recorded"));
            if(m.Value(own) is not double value){content.Add(Text("Not recorded in these saved games. New games record full-play context."));return;}
            var ordered=available.OrderBy(x=>m.Higher?-m.Value(x)!.Value:m.Value(x)!.Value).ToArray();var leader=ordered[0];var avg=available.Average(x=>m.Value(x)!.Value);var rank=1+available.Count(x=>m.Higher?m.Value(x)>value:m.Value(x)<value);
            content.Add(Text($"{m.Name} · Rank {rank}/{available.Length} · "+(m.Higher?"Higher is better":"Lower is better")));
            content.Add(FootballBarChart.Create(new[]{(own.Team,value),("League average",avg),("Leader · "+leader.Team,m.Value(leader)!.Value)},m.Format,metric.SelectedIndex<3?"%":""));
            content.Add(FootballDataGrid.Create([("Team",180),("Value",90),("Games",55),("Plays",65)],ordered.Select(x=>new View[]{Text(x.Team),Text(m.Value(x)!.Value.ToString(m.Format)),Text(x.Games.ToString()),Text(x.Plays.ToString())})));
        }
        team.SelectedIndexChanged+=(_,_)=>Draw();metric.SelectedIndexChanged+=(_,_)=>Draw();body.Add(team);body.Add(metric);body.Add(content);Draw();
        var details=new Label{Text="Efficiency = yards + 20 per offensive TD − 45 per turnover, per play. Offense is above expected; defense is below allowed. Opponent expectations use their other games, shrunk toward the league by 200 plays. Net adds offense and defense. Success requires 40% of distance on first down, 60% on second, 100% on later downs; turnovers fail. Explosives gain 20+ yards. Context excludes kneels, spikes and special teams. Early-season ranks are provisional; context rates use only recorded snaps.",IsVisible=false,TextColor=DynastyTheme.Muted,FontSize=12};
        var method=new Button{Text="How these stats work",AutomationId="efficiency-method"};method.Clicked+=(_,_)=>{details.IsVisible=!details.IsVisible;method.Text=details.IsVisible?"Hide metric definitions":"How these stats work";};body.Add(method);body.Add(details);return body;
    }
}
