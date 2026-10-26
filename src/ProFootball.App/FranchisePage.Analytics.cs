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
    void Analytics()
    {
        var s = state!; var roster = s.Players.Where(p => p.Football.TeamName == s.UserTeam && !p.Retired).ToArray();
        body.Add(Text("Franchise analytics", 23, true));
        var comparisons=ProLeagueComparisons.Build(s);var sections=new[]{"Team performance","Roster quality","Cap & draft"};
        var section=new Picker {ItemsSource=sections,SelectedIndex=0};body.Add(section);var comparisonBody=new VerticalStackLayout {Spacing=10};body.Add(comparisonBody);
        void DrawComparisons()
        {
            comparisonBody.Clear();var selected=comparisons.Where(c=>section.SelectedIndex==0?c.Unit is "pts/g" or "yd/g" or "TO/g":section.SelectedIndex==1?c.Unit is "OVR" or "years":c.Unit is "$m" or "picks").ToArray();
            comparisonBody.Add(Text("Your team vs league average and leading team · tied values share a rank",12));
            if(selected.Length==0){comparisonBody.Add(Text("Regular-season comparisons appear after your team plays. Preseason and exhibition results are excluded."));return;}
            comparisonBody.Add(Table([("Metric",195),("Unit",65),("Your team",90),("League avg",100),("Leader",95),("Leading team",190),("Rank",75)],selected.Select(c=>new View[]{Text(c.Metric),Text(c.Unit),Text(c.Team.ToString("0.0")),Text(c.Average.ToString("0.0")),Text(c.Leader.ToString("0.0")),Text(c.LeaderTeam),Text($"{c.Rank}/{c.Teams}")})));
            comparisonBody.Add(Text("Performance uses per-game regular-season values and teams with results. Roster comparisons use active players; lower age means younger, and lower committed cap means less committed—not necessarily a stronger team.",12));
            var metric=new Picker {Title="Compare metric",ItemsSource=selected.Select(c=>c.Metric).ToArray(),SelectedIndex=0};var chart=new ContentView();comparisonBody.Add(metric);comparisonBody.Add(chart);
            void DrawChart(){var c=selected[metric.SelectedIndex];chart.Content=FootballBarChart.Create(new[]{(s.UserTeam,c.Team),("League average",c.Average),("Leader · "+c.LeaderTeam,c.Leader)},"0.0");}
            metric.SelectedIndexChanged+=(_,_)=>DrawChart();DrawChart();
        }
        section.SelectedIndexChanged+=(_,_)=>DrawComparisons();DrawComparisons();
        var ids = roster.ToDictionary(p => p.Football.PlayerId);
        body.Add(Panel("Current cap by position · $m", FootballBarChart.Create(s.Contracts.Where(c => c.Team == s.UserTeam && c.Active && ids.ContainsKey(c.PlayerId)).GroupBy(c => ids[c.PlayerId].Football.Position).Select(g => (g.Key.ToString(), g.Sum(c => ProEconomy.CapHit(c, s.Year)) / 1_000_000d)), "0.00")));
        body.Add(Panel("Future committed cap · $m", FootballBarChart.Create(Enumerable.Range(s.Year, 4).Select(y => (y.ToString(), (s.Contracts.Where(c => c.Team == s.UserTeam).Sum(c => ProEconomy.CapHit(c, y)) + s.DeadCap.Where(c => c.Team == s.UserTeam && c.Year == y).Sum(c => c.Amount)) / 1_000_000d)), "0.00"), Text("Current signed commitments only; future cap growth and unsigned players are excluded.", 12)));
        var games = s.Games.Where(g => g.Football.HasPlayed && g.Football.InvolvesTeam(s.UserTeam)).Select(g => g.Football).ToArray();
        body.Add(Panel("Scoring margin by game", FootballBarChart.Create(games.Select(g => ($"W{g.Week} · {g.AwayTeamName} at {g.HomeTeamName}", (double)(g.HomeTeamName == s.UserTeam ? (g.HomeScore ?? 0) - (g.AwayScore ?? 0) : (g.AwayScore ?? 0) - (g.HomeScore ?? 0)))))));
        body.Add(Panel("League cap spending · $m", FootballBarChart.Create(s.League.Teams.OrderByDescending(t => ProEconomy.Used(s, t.Name)).Select(t => (t.Abbreviation, ProEconomy.Used(s, t.Name) / 1_000_000d)), "0.00")));
        body.Add(Panel("Draft inventory", FootballBarChart.Create(s.Picks.Where(p => p.Owner == s.UserTeam && ProTrade.Available(s, p)).GroupBy(p => p.Year).OrderBy(g => g.Key).Select(g => (g.Key.ToString(), (double)g.Count())))));
    }
}
