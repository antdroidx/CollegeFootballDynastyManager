using DynastyManager.Core.Models;
namespace ProFootball.Core;

/// <summary>Trade availability and deterministic CPU market discovery; evaluations never mutate rosters.</summary>
public static class ProTradeMarket
{
    public static bool IsOpen(ProFranchise s) => s.Phase != ProPhase.Playoffs && (s.Phase != ProPhase.RegularSeason || s.Week <= 9);
    public static ProFranchise List(ProFranchise s, string team, Guid id, bool listed)
    {
        if (!IsOpen(s)) throw new InvalidOperationException("The trade deadline has passed.");
        var p = s.Players.SingleOrDefault(p => p.Football.PlayerId == id && p.Football.TeamName == team && !p.Retired)
            ?? throw new InvalidOperationException("Only your current players can be listed.");
        if (team == s.UserTeam) ProCareer.RequirePlayer(s,id);
        var block = s.TradeBlock.Where(x=>x!=id).ToList(); if(listed) block.Add(id);
        return s with { TradeBlock = block };
    }
    public static ProFranchise Refresh(ProFranchise s)
    {
        var owned = s.Players.Where(p=>!p.Retired && s.League.Teams.Any(t=>t.Name==p.Football.TeamName)).ToDictionary(p=>p.Football.PlayerId);
        var block = s.TradeBlock.Where(id=>owned.TryGetValue(id,out var p)&&p.Football.TeamName==s.UserTeam).Concat(s.Players.Where(p=>p.TradeRequested&&!p.Retired&&p.Football.TeamName==s.UserTeam).Select(p=>p.Football.PlayerId)).Distinct().ToList();
        foreach(var team in s.League.Teams.Where(t=>t.Name!=s.UserTeam))
        {
            var candidates = s.Players.Where(p=>p.Football.TeamName==team.Name&&!p.Retired&&!p.Reserve&&!p.PracticeSquad&&(p.TradeRequested||p.Football.DepthChartOrder>1&&p.Football.OverallRating>=60&&ProCpu.Need(s,team.Name,p.Football.Position)==0))
                .OrderByDescending(p=>p.TradeRequested).ThenByDescending(p=>p.Age>=29?1:0).ThenByDescending(p=>p.Football.OverallRating).Take(2);
            block.AddRange(candidates.Select(p=>p.Football.PlayerId));
        }
        return s with { TradeBlock = block.Distinct().ToList() };
    }
    public static IReadOnlyList<string> InterestedTeams(ProFranchise s, ProPlayer player) => s.League.Teams
        .Where(t=>t.Name!=player.Football.TeamName&&ProRoster.CountForLimit(s,t.Name)<ProRoster.Limit(s)&&ProEconomy.Room(s,t.Name)>=s.Contracts.Where(c=>c.Active&&c.PlayerId==player.Football.PlayerId).Sum(c=>c.Years.FirstOrDefault(y=>y.Year==s.Year)?.Salary??0))
        .OrderByDescending(t=>ProCpu.Need(s,t.Name,player.Football.Position)*100+ProTrade.PlayerValue(s,player,t.Name))
        .Take(5).Select(t=>t.Name).ToArray();

    public static double PackageValue(ProFranchise s, IEnumerable<Guid> players, IEnumerable<Guid> picks, string recipient) =>
        players.Sum(id=>ProTrade.PlayerValue(s,s.Players.Single(p=>p.Football.PlayerId==id),recipient)) + picks.Sum(id=>ProTrade.PickValue(s,s.Picks.Single(p=>p.Id==id)));

    public static IReadOnlyList<Guid> CounterPicks(ProFranchise s, string owner, double shortfall, IEnumerable<Guid> selected)
    {
        if(shortfall<=0)return [];
        var used=selected.ToHashSet(); var result=new List<Guid>();
        // Smallest sufficient asset first; otherwise combine at most three available picks.
        var picks=s.Picks.Where(p=>p.Owner==owner&&ProTrade.Available(s,p)&&!used.Contains(p.Id)).OrderBy(p=>ProTrade.PickValue(s,p)).ToList();
        var single=picks.FirstOrDefault(p=>ProTrade.PickValue(s,p)>=shortfall);
        if(single is not null)return [single.Id];
        foreach(var p in picks.OrderByDescending(p=>ProTrade.PickValue(s,p)).Take(3)) { result.Add(p.Id);shortfall-=ProTrade.PickValue(s,p);if(shortfall<=0)return result; }
        return [];
    }
}
