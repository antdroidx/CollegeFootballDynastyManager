using DynastyManager.Core.Models;
namespace DynastyManager.Core.Simulation;

public sealed record TeamEfficiency(string Team,int Games,int Plays,double Offense,double Defense,double Net,double YardsPerPlay,double ExplosiveRate,double SackRate,double TurnoverRate,double? SuccessRate,double? ThirdDownRate,int ContextPlays);

/// <summary>Own game-derived efficiency model. Percentage above league value/play, corrected for opponents.</summary>
public static class FootballEfficiency
{
    public static PlayEfficiency Capture(IEnumerable<GameSnapEvent> source)
    {
        var plays=source.Where(e=>e.Outcome is SnapOutcome.Run or SnapOutcome.Completion or SnapOutcome.Incompletion or SnapOutcome.Sack or SnapOutcome.Turnover or SnapOutcome.Touchdown or SnapOutcome.Interception or SnapOutcome.Fumble or SnapOutcome.Scramble).ToArray();
        bool Success(GameSnapEvent e)=>e.Outcome is not (SnapOutcome.Turnover or SnapOutcome.Interception or SnapOutcome.Fumble) &&
            (!e.DefensiveScore && (e.Outcome==SnapOutcome.Touchdown || e.Yards>=Math.Max(1,e.Distance)*(e.Down==1?.4:e.Down==2?.6:1)));
        return new(plays.Length,plays.Sum(e=>e.Yards),plays.Count(Success),plays.Count(e=>e.Down==3),plays.Count(e=>e.Down==3&&Success(e)),plays.Where(e=>e.Outcome==SnapOutcome.Sack).Sum(e=>Math.Max(0,-e.Yards)));
    }
    sealed record Side(string Id,string Team,string Opponent,GameTeamStats Stats)
    {
        // This shared engine already includes sacks in rushing attempts/yards. Do not count them twice.
        public int Plays=>Stats.Efficiency?.Plays??Stats.PassAttempts+Stats.RushAttempts;
        public double Yards=>Stats.Efficiency?.Yards??Stats.TotalYards;
        public double Value=>Yards+20d*(Stats.PassingTouchdowns+Stats.RushingTouchdowns)-45d*Stats.Turnovers;
    }
    public static IReadOnlyList<TeamEfficiency> Build(IEnumerable<ScheduledGame> source)
    {
        var games=source.Where(g=>g.HasPlayed&&g.HomeStats is not null&&g.AwayStats is not null).DistinctBy(g=>g.GameId).ToArray();
        var sides=games.SelectMany(g=>new[]{new Side(g.GameId,g.HomeTeamName,g.AwayTeamName,g.HomeStats!),new Side(g.GameId,g.AwayTeamName,g.HomeTeamName,g.AwayStats!)}).Where(x=>x.Plays>0).ToArray();
        if(sides.Length==0)return [];
        var baseline=sides.Sum(x=>x.Value)/sides.Sum(x=>x.Plays);var scale=Math.Max(2,Math.Abs(baseline));
        double Expected(Side game,bool offense)
        {
            var rest=sides.Where(x=>x.Id!=game.Id&&(offense?x.Opponent==game.Opponent:x.Team==game.Team)).ToArray();
            var n=rest.Sum(x=>x.Plays);
            return (rest.Sum(x=>x.Value)+200*baseline)/(n+200); // shrink sparse opponents to league; exclude this matchup
        }
        return sides.Select(x=>x.Team).Distinct().Select(team=>
        {
            var own=sides.Where(x=>x.Team==team).ToArray();var against=sides.Where(x=>x.Opponent==team).ToArray();
            var n=own.Sum(x=>x.Plays);var dn=against.Sum(x=>x.Plays);
            var off=100*own.Sum(x=>x.Value-x.Plays*Expected(x,true))/n/scale;
            var def=dn==0?0:100*against.Sum(x=>x.Plays*Expected(x,false)-x.Value)/dn/scale;
            var context=own.Select(x=>x.Stats.Efficiency).OfType<PlayEfficiency>().ToArray();var recorded=context.Sum(x=>x.Plays);var thirds=context.Sum(x=>x.ThirdDowns);
            var dropbacks=own.Sum(x=>x.Stats.PassAttempts+x.Stats.SacksAllowed);
            return new TeamEfficiency(team,own.Length,n,off,def,off+def,own.Sum(x=>x.Yards)/n,
                100d*own.Sum(x=>x.Stats.ExplosivePasses20+x.Stats.ExplosiveRuns20)/n,
                dropbacks==0?0:100d*own.Sum(x=>x.Stats.SacksAllowed)/dropbacks,
                100d*own.Sum(x=>x.Stats.Turnovers)/n,recorded==0?null:100d*context.Sum(x=>x.Successful)/recorded,thirds==0?null:100d*context.Sum(x=>x.ThirdDownConversions)/thirds,recorded);
        }).OrderByDescending(x=>x.Net).ThenBy(x=>x.Team,StringComparer.Ordinal).ToArray();
    }
}
