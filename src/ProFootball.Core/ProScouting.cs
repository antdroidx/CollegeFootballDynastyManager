using DynastyManager.Core.Models;
namespace ProFootball.Core;

/// <summary>Scouting depth tiers: every prospect gets a basic grade, 60 deeper reports and 30 visit reports inside those 60.</summary>
public static class ProScouting
{
    public const int PriorityLimit=60;
    public const int VisitLimit=30;

    public static int StaffRating(ProFranchise s,string team,Position position=Position.QB)
    {
        var club=s.League.Teams.Single(t=>t.Name==team);
        var role=position is Position.QB or Position.RB or Position.FB or Position.WR or Position.TE or Position.OL?StaffRole.OffensiveCoordinator:position==Position.K?StaffRole.SpecialTeamsCoordinator:StaffRole.DefensiveCoordinator;
        return (int)Math.Round(s.Coaches.Where(c=>c.TeamId==club.Id&&!c.Vacant&&(c.Role==role||c.Role==StaffRole.HeadCoach)).Select(c=>c.Development).DefaultIfEmpty(65).Average(),MidpointRounding.AwayFromZero);
    }

    public static int Confidence(int staff,ScoutDepth depth,bool combine=false)=>depth switch
    {
        ScoutDepth.Priority=>Math.Clamp(52+(staff-50)/2+(combine?3:0),50,75),
        ScoutDepth.Visit=>Math.Clamp(77+(staff-50)/2+(combine?5:0),75,95),
        _=>Math.Clamp(24+(staff-50)/3+(combine?4:0),12,49)
    };

    public static ScoutReport Evaluate(ProFranchise s,string team,ProPlayer player,ScoutDepth depth,ScoutSource source=ScoutSource.User)
    {
        var staff=StaffRating(s,team,player.Football.Position);
        var combine=s.Phase==ProPhase.Offseason&&s.OffseasonStep==OffseasonStep.Draft;
        var confidence=Confidence(staff,depth,combine);
        var error=Math.Max(2,(100-confidence)/4);
        var random=new Random(BitConverter.ToInt32(player.Football.PlayerId.ToByteArray(),0)^s.Year^(int)depth);
        var grade=Math.Clamp(player.Football.OverallRating+random.Next(-error,error+1),40,99);
        return new(player.Football.PlayerId,team,s.Year,grade,confidence){Depth=depth,Source=source};
    }

    public static ProFranchise Migrate(ProFranchise s)
    {
        if(s.ScoutTargets.Any(t=>t.Year==s.Year))return s;
        if(s.Scouting.Count==0)return s;
        var board=s.DraftBoard.ToHashSet();
        var targets=s.Scouting.Where(r=>r.Year==s.Year).GroupBy(r=>new{r.Team,r.PlayerId}).Select(g=>g.OrderByDescending(r=>(int)r.Depth).First())
            .Select(r=>new ScoutTarget(r.Team,r.PlayerId,r.Year,r.Confidence>=75?ScoutDepth.Visit:ScoutDepth.Priority,
                r.Team==s.UserTeam&&board.Contains(r.PlayerId)?ScoutSource.User:ScoutSource.Cpu))
            .DistinctBy(t=>new{t.Team,t.PlayerId}).OrderBy(t=>t.PlayerId).ToList();
        return targets.Count==0?s:s with{ScoutTargets=targets};
    }

