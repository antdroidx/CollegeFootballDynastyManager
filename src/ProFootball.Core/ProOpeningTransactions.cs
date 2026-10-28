namespace ProFootball.Core;

/// <summary>Confirmed opening roster transactions. Factory only: never replay real trades over a played save.</summary>
public static class ProOpeningTransactions
{
    public const string ObservedOn = "2026-10-28";
    public sealed record OpeningTrade(string Player, string From, string To, IReadOnlyList<(int Year,int Round)> Picks, string Source);
    public static IReadOnlyList<OpeningTrade> Trades { get; } = [
        new("Quenton Nelson","IND","SEA",[(2027,3),(2028,5)],"https://www.colts.com/news/colts-trade-g-quenton-nelson-to-seattle-seahawks-for-2-future-nfl-draft-picks"),
        new("DeForest Buckner","IND","SF",[(2027,4)],"https://www.colts.com/news/colts-acquire-2027-fourth-round-pick-in-exchange-for-dt-deforest-buckner-in-trade-with-san-francisco-49ers")
    ];
    internal static ProFranchise InitializeNew(ProFranchise s)
    {
        if(s.OpeningTransactionsSnapshot==ObservedOn)return s;
        if(s.Year!=2026||s.History.Count!=0||s.TradeHistory.Count!=0||s.Games.Any(g=>g.Football.HasPlayed)
            ||s.Contracts.Any(c=>c.LastChangedYear!=0))
            throw new InvalidOperationException("Opening roster updates apply only to new franchises.");
        var teams=s.League.Teams.ToDictionary(t=>t.Abbreviation,t=>t.Name);
        foreach(var trade in Trades)
        {
            var from=teams[trade.From];var to=teams[trade.To];
            var player=s.Players.Single(p=>p.Football.FullName.Equals(trade.Player,StringComparison.OrdinalIgnoreCase));
            if(player.Football.TeamName!=from)throw new InvalidDataException("Opening trade roster baseline differs: "+trade.Player);
            var picks=trade.Picks.Select(item=>s.Picks.Single(p=>p.Year==item.Year&&p.Round==item.Round&&p.OriginalTeam==to&&p.Owner==to)).ToArray();
            // Use the existing assignment accounting: seller retains paid bonus obligations;
            // buyer assumes remaining playing salaries. Do not ask trade AI to approve an already confirmed trade.
            s=ProTrade.Move(s,player,to);
            s=s with{Picks=s.Picks.Select(p=>picks.Any(x=>x.Id==p.Id)?p with{Owner=from}:p).ToList()};
        }
        return s with{OpeningTransactionsSnapshot=ObservedOn};
    }
}
