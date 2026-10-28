using DynastyManager.App;
using ProFootball.Core;
namespace ProFootball.App;
public sealed partial class FranchisePage
{
    View PlayerAnalytics(ProPlayer player)
    {
        var s = state!; var id = player.Football.PlayerId;
        var games = s.History.SelectMany(h => h.Games).Concat(s.Games).Where(g => g.Phase == ProPhase.RegularSeason && g.Football.HasPlayed).DistinctBy(g => g.Football.GameId).Select(g => g.Football).ToArray();
        var current = games.Where(g => g.SeasonYear == s.Year).Select(g => (Game: g, Line: g.PlayerStats.FirstOrDefault(p => p.PlayerId == id))).Where(x => x.Line is not null).ToArray();
        var career = games.SelectMany(g => g.PlayerStats).Where(p => p.PlayerId == id).GroupBy(p => p.SeasonYear).OrderBy(g => g.Key);
        var metric = player.Football.Position == DynastyManager.Core.Models.Position.QB ? "Passing yards" : player.Football.Position is DynastyManager.Core.Models.Position.RB ? "Rushing yards" : player.Football.Position is DynastyManager.Core.Models.Position.WR or DynastyManager.Core.Models.Position.TE ? "Receiving yards" : "Tackles";
        double Value(DynastyManager.Core.Models.PlayerSeasonStatLine x) => metric == "Passing yards" ? x.PassYards : metric == "Rushing yards" ? x.RushYards : metric == "Receiving yards" ? x.ReceivingYards : x.Tackles;
        var peers = s.Players.Where(p => !p.Retired && p.Football.Position == player.Football.Position).ToArray();
        return Panel("Player analytics", Text(metric + " · game log", 15, true), FootballBarChart.Create(current.Select(x => ($"Week {x.Game.Week}", Value(x.Line!)))),
            Text(metric + " · career by season", 15, true), FootballBarChart.Create(career.Select(g => (g.Key.ToString(), g.Sum(Value)))),
            Text("Position comparison · overall", 15, true), FootballBarChart.Create(new[] { (player.Football.FullName, (double)player.Football.OverallRating), ("League position average", peers.Select(p => (double)p.Football.OverallRating).DefaultIfEmpty(0).Average()), ("League position best", peers.Select(p => (double)p.Football.OverallRating).DefaultIfEmpty(0).Max()) }, "0.0"));
    }
    int analyticsSection;
    void Analytics()
    {
        var s=state!;body.Add(Text("Franchise analytics",23,true));
        var sections=new[]{"Team performance","Roster quality","Cap & draft","Player development","Draft outcomes","Career records"};
        var picker=new Picker{Title="Analytics category",AutomationId="pro-analytics-category",ItemsSource=sections,SelectedIndex=analyticsSection};body.Add(picker);
        var content=new VerticalStackLayout{Spacing=10};body.Add(content);
        void Draw()
        {
            content.Clear();analyticsSection=Math.Max(0,picker.SelectedIndex);
            if(analyticsSection==3)
            {
                var players=s.Players.Where(p=>!p.Retired&&!p.PracticeSquad&&!p.Reserve&&ProDevelopment.Delta(p).HasValue).ToArray();
                var own=players.Where(p=>p.Football.TeamName==s.UserTeam).OrderByDescending(p=>ProDevelopment.Delta(p)).ToArray();
                content.Add(Text("Player development",20,true));content.Add(Text("Season rating changes compared with league players at the same position.",12));
                if(own.Length==0){content.Add(Text("Development comparisons appear once a season-start rating is available."));return;}
                content.Add(FootballBarChart.Create(own.Take(10).Select(p=>(p.Football.FullName,(double)ProDevelopment.Delta(p)!.Value)),"+0;-0;0"));
                content.Add(Table([("Player",150),("Change",60),("Pos avg Δ",85),("Pos",45),("OVR",50),("Upside",75)],own.Select(p=>new View[]{Link(p.Football.FullName,()=>Player(p)),Text(ProDevelopment.Delta(p)!.Value.ToString("+0;-0;0")),Text(players.Where(x=>x.Football.Position==p.Football.Position).Average(x=>ProDevelopment.Delta(x)!.Value).ToString("+0.0;-0.0;0.0")),Text(p.Football.Position.ToString()),Text(p.Football.OverallRating.ToString()),Text(ProDevelopment.Headroom(p)+" OVR")})));
                return;
            }
            if(analyticsSection==4)
            {
                content.Add(Text("Draft outcomes",20,true));var cohorts=ProAnalytics.DraftOutcomes(s,s.UserTeam);
                if(cohorts.Count==0){content.Add(Text("Your drafted classes appear here after your first selection."));return;}
                content.Add(FootballBarChart.Create(cohorts.Select(c=>($"{c.Year} R{c.Round}",c.TeamGrowth)),"+0.0;-0.0;0.0"));
                content.Add(Table([("Class / round",90),("Team Δ",70),("League Δ",85),("Picks",45),("Draft OVR",75),("Now OVR",75)],cohorts.Select(c=>new View[]{Text($"{c.Year} R{c.Round}"),Text(c.TeamGrowth.ToString("+0.0;-0.0;0.0")),Text(c.LeagueGrowth.ToString("+0.0;-0.0;0.0")),Text(c.Count.ToString()),Text(c.DraftOverall.ToString("0.0")),Text(c.CurrentOverall.ToString("0.0"))})));
                content.Add(Text("League comparison uses the same draft class and round. Players no longer present in the save are excluded.",12));return;
            }
            if(analyticsSection==5)
            {
                content.Add(Text("Career records",20,true));var scope=new Picker{ItemsSource=new[]{"Your franchise","League"},SelectedIndex=0};var records=new ContentView();content.Add(scope);content.Add(records);
                void Records(){var rows=ProAnalytics.CareerRecords(s,scope.SelectedIndex==0?s.UserTeam:null);records.Content=rows.Count==0?Text("Records appear after regular-season games."):Table([("Record",100),("Player",150),("Total",75),("Team",170)],rows.Select(r=>new View[]{Text(r.Category),Text(r.Name),Text(r.Total.ToString("N0")),Text(r.Team)}));}
                scope.SelectedIndexChanged+=(_,_)=>Records();Records();return;
            }
            var selected=ProLeagueComparisons.Build(s).Where(c=>analyticsSection==0?c.Unit is "pts/g" or "yd/g" or "TO/g":analyticsSection==1?c.Unit is "OVR" or "years":c.Unit is "$m" or "picks").ToArray();
            content.Add(Text("Your team vs league average and leading team · tied values share a rank",12));
            if(selected.Length>0)
            {
                var metric=new Picker{Title="Compare metric",ItemsSource=selected.Select(c=>c.Metric).ToArray(),SelectedIndex=0};var chart=new ContentView();content.Add(metric);content.Add(chart);
                void Chart(){if(metric.SelectedIndex<0)return;var c=selected[metric.SelectedIndex];chart.Content=FootballBarChart.Create(new[]{(s.UserTeam,c.Team),("League average",c.Average),("Leader · "+c.LeaderTeam,c.Leader)},"0.0");}
                metric.SelectedIndexChanged+=(_,_)=>Chart();Chart();
                content.Add(Table([("Metric",170),("Your team",80),("League avg",85),("Leader",85),("Rank",65)],selected.Select(c=>new View[]{Text(c.Metric+" · "+c.Unit),Text(c.Team.ToString("0.0")),Text(c.Average.ToString("0.0")),Text(c.Leader.ToString("0.0")),Text($"{c.Rank}/{c.Teams}")})));
            }
            else content.Add(Text("Regular-season comparisons appear after your team plays."));
            if(analyticsSection==0)
            {
                var games=s.Games.Where(g=>g.Phase==ProPhase.RegularSeason&&g.Football.HasPlayed&&g.Football.InvolvesTeam(s.UserTeam)).Select(g=>g.Football);
                content.Add(Panel("Scoring margin by game",FootballBarChart.Create(games.Select(g=>($"Week {g.Week}",(double)(g.HomeTeamName==s.UserTeam?g.HomeScore!.Value-g.AwayScore!.Value:g.AwayScore!.Value-g.HomeScore!.Value))))));
            }
            if(analyticsSection==2)
            {
                var roster=s.Players.Where(p=>p.Football.TeamName==s.UserTeam&&!p.Retired).ToDictionary(p=>p.Football.PlayerId);
                content.Add(Panel("Cap by position · $m",FootballBarChart.Create(s.Contracts.Where(c=>c.Active&&c.Team==s.UserTeam&&roster.ContainsKey(c.PlayerId)).GroupBy(c=>roster[c.PlayerId].Football.Position).Select(g=>(g.Key.ToString(),g.Sum(c=>ProEconomy.CapHit(c,s.Year))/1_000_000d)),"0.00")));
                content.Add(Panel("Future committed cap · $m",FootballBarChart.Create(Enumerable.Range(s.Year,4).Select(y=>(y.ToString(),(s.Contracts.Where(c=>c.Team==s.UserTeam).Sum(c=>ProEconomy.CapHit(c,y))+s.DeadCap.Where(d=>d.Team==s.UserTeam&&d.Year==y).Sum(d=>d.Amount))/1_000_000d)),"0.00")));
                content.Add(Panel("Draft inventory",FootballBarChart.Create(s.Picks.Where(p=>p.Owner==s.UserTeam&&ProTrade.Available(s,p)).GroupBy(p=>p.Year).OrderBy(g=>g.Key).Select(g=>(g.Key.ToString(),(double)g.Count())))));
            }
        }
        picker.SelectedIndexChanged+=(_,_)=>Draw();Draw();
    }
}
