using DynastyManager.Core.Models;
namespace ProFootball.Core;
public static class ProPickProjection
{
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<ProFranchise,System.Collections.Concurrent.ConcurrentDictionary<int,Lazy<Dictionary<string,double>>>> Cache=new();
    public static double Strength(ProFranchise s,string team,int year)
    {
        var horizon=Math.Max(0,year-s.Year);var pool=s.Players.Where(p=>p.Football.TeamName==team&&!p.Retired&&!p.PracticeSquad&&!p.Reserve).ToArray();
        var deals=s.Contracts.Where(c=>c.Active).GroupBy(c=>c.PlayerId).ToDictionary(g=>g.Key,g=>g.Last());
        double Talent(ProPlayer p)
        {
            var peak=p.Football.Position==Position.QB?31:p.Football.Position==Position.K?34:p.Football.Position==Position.RB?26:29;
            var age=p.Age+horizon;var gain=p.Age<25?Math.Min(p.Football.PotentialRating-p.Football.OverallRating,horizon*1.5):0;
            var decline=Math.Max(0,age-peak)*Math.Min(horizon,3)*.8;
            var contract=deals.GetValueOrDefault(p.Football.PlayerId);
            var uncertainty=contract is not null&&contract.Years.Max(y=>y.Year)<year?1.5:0;
            return Math.Clamp(p.Football.OverallRating+gain-decline-uncertainty,35,99);
        }
        double Sum=0,weight=0;
        foreach(var pos in Enum.GetValues<Position>())
        {
            var count=pos==Position.OL?5:pos is Position.WR or Position.CB or Position.DE or Position.DT or Position.OLB?2:1;
            var score=pool.Where(p=>p.Football.Position==pos).Select(Talent).OrderDescending().Take(count).DefaultIfEmpty(45).Average();
            var w=ProTrade.PositionWeight(pos);Sum+=score*w;weight+=w;
        }
        var roster=(Sum/weight-50)/45;var games=s.Games.Where(g=>g.Phase==ProPhase.RegularSeason&&g.Football.HasPlayed&&g.Football.InvolvesTeam(team)).ToArray();
        var record=games.Length<4?roster:games.Average(g=>g.Football.WinnerTeamName==team?1:g.Football.WinnerTeamName is null?.5:0);
        var recordWeight=games.Length<4?0:.35/(horizon+1);
        return roster*(1-recordWeight)+record*recordWeight;
    }
    public static double Slot(ProFranchise s,ProDraftPick p)
    {
        if(p.Year==s.Year&&s.Phase==ProPhase.Offseason&&s.OffseasonStep is OffseasonStep.Draft or OffseasonStep.RosterCuts)return p.Slot;
        var ranks=Cache.GetOrCreateValue(s).GetOrAdd(p.Year,y=>new Lazy<Dictionary<string,double>>(()=>Build(s,y))).Value;
        return ranks.GetValueOrDefault(p.OriginalTeam,16.5);
    }
    static Dictionary<string,double> Build(ProFranchise s,int year)
    {
        var ranking=s.League.Teams.OrderBy(t=>Strength(s,t.Name,year)).ThenBy(t=>Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(t.Id+"/"+year)))).ToArray();
        var uncertainty=Math.Clamp(.15+.2*Math.Max(0,year-s.Year-1),.15,.65);
        return ranking.Select((t,i)=>(t.Name,Slot:(i+1)*(1-uncertainty)+16.5*uncertainty)).ToDictionary(x=>x.Name,x=>x.Slot);
    }
}