    public static ProFranchise SetTarget(ProFranchise s,string team,Guid id,ScoutDepth? depth)
    {
        var targets=s.ScoutTargets.Where(t=>t.Year!=s.Year||t.Team!=team||t.PlayerId!=id).ToList();
        if(depth is null)return s with{ScoutTargets=targets};
        var currentTargets=targets.Where(t=>t.Year==s.Year&&t.Team==team).ToList();
        var priorityCount=currentTargets.Select(t=>t.PlayerId).Distinct().Count();
        var visitCount=currentTargets.Count(t=>t.Depth==ScoutDepth.Visit);
        var needsRoom=depth is ScoutDepth.Priority&&priorityCount>=PriorityLimit||depth is ScoutDepth.Visit&&(priorityCount>=PriorityLimit||visitCount>=VisitLimit);
        if(needsRoom)
        {
            var staff=StaffRating(s,team);
            var picks=s.Picks.Where(p=>p.Year==s.Year&&p.Owner==team).OrderBy(p=>p.Round).ThenBy(p=>p.Slot).Select(p=>ProDraft.Overall(s,p)).ToArray();
            var prospects=s.Prospects.ToDictionary(p=>p.Football.PlayerId);
            var evict=currentTargets.Where(t=>t.Source==ScoutSource.Cpu&&prospects.ContainsKey(t.PlayerId))
                .OrderBy(t=>Score(s,team,prospects[t.PlayerId],staff,picks)).ThenBy(t=>t.PlayerId).FirstOrDefault();
            if(evict is null)throw depth is ScoutDepth.Visit&&visitCount>=VisitLimit
                ?new InvalidOperationException("Your 30 visit-level reports are full.")
                :new InvalidOperationException("Your 60 prospect priority board is full.");
            targets=targets.Where(t=>!(t.Year==s.Year&&t.Team==team&&t.PlayerId==evict.PlayerId)).ToList();
        }
        return s with{ScoutTargets=targets.Append(new ScoutTarget(team,id,s.Year,depth.Value,ScoutSource.User)).ToList()};
    }
    static ProFranchise EnsureBasic(ProFranchise s)
    {
        var seen=s.Scouting.Where(r=>r.Team==s.UserTeam&&r.Year==s.Year).Select(r=>r.PlayerId).ToHashSet();
        var missing=s.Prospects.Where(p=>!seen.Contains(p.Football.PlayerId)).ToArray();
        return missing.Length==0?s:s with{Scouting=s.Scouting.Concat(missing.Select(p=>Evaluate(s,s.UserTeam,p,ScoutDepth.Basic))).ToList()};
    }

    public static double Score(ProFranchise s,string team,ProPlayer p,int staff,int[] picks)
    {
        var projection=RankedProspect(s,p)+1;
        var need=ProCpu.Need(s,team,p.Football.Position)+1;
        var availability=picks.Length==0?1:picks.Min(pick=>Math.Clamp(2-Math.Abs(projection-pick)/32d,-2,2));
        var upside=Math.Clamp(ProDevelopment.Headroom(p)/40d,0,1);
        var uncertainty=Math.Clamp(Bit(p.Football.PlayerId,5)*2-1,-1,1);
        var staffWeight=(staff-50)/50d;
        return need*(2.5+staffWeight*.7)+availability*(1.8+staffWeight*.6)+upside*(1+staffWeight*1.2)+uncertainty;
    }

    static int RankedProspect(ProFranchise s,ProPlayer p)
    {
        var grade=ProDraft.PublicGrade(p);
        return s.Prospects.Count(o=>ProDraft.PublicGrade(o)>grade||ProDraft.PublicGrade(o)==grade&&o.Football.PlayerId.CompareTo(p.Football.PlayerId)<0);
    }
    static double Bit(Guid id,int index)=>id.ToByteArray()[index]/255d;

