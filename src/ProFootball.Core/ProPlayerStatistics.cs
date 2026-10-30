using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
namespace ProFootball.Core;

public static class ProPlayerStatistics
{
    public static IReadOnlyList<PlayerSeasonStatLine> Lines(ProFranchise state, bool playoffs, bool career)
    {
        var phase=playoffs?ProPhase.Playoffs:ProPhase.RegularSeason;
        return state.Games.Concat(state.History.SelectMany(h=>h.Games))
            .Where(g=>g.Phase==phase&&g.Football.HasPlayed&&(career||g.Football.SeasonYear==state.Year))
            .DistinctBy(g=>(g.Football.SeasonYear,g.Football.GameId)).SelectMany(g=>g.Football.PlayerStats).ToArray();
    }
    public static IReadOnlyList<PlayerSeasonStatLine> Totals(ProFranchise state,bool playoffs,bool career,string? team=null)
        =>Lines(state,playoffs,career).Where(p=>team is null||p.TeamName==team).GroupBy(p=>p.PlayerId)
            .Select(g=>PlayerStatTotals.Total(g) with {TeamName=g.Select(p=>p.TeamName).Distinct().Count()>1?"Multiple teams":g.First().TeamName}).ToArray();
}
