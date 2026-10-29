using DynastyManager.Core.Models;
namespace ProFootball.Core;
public sealed record ProLeagueComparison(string Metric,string Unit,double Team,double Average,double Leader,string LeaderTeam,int Rank,int Teams,bool LowerIsBetter);
public static class ProLeagueComparisons
{
    public static IReadOnlyList<ProLeagueComparison> Build(ProFranchise s)
    {
        var result=new List<ProLeagueComparison>();var active=s.Players.Where(p=>!p.Retired&&!p.Reserve&&!p.PracticeSquad).ToArray();
        void Add(string metric,string unit,Func<string,double?> value,bool lower=false)
        {
            var rows=s.League.Teams.Select(t=>(Team:t.Name,Value:value(t.Name))).Where(x=>x.Value.HasValue).Select(x=>(x.Team,Value:x.Value!.Value)).ToArray();
            var own=rows.FirstOrDefault(x=>x.Team==s.UserTeam);if(!rows.Any(x=>x.Team==s.UserTeam))return;
            var leader=lower?rows.OrderBy(x=>x.Value).First():rows.OrderByDescending(x=>x.Value).First();
            var rank=1+rows.Count(x=>lower?x.Value<own.Value-1e-9:x.Value>own.Value+1e-9);
            result.Add(new(metric,unit,own.Value,rows.Average(x=>x.Value),leader.Value,leader.Team,rank,rows.Length,lower));
        }
        double? Average(string team,Func<ProPlayer,double> select,Position? position=null)
        {var pool=active.Where(p=>p.Football.TeamName==team&&(position is null||p.Football.Position==position)).ToArray();return pool.Length==0?null:pool.Average(select);}
        Add("Active roster overall","OVR",t=>Average(t,p=>p.Football.OverallRating));
        Add("Active roster age","years",t=>Average(t,p=>p.Age),true);
        foreach(var pos in Enum.GetValues<Position>())Add(pos+" overall","OVR",t=>Average(t,p=>p.Football.OverallRating,pos));
        foreach(var year in Enumerable.Range(s.Year,4))
            Add(year+" committed cap","$m",t=>(s.Contracts.Where(c=>c.Team==t).Sum(c=>ProEconomy.CapHit(c,year))+s.DeadCap.Where(c=>c.Team==t&&c.Year==year).Sum(c=>c.Amount))/1_000_000d,true);
        var games=s.Games.Where(g=>g.Phase==ProPhase.RegularSeason&&g.Football.HasPlayed).Select(g=>g.Football).ToArray();
        double? GameAverage(string team,Func<ScheduledGame,bool,double> stat,bool requireStats=false)
        {var played=games.Where(g=>g.InvolvesTeam(team)&&(!requireStats||g.HomeStats is not null&&g.AwayStats is not null)).ToArray();return played.Length==0?null:played.Average(g=>stat(g,g.HomeTeamName==team));}
        Add("Points scored","pts/g",t=>GameAverage(t,(g,h)=>(h?g.HomeScore:g.AwayScore)??0));
        Add("Points allowed","pts/g",t=>GameAverage(t,(g,h)=>(h?g.AwayScore:g.HomeScore)??0),true);
        Add("Offensive yards","yd/g",t=>GameAverage(t,(g,h)=>(h?g.HomeStats:g.AwayStats)!.TotalYards,true));
        Add("Yards allowed","yd/g",t=>GameAverage(t,(g,h)=>(h?g.AwayStats:g.HomeStats)!.TotalYards,true),true);
        Add("Turnover margin","TO/g",t=>GameAverage(t,(g,h)=>(h?g.AwayStats!.Turnovers-g.HomeStats!.Turnovers:g.HomeStats!.Turnovers-g.AwayStats!.Turnovers),true));
        foreach(var year in Enumerable.Range(s.Year+1,3))Add(year+" draft picks","picks",t=>s.Picks.Count(p=>p.Owner==t&&p.Year==year&&ProTrade.Available(s,p)));
        return result;
    }
}
