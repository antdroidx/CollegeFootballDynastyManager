namespace ProFootball.Core;
public sealed record ProNotification(string Id,string Title,string Detail,string Section,int Priority,Guid? PlayerId=null);
public static class ProInbox
{
    public static IReadOnlyList<ProNotification> Build(ProFranchise s)
    {
        var list=new List<ProNotification>();var team=s.UserTeam;
        if(ProEconomy.Room(s,team)<0)list.Add(new($"cap-{s.Year}-{team}","Salary cap action required",$"${-ProEconomy.Room(s,team):N0} over the cap. Regular-season entry is blocked.","Front office",0));
        if(ProCapCounting.Offseason(s,s.Year)&&ProEconomy.Room(s,team)>=0&&ProEconomy.Room(s with{Phase=ProPhase.RegularSeason},team)<0)
            list.Add(new($"opening-cap-{s.Year}-{team}","Opening-season cap action needed",$"${-ProEconomy.Room(s with{Phase=ProPhase.RegularSeason},team):N0} over the full-roster budget. Offseason exclusions end before regular-season entry.","Front office",1));
        if(s.UnsignedRookies.Any(r=>r.Team==team))list.Add(new($"rookies-{s.Year}-{team}","Unsigned draft picks","Clear cap/roster room, then sign your rookie class from Overview.","Overview",0));
        foreach(var p in s.Players.Where(p=>p.Football.TeamName==team&&!p.Retired&&ProCareer.Controls(s,p.Football.Position)))
        {
            var id=p.Football.PlayerId;
            if(p.TradeRequested)list.Add(new($"trade-{s.Year}-{id}",p.Football.FullName+" requests a trade","Sustained dissatisfaction has led to a trade request.","Front office",0,id));
            else if(p.ContractConcernWeeks>0)list.Add(new($"contract-{s.Year}-{id}",p.Football.FullName+" wants a new contract","The player is unhappy with contract value. Review the deal and morale.","Front office",1,id));
            if(p.Football.CurrentInjury is {WeeksRemaining:>0} injury)list.Add(new($"injury-{s.Year}-{id}-{injury.BodyArea}",p.Football.FullName+" unavailable",$"{injury.BodyArea} · {injury.WeeksRemaining} weeks remaining","Roster",1,id));
        }
        if(s.Phase==ProPhase.Offseason&&s.OffseasonStep==OffseasonStep.Retention)
        {
            var count=ProRetention.Expiring(s,team).Count;
            if(count>0)list.Add(new($"retention-{s.Year}-{team}","Expiring contracts",$"{count} players need a retention decision before offseason advancement.","Front office",1));
        }
        foreach(var d in s.GmDecisions.TakeLast(30))list.Add(new("gm-"+d.Id,(d.Approved?"GM approved: ":"GM declined: ")+d.Action,d.Reason,"Career",2));
        if(s.JobOffers.Count>0)list.Add(new($"jobs-{s.Year}-{s.JobOffers.Count}","Coaching job offers",$"{s.JobOffers.Count} offers are available.","Career",1));
        return list.OrderBy(n=>n.Priority).ThenBy(n=>n.Title).ToArray();
    }
}
