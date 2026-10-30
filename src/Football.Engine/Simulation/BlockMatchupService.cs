using DynastyManager.Core.Models;
namespace DynastyManager.Core.Simulation;
public sealed record BlockingResolution(IReadOnlyList<BlockResult> Blocks,IReadOnlyList<Guid> PressureRushers,Guid? SackBlockerId,Guid? LeadingRusherId);

/// <summary>Resolve actual protection assignments; free rushers have no invented blocker liability.</summary>
public static class BlockMatchupService
{
    public static BlockingResolution Resolve(IReadOnlyList<DynastyPlayer> blockers,IReadOnlyList<DynastyPlayer> rushers,
        double releaseSeconds,bool pass,Random random)
    {
        var pairs=blockers.ToDictionary(p=>p.PlayerId,_=>new List<DynastyPlayer>());
        // Edge/inside alignment: tackles meet ends; interior linemen meet tackles; extra protection helps a threatened lane.
        var available=blockers.ToList();var unblocked=new List<DynastyPlayer>();
        foreach(var rusher in rushers.OrderBy(p=>p.Position is Position.DE?0:1).ThenBy(p=>p.DepthChartOrder))
        {
            var blocker=available.OrderBy(p=>rusher.Position==Position.DE ? p.Position==Position.OL && p.NcaaPositionCode is 5 or 9?0:p.Position==Position.OL?1:2 : p.Position==Position.OL&&p.NcaaPositionCode is 6 or 7 or 8?0:p.Position==Position.OL?1:2)
                .ThenBy(p=>p.DepthChartOrder).FirstOrDefault();
            if(blocker is null){unblocked.Add(rusher);continue;}
            pairs[blocker.PlayerId].Add(rusher);available.Remove(blocker);
        }
        foreach(var helper in available)
        {
            var threat=blockers.Where(p=>pairs[p.PlayerId].Count>0).OrderBy(p=>(p.Skills?.PassBlocking??p.OverallRating)-pairs[p.PlayerId][0].OverallRating).FirstOrDefault();
            if(threat is not null)pairs[helper.PlayerId].Add(pairs[threat.PlayerId][0]);
        }
        var results=new List<BlockResult>();var losses=new List<(Guid Rusher,Guid? Blocker,double Time)>();
        foreach(var blocker in blockers)
        {
            var defender=pairs[blocker.PlayerId].FirstOrDefault();
            if(defender is null){results.Add(new(blocker.PlayerId,null,pass,true,99));continue;}
            var help=pairs.Count(p=>p.Value.Any(d=>d.PlayerId==defender.PlayerId))>1;
            var offense=(pass?blocker.Skills?.PassBlocking:blocker.Skills?.RunBlocking)??blocker.OverallRating;
            var defense=pass?(defender.StrengthRating+defender.AgilityRating+defender.AwarenessRating)/3d:(defender.StrengthRating+(defender.Skills?.Tackling??defender.OverallRating))/2d;
            var chance=pass?Math.Clamp(.085+(defense-offense)*.0025+Math.Max(0,releaseSeconds-2.5)*.065-(help ? .035 : 0),.015,.40)
                :Math.Clamp(.64+(offense-defense)*.008+(help ? .06 : 0),.12,.94);
            var lost=pass?random.NextDouble()<chance:random.NextDouble()>chance;
            var time=lost?1.2+random.NextDouble()*Math.Max(.1,releaseSeconds-1.2):Math.Max(2.5,releaseSeconds)+.5;
            results.Add(new(blocker.PlayerId,defender.PlayerId,pass,!lost,time));
            if(pass&&lost)losses.Add((defender.PlayerId,blocker.PlayerId,time));
        }
        // A double team is beaten only if neither assigned blocker contains that defender.
        losses=losses.Where(l=>!results.Any(b=>b.DefenderId==l.Rusher&&b.Won)).ToList();
        foreach(var free in unblocked)if(releaseSeconds>1.4)losses.Add((free.PlayerId,null,1.4));
        var leading=losses.OrderBy(l=>l.Time).FirstOrDefault();
        return new(results,losses.Select(l=>l.Rusher).Distinct().ToArray(),leading.Blocker,losses.Count>0?leading.Rusher:null);
    }
}
