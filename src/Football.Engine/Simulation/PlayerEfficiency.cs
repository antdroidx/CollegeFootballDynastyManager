using DynastyManager.Core.Models;
namespace DynastyManager.Core.Simulation;
public sealed record PlayerEfficiencyMetric(string Name,double Value,double PeerAverage,int Rank,int Players,int Opportunities);
public static class PlayerEfficiency
{
    public static IReadOnlyList<PlayerEfficiencyMetric> Build(IEnumerable<PlayerSeasonStatLine> source,Guid id)
    {
        var groups=source.GroupBy(x=>x.PlayerId).ToArray();var own=groups.FirstOrDefault(g=>g.Key==id);if(own is null)return [];
        var position=own.Last().Position;var peers=groups.Where(g=>g.Last().Position==position).ToArray();
        var metrics=new(string Name,Func<PlayerSeasonStatLine,int> Opportunities,Func<PlayerSeasonStatLine,double> Total,int Minimum,bool Higher)[]{
            ("Adjusted pass yards / attempt",p=>p.PassAttempts,p=>p.PassYards+20d*p.PassingTouchdowns-45d*p.InterceptionsThrown,20,true),
            ("Rush yards / carry",p=>p.RushAttempts,p=>p.RushYards,10,true),
            ("Receiving yards / catch",p=>p.Receptions,p=>p.ReceivingYards,5,true),
            ("YAC / catch",p=>p.Receptions,p=>p.YardsAfterCatch,5,true),
            ("Drops / catchable ball %",p=>p.Receptions+p.Drops,p=>100d*p.Drops,5,false)};
        var result=new List<PlayerEfficiencyMetric>();
        foreach(var m in metrics)
        {
            var n=own.Sum(m.Opportunities);if(n<m.Minimum)continue;
            var eligible=peers.Where(g=>g.Sum(m.Opportunities)>=m.Minimum).ToArray();var value=own.Sum(m.Total)/n;
            var average=eligible.Sum(g=>g.Sum(m.Total))/eligible.Sum(g=>g.Sum(m.Opportunities));
            result.Add(new(m.Name,value,average,1+eligible.Count(g=>m.Higher?g.Sum(m.Total)/g.Sum(m.Opportunities)>value:g.Sum(m.Total)/g.Sum(m.Opportunities)<value),eligible.Length,n));
        }
        return result;
    }
}
