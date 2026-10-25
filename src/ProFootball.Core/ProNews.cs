using System.Security.Cryptography;
using System.Text;
namespace ProFootball.Core;

/// <summary>Offline stories from completed results and actual transactions, with stable source identities.</summary>
public static class ProNews
{
    static string Key(string source) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)))[..24];
    public static ProFranchise Refresh(ProFranchise s)
    {
        var stories = s.News.ToDictionary(n => n.Id);
        void Add(ProNewsStory story) => stories.TryAdd(story.Id, story);
        foreach (var g in s.Games.Where(g => g.Football.HasPlayed))
        {
            var f = g.Football; var margin = Math.Abs((f.HomeScore ?? 0) - (f.AwayScore ?? 0));
            Add(new("game-" + f.GameId, f.SeasonYear, f.Week, g.Phase == ProPhase.Playoffs ? "Playoffs" : "Results",
                f.HomeScore == f.AwayScore ? "All square: " + f.AwayTeamName + " and " + f.HomeTeamName : f.WinnerTeamName + (margin >= 21 ? " rolls to a convincing win" : margin <= 3 ? " survives a close finish" : " takes the win"),
                $"{g.Phase} · {f.AwayTeamName} {f.AwayScore} – {f.HomeScore} {f.HomeTeamName}", f.WinnerTeamName, f.GameId));
        }
        foreach (var trade in s.TradeHistory)
            Add(new("trade-" + trade.Id, trade.Year, trade.Week, "Trades", trade.From + " and " + trade.To + " complete deal",
                $"{trade.GivenPlayers.Count + trade.ReceivedPlayers.Count} players and {trade.GivenPicks.Count + trade.ReceivedPicks.Count} draft picks changed hands.", trade.From));
        foreach (var text in s.Transactions)
        {
            // Private GM decisions are stored separately and never published here.
            var category = text.Contains("hired") || text.Contains("dismissed") || text.Contains("job") ? "Coaching" : text.Contains("draft", StringComparison.OrdinalIgnoreCase) ? "Draft" : "Transactions";
            Add(new(Key("transaction:" + text), s.Year, s.Week, category, text, "Confirmed league transaction."));
        }
        var news = stories.Values.OrderByDescending(n => n.Year).ThenByDescending(n => n.Week).Take(1000).ToList();
        return s with { News = news, ReadNewsIds = s.ReadNewsIds.Where(id => news.Any(n => n.Id == id)).Distinct().ToList() };
    }
}
