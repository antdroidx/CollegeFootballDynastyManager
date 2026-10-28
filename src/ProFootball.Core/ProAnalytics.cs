using DynastyManager.Core.Models;
namespace ProFootball.Core;
public sealed record ProDraftOutcome(int Year,int Round,int Count,double DraftOverall,double CurrentOverall,double LeagueGrowth,double TeamGrowth);
public sealed record ProCareerRecord(string Category,Guid PlayerId,string Name,string Team,int Total);
public static class ProAnalytics
{
    public static IReadOnlyList<ProDraftOutcome> DraftOutcomes(ProFranchise s,string team)
    {
        var players=s.Players.ToDictionary(p=>p.Football.PlayerId);
        var known=s.DraftHistory.Where(d=>players.ContainsKey(d.Player.Football.PlayerId)).ToArray();
        double Growth(DraftSelection d)=>players[d.Player.Football.PlayerId].Football.OverallRating-d.Player.Football.OverallRating;
        return known.Where(d=>d.Team==team).GroupBy(d=>(d.Year,d.Round)).OrderByDescending(g=>g.Key.Year).ThenBy(g=>g.Key.Round)
            .Select(g=>new ProDraftOutcome(g.Key.Year,g.Key.Round,g.Count(),g.Average(d=>d.Player.Football.OverallRating),g.Average(d=>players[d.Player.Football.PlayerId].Football.OverallRating),known.Where(d=>d.Year==g.Key.Year&&d.Round==g.Key.Round).Average(Growth),g.Average(Growth))).ToArray();
    }
    public static IReadOnlyList<ProCareerRecord> CareerRecords(ProFranchise s,string? team=null)
    {
        var lines=s.History.SelectMany(h=>h.Games).Concat(s.Games).Where(g=>g.Phase==ProPhase.RegularSeason&&g.Football.HasPlayed).DistinctBy(g=>g.Football.GameId).SelectMany(g=>g.Football.PlayerStats).Where(p=>team is null||p.TeamName==team).ToArray();
        var metrics=new (string Name,Func<PlayerSeasonStatLine,int> Value)[]{("Passing yards",p=>p.PassYards),("Passing TD",p=>p.PassingTouchdowns),("Rushing yards",p=>p.RushYards),("Receiving yards",p=>p.ReceivingYards),("Tackles",p=>p.Tackles),("Sacks",p=>p.Sacks),("Interceptions",p=>p.Interceptions)};
        return metrics.SelectMany(m=>lines.GroupBy(p=>p.PlayerId).Select(g=>new ProCareerRecord(m.Name,g.Key,g.Last().PlayerName,g.Last().TeamName,g.Sum(m.Value))).Where(r=>r.Total>0).OrderByDescending(r=>r.Total).ThenBy(r=>r.PlayerId).Take(5)).ToArray();
    }
}
