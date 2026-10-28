using System.Security.Cryptography;
using System.Text;
using DynastyManager.Core.Models;
namespace ProFootball.Core;

/// <summary>Postseason ordering follows NFL division and wild-card procedures. Final coin toss is seeded for save stability.</summary>
public static class ProStandings
{
    public static List<ProStanding> Build(ProFranchise s) => new Order(s).Rank(s.League.Teams.Select(t => t.Name).ToList());
    public static List<ProStanding> Seeds(ProFranchise s, string conference)
    {
        var order = new Order(s);
        var divisions = s.League.Teams.Where(t => t.Conference == conference).GroupBy(t => t.Division).OrderBy(g => g.Key)
            .Select(g => order.Rank(g.Select(t => t.Name).ToList())).ToArray();
        var winners = order.Rank(divisions.Select(g => g[0].Team.Name).ToList());
        // Preserve each division's original order while selecting one wild card at a time.
        var queues = divisions.Select(g => g.Skip(1).Select(r => r.Team.Name).ToList()).ToArray();
        var wild = new List<ProStanding>();
        while (wild.Count < 3 && queues.Any(q => q.Count > 0))
        {
            var chosen = order.Rank(queues.Where(q => q.Count > 0).Select(q => q[0]).ToList())[0];
            wild.Add(chosen); foreach (var q in queues) q.Remove(chosen.Team.Name);
        }
        return winners.Concat(wild).ToList();
    }
    sealed class Order
    {
        readonly ProFranchise state;
        readonly ScheduledGame[] games;
        readonly Dictionary<string, ProTeam> teams;
        readonly Dictionary<string, ScheduledGame[]> own;
        readonly Dictionary<string, ProStanding> rows;
        readonly Dictionary<string, (int W, int L, int T)> records;
        public Order(ProFranchise s)
        {
            state = s; teams = s.League.Teams.ToDictionary(t => t.Name);
            games = s.Games.Where(g => g.Phase == ProPhase.RegularSeason && g.Football.SeasonYear == s.Year && g.Football.HasPlayed).Select(g => g.Football).ToArray();
            own = teams.Keys.ToDictionary(t => t, t => games.Where(g => g.InvolvesTeam(t)).ToArray());
            records = own.ToDictionary(x => x.Key, x => (x.Value.Count(g => Win(g,x.Key)), x.Value.Count(g => !Win(g,x.Key) && g.HomeScore != g.AwayScore), x.Value.Count(g => g.HomeScore == g.AwayScore)));
            rows = teams.Values.ToDictionary(t => t.Name, t => { var r = records[t.Name]; return new ProStanding(t,r.W,r.L,r.T,Percent(own[t.Name],t.Name),Percent(own[t.Name].Where(g => Division(t.Name,Opp(g,t.Name))),t.Name),Percent(Conference(t.Name),t.Name),Strength(t.Name,false),Net(own[t.Name],t.Name)); });
        }
        static bool Win(ScheduledGame g,string t) => g.HomeScore != g.AwayScore && g.WinnerTeamName == t;
        static string Opp(ScheduledGame g,string t) => g.HomeTeamName == t ? g.AwayTeamName : g.HomeTeamName;
        static double Percent(IEnumerable<ScheduledGame> source,string t) { var a=source.ToArray(); return a.Length==0?0:a.Sum(g => g.HomeScore==g.AwayScore?.5:Win(g,t)?1d:0)/a.Length; }
        bool Division(string a,string b) => teams[a].Conference==teams[b].Conference && teams[a].Division==teams[b].Division;
        IEnumerable<ScheduledGame> Conference(string t) => own[t].Where(g => teams[Opp(g,t)].Conference==teams[t].Conference);
        static int Net(IEnumerable<ScheduledGame> source,string t) => source.Sum(g => g.HomeTeamName==t?g.HomeScore!.Value-g.AwayScore!.Value:g.AwayScore!.Value-g.HomeScore!.Value);
        double Strength(string t,bool victory)
        {
            var opponents=own[t].Where(g => !victory || Win(g,t)).Select(g => records[Opp(g,t)]).ToArray();
            var total=opponents.Sum(r=>r.W+r.L+r.T); return total==0?0:opponents.Sum(r=>r.W+r.T*.5)/total;
        }
        int Points(string t,bool allowed) => own[t].Sum(g => (g.HomeTeamName==t)^allowed?g.HomeScore??0:g.AwayScore??0);
        double CombinedRank(string t,bool conference)
        {
            var field=teams.Keys.Where(x => !conference || teams[x].Conference==teams[t].Conference).ToArray();
            return -(2+field.Count(x=>Points(x,false)>Points(t,false))+field.Count(x=>Points(x,true)<Points(t,true)));
        }
        int Touchdowns(string t) => own[t].Sum(g => g.PlayerStats.Sum(p => (p.TeamName==t?1:-1)*(p.RushingTouchdowns+p.ReceivingTouchdowns+p.DefensiveTouchdowns+p.KickReturnTouchdowns+p.PuntReturnTouchdowns)));
        public List<ProStanding> Rank(List<string> source)
        {
            var result=new List<ProStanding>();
            foreach(var group in source.GroupBy(t=>rows[t].Percentage).OrderByDescending(g=>g.Key))
            {
                var remaining=group.ToList();
                while(remaining.Count>0) { var winner=Resolve(remaining); result.Add(rows[winner]); remaining.Remove(winner); }
            }
            return result;
        }
        string Resolve(List<string> original)
        {
            if(original.Count==1)return original[0];
            var division=original.All(t=>Division(t,original[0]));
            var tied=original.ToList();
            if(!division && tied.Count>2)
                tied=tied.GroupBy(t=>(teams[t].Conference,teams[t].Division)).Select(g=>Rank(g.ToList())[0].Team.Name).ToList();
            if(tied.Count==1)return tied[0];
            var common=tied.Select(t=>own[t].Select(g=>Opp(g,t)).ToHashSet()).Aggregate((a,b)=>{a.IntersectWith(b);return a;});
            IEnumerable<ScheduledGame> Common(string t)=>own[t].Where(g=>common.Contains(Opp(g,t)));
            IEnumerable<ScheduledGame> Head(string t)=>own[t].Where(g=>tied.Contains(Opp(g,t)));
            var metrics=new List<Func<string,double>>();
            if(division || tied.Count==2)
            {
                if(tied.All(t=>Head(t).Any()))metrics.Add(t=>Percent(Head(t),t));
            }
            else
            {
                // Sweep applies only to a club beating (or losing to) every other tied club.
                bool Sweep(string t,bool win)=>tied.Where(x=>x!=t).All(x=>Head(t).Any(g=>Opp(g,t)==x) && Head(t).Where(g=>Opp(g,t)==x).All(g=>win?Win(g,t):Win(g,x)));
                metrics.Add(t=>Sweep(t,true)?1:Sweep(t,false)?-1:0);
            }
            if(division)metrics.Add(t=>rows[t].DivisionPercentage);
            if(division && tied.All(t=>Common(t).Any()))metrics.Add(t=>Percent(Common(t),t));
            metrics.Add(t=>rows[t].ConferencePercentage);
            if(!division && tied.All(t=>Common(t).Count()>=4))metrics.Add(t=>Percent(Common(t),t));
            metrics.Add(t=>Strength(t,true));metrics.Add(t=>Strength(t,false));
            metrics.Add(t=>CombinedRank(t,true));metrics.Add(t=>CombinedRank(t,false));
            metrics.Add(t=>Net(division?Common(t):Conference(t),t));
            metrics.Add(t=>rows[t].Differential);metrics.Add(t=>Touchdowns(t));
            foreach(var metric in metrics)
            {
                var best=tied.Max(metric);var survivors=tied.Where(t=>Math.Abs(metric(t)-best)<1e-10).ToList();
                if(survivors.Count==1)return survivors[0];
                if(survivors.Count<tied.Count)return Resolve(survivors);
            }
            return tied.OrderBy(t=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{state.Year}-playoff-coin-{t}"))),StringComparer.Ordinal).First();
        }
    }
}