    static ProFranchise FillTiers(ProFranchise s,string team)
    {
        var year=s.Year;
        var picks=s.Picks.Where(p=>p.Year==year&&p.Owner==team&&!s.DraftHistory.Any(h=>h.Year==p.Year&&h.Round==p.Round&&h.Slot==p.Slot))
            .OrderBy(p=>p.Round).ThenBy(p=>p.Slot).Select(p=>ProDraft.Overall(s,p)).ToArray();
        var staff=StaffRating(s,team);
        var targets=s.ScoutTargets.Where(t=>t.Team==team&&t.Year==year).ToList();
        var taken=targets.Select(t=>t.PlayerId).ToHashSet();
        var priorityCount=targets.Where(t=>t.Depth!=ScoutDepth.Basic).Select(t=>t.PlayerId).Distinct().Count();
        if(priorityCount<PriorityLimit)
        {
            var fill=s.Prospects.Where(p=>!taken.Contains(p.Football.PlayerId))
                .Select(p=>new{P=p,Rank=Score(s,team,p,staff,picks)})
                .OrderByDescending(x=>x.Rank).ThenBy(x=>x.P.Football.PlayerId).Take(PriorityLimit-priorityCount).ToArray();
            targets=targets.Concat(fill.Select(x=>new ScoutTarget(team,x.P.Football.PlayerId,year,ScoutDepth.Priority,ScoutSource.Cpu))).ToList();
        }
        var visited=targets.Where(t=>t.Depth==ScoutDepth.Visit).Select(t=>t.PlayerId).ToHashSet();
        var visitedCount=visited.Count;
        if(visitedCount<VisitLimit)
        {
            var priority=targets.Where(t=>t.Depth!=ScoutDepth.Basic).Select(t=>t.PlayerId).ToHashSet();
            var fill=s.Prospects.Where(p=>priority.Contains(p.Football.PlayerId)&&!visited.Contains(p.Football.PlayerId))
                .Select(p=>new{P=p,Rank=Score(s,team,p,staff,picks)})
                .OrderByDescending(x=>x.Rank).ThenBy(x=>x.P.Football.PlayerId).Take(VisitLimit-visitedCount).ToArray();
            targets=targets.Concat(fill.Select(x=>new ScoutTarget(team,x.P.Football.PlayerId,year,ScoutDepth.Visit,ScoutSource.Cpu))).ToList();
        }
        return s with{ScoutTargets=s.ScoutTargets.Where(t=>!(t.Team==team&&t.Year==year)).Concat(targets).ToList()};
    }

    static ProFranchise EnsureDeep(ProFranchise s)
    {
        var targets=s.ScoutTargets.Where(t=>t.Year==s.Year).ToList();
        var known=s.Scouting.Where(r=>r.Year==s.Year).Select(r=>new{r.Team,r.PlayerId}).ToHashSet();
        var reports=s.Scouting.ToList();
        foreach(var target in targets)
        {
            if(known.Contains(new{target.Team,target.PlayerId}))continue;
            var p=s.Prospects.FirstOrDefault(x=>x.Football.PlayerId==target.PlayerId);
            if(p is null)continue;
            reports.Add(Evaluate(s,target.Team,p,target.Depth,target.Source));
        }
        return reports.Count==s.Scouting.Count?s:s with{Scouting=reports};
    }
    public static ProFranchise Auto(ProFranchise s)
    {
        if(s.Phase!=ProPhase.Offseason||s.OffseasonStep is not (OffseasonStep.FreeAgency or OffseasonStep.Draft))return s;
        s=Migrate(s);
        s=EnsureBasic(s);
        foreach(var team in s.League.Teams)s=FillTiers(s,team.Name);
        s=EnsureDeep(s);
        return UpdateReports(s);
    }

    public static string DepthSummary(ProFranchise s)
    {
        var year=s.Year;
        var targets=s.ScoutTargets.Where(t=>t.Team==s.UserTeam&&t.Year==year).ToList();
        var priority=targets.Count(t=>t.Depth!=ScoutDepth.Basic);
        var visit=targets.Count(t=>t.Depth==ScoutDepth.Visit);
        var reports=s.Scouting.Count(r=>r.Team==s.UserTeam&&r.Year==year);
        return $"{priority}/{PriorityLimit} priority · {visit}/{VisitLimit} visits · {reports} reports";
    }
    public static ProFranchise UpdateReports(ProFranchise s)
    {
        var prospects=s.Prospects.ToDictionary(p=>p.Football.PlayerId);
        var targets=s.ScoutTargets.Where(t=>t.Year==s.Year).GroupBy(t=>new{t.Team,t.PlayerId}).ToDictionary(g=>g.Key,g=>g.OrderByDescending(t=>t.Depth).First());
        return s with{Scouting=s.Scouting.Select(r=>{
            if(r.Year!=s.Year||!prospects.TryGetValue(r.PlayerId,out var p))return r;
            targets.TryGetValue(new{r.Team,r.PlayerId},out var target);
            var depth=(ScoutDepth)Math.Max((int)r.Depth,target is null?0:(int)target.Depth);
            return Evaluate(s,r.Team,p,depth,target?.Source??r.Source) with{Grade=r.Grade};
        }).ToList()};
    }
}
