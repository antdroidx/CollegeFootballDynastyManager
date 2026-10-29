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

    public static IReadOnlyList<Guid> CounterPicks(ProFranchise s, string owner, double shortfall, IEnumerable<Guid> selected,
        double maximumValue=double.MaxValue,int? minimumYear=null,int? maximumYear=null)
    {
        if(shortfall<=0)return [];
        var used=selected.ToHashSet();
        var picks=s.Picks.Where(p=>p.Owner==owner&&ProTrade.Available(s,p)&&!used.Contains(p.Id)
            &&(!minimumYear.HasValue||p.Year>=minimumYear)&&(!maximumYear.HasValue||p.Year<=maximumYear))
            .Select(p=>(Pick:p,Value:ProTrade.PickValue(s,p))).OrderBy(p=>p.Value).ToArray();
        Guid[] best=[];var cost=maximumValue;
        void Consider(double value,Guid a,Guid? b=null,Guid? c=null){var count=c.HasValue?3:b.HasValue?2:1;if(value>=shortfall&&value<=cost&&(best.Length==0||value<cost||value==cost&&count<best.Length)){cost=value;best=c.HasValue?[a,b!.Value,c.Value]:b.HasValue?[a,b.Value]:[a];}}
        for(var i=0;i<picks.Length;i++)
        {
            var a=picks[i];Consider(a.Value,a.Pick.Id);
            for(var j=i+1;j<picks.Length&&a.Value+picks[j].Value<=cost;j++)
            {
                var b=picks[j];Consider(a.Value+b.Value,a.Pick.Id,b.Pick.Id);
                if(a.Value+b.Value>=shortfall)continue;
                for(var k=j+1;k<picks.Length&&a.Value+b.Value+picks[k].Value<=cost;k++)
                    Consider(a.Value+b.Value+picks[k].Value,a.Pick.Id,b.Pick.Id,picks[k].Pick.Id);
            }
        }
        return best;
    }
}
