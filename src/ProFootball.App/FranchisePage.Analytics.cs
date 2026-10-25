using DynastyManager.App;
using ProFootball.Core;
namespace ProFootball.App;
public sealed partial class FranchisePage
{
    void Analytics()
    {
        var s = state!; var roster = s.Players.Where(p => p.Football.TeamName == s.UserTeam && !p.Retired).ToArray();
        body.Add(Text("Franchise analytics", 23, true));
        body.Add(Cards(
            Panel("Roster quality by position", FootballBarChart.Create(roster.GroupBy(p => p.Football.Position).OrderBy(g => g.Key).Select(g => (g.Key.ToString(), g.Average(p => p.Football.OverallRating))), "0.0")),
            Panel("Age profile", FootballBarChart.Create(roster.GroupBy(p => p.Age < 25 ? "Under 25" : p.Age < 30 ? "25–29" : "30+").Select(g => (g.Key, (double)g.Count()))))));
        var ids = roster.ToDictionary(p => p.Football.PlayerId);
        body.Add(Panel("Current cap by position · $m", FootballBarChart.Create(s.Contracts.Where(c => c.Team == s.UserTeam && c.Active && ids.ContainsKey(c.PlayerId)).GroupBy(c => ids[c.PlayerId].Football.Position).Select(g => (g.Key.ToString(), g.Sum(c => ProEconomy.CapHit(c, s.Year)) / 1_000_000d)), "0.00")));
        body.Add(Panel("Future committed cap · $m", FootballBarChart.Create(Enumerable.Range(s.Year, 4).Select(y => (y.ToString(), (s.Contracts.Where(c => c.Team == s.UserTeam).Sum(c => ProEconomy.CapHit(c, y)) + s.DeadCap.Where(c => c.Team == s.UserTeam && c.Year == y).Sum(c => c.Amount)) / 1_000_000d)), "0.00"), Text("Current signed commitments only; future cap growth and unsigned players are excluded.", 12)));
        var games = s.Games.Where(g => g.Football.HasPlayed && g.Football.InvolvesTeam(s.UserTeam)).Select(g => g.Football).ToArray();
        body.Add(Panel("Scoring margin by game", FootballBarChart.Create(games.Select(g => ($"W{g.Week} · {g.AwayTeamName} at {g.HomeTeamName}", (double)(g.HomeTeamName == s.UserTeam ? (g.HomeScore ?? 0) - (g.AwayScore ?? 0) : (g.AwayScore ?? 0) - (g.HomeScore ?? 0)))))));
        body.Add(Panel("League cap spending · $m", FootballBarChart.Create(s.League.Teams.OrderByDescending(t => ProEconomy.Used(s, t.Name)).Select(t => (t.Abbreviation, ProEconomy.Used(s, t.Name) / 1_000_000d)), "0.00")));
        body.Add(Panel("Draft inventory", FootballBarChart.Create(s.Picks.Where(p => p.Owner == s.UserTeam && ProTrade.Available(s, p)).GroupBy(p => p.Year).OrderBy(g => g.Key).Select(g => (g.Key.ToString(), (double)g.Count())))));
    }
}
