namespace ProFootball.Core;
/// <summary>CPU cap planning uses legal restructures before sacrificing core starters.</summary>
public static class ProCpuBudget
{
    public static long DepthReserve(ProFranchise s,string team,int year)
    {
        var contracted=s.Contracts.Where(c=>c.Active&&c.Team==team&&ProContractInsights.PlayingEnd(c)>=year).Select(c=>c.PlayerId).ToHashSet();
        var count=s.Players.Count(p=>p.Football.TeamName==team&&!p.Retired&&!p.PracticeSquad&&!p.Reserve&&contracted.Contains(p.Football.PlayerId));
        return (long)(Math.Max(0,53-count)*1_500_000*Math.Max(1,ProFinance.ForecastCap(s,year)/(double)ProFinance.Base2026));
    }
    public static long FutureRolloverNeed(ProFranchise s,string team)
    {
        // Planning only: this does not book cap charges or assume an unearned rollover election.
        var full=s with{Phase=ProPhase.RegularSeason};long needed=0,cumulative=0;
        foreach(var year in Enumerable.Range(s.Year+1,4))
        {
            var cost=ProFinance.Budget(full,team,year).TotalCommitments+ProEconomy.PendingForYear(s,team,year)+DepthReserve(s,team,year);
            // Carryover is consumed by a deficit; do not reuse it or assume unearned future surpluses.
            cumulative+=Math.Max(0,cost-ProFinance.Limit(s,team,year));
            needed=Math.Max(needed,cumulative);
        }
        return Math.Max(0,needed);
    }
    public static bool ReleaseKeepsFuturePlan(ProFranchise s,ProPlayer player,long plannedSpend=0)
    {
        var rows=ProContractInsights.Release(s,player.Football.PlayerId);
        if(!rows.Any(r=>r.Year>s.Year&&r.DeadMoney>r.Before))return true;
        var before=FutureRolloverNeed(s,player.Football.TeamName)-Math.Max(0,ProEconomy.Room(s,player.Football.TeamName)-plannedSpend);
        var released=ProEconomy.Release(s,player.Football.TeamName,player.Football.PlayerId,false);
        var after=FutureRolloverNeed(released,player.Football.TeamName)-Math.Max(0,ProEconomy.Room(released,player.Football.TeamName)-plannedSpend);
        return after<=Math.Max(0,before);
    }
    public static ProFranchise PlanRollover(ProFranchise s,string team)
    {
        // Bank the known multi-year shortfall before spending on depth. Never cancel or shift already booked debt.
        for(var attempt=0;attempt<8&&ProRoster.Active(s,team)>45;attempt++)
        {
            var needed=FutureRolloverNeed(s,team);var room=ProEconomy.Room(s,team);if(needed<=room)break;
            ProFranchise? best=null;double bestScore=0;
            foreach(var player in s.Players.Where(p=>p.Football.TeamName==team&&!p.Retired&&!p.PracticeSquad&&!p.Reserve&&ProCpu.Need(s,team,p.Football.Position)==0))
            {
                var candidate=ProEconomy.Release(s,team,player.Football.PlayerId,false);var cashGain=ProEconomy.Room(candidate,team)-room;if(cashGain<=0)continue;
                var improvement=cashGain+needed-FutureRolloverNeed(candidate,team);
                var score=improvement/Math.Max(1,player.Football.OverallRating-45);
                if(score>bestScore){best=candidate;bestScore=score;}
            }
            if(best is null)break;s=best;
        }
        return s;
    }
    public static ProFranchise Relieve(ProFranchise s,string team,long targetRoom,Func<ProPlayer,bool>? eligible=null)
    {
        if(ProFinance.ContractChangeYear(s)!=s.Year||ProEconomy.Room(s,team)>=targetRoom)return s;
        var players=s.Players.Where(p=>p.Football.TeamName==team&&!p.Retired&&(eligible is null||eligible(p))).ToDictionary(p=>p.Football.PlayerId);
        var candidates=s.Contracts.Where(c=>c.Active&&c.Team==team&&c.LastChangedYear!=s.Year&&players.TryGetValue(c.PlayerId,out var p)&&!ProContractInsights.RookieDeal(s,p,c))
            .OrderByDescending(c=>players[c.PlayerId].Football.OverallRating).ThenBy(c=>c.PlayerId).ToArray();
        foreach(var c in candidates)
        {
            var last=Math.Min(s.Year+4,c.Years.Max(y=>y.Year));var count=last-s.Year+1;var row=c.Years.FirstOrDefault(y=>y.Year==s.Year);
            if(count<2||row is null||Enumerable.Range(s.Year,count).Any(year=>!c.Years.Any(y=>y.Year==year)))continue;
            var maximum=row.Salary-ProFinance.Minimum(s,players[c.PlayerId],practice:false);
            // Avoid spending tomorrow's minimum roster budget to repair today's cap.
            foreach(var year in Enumerable.Range(s.Year+1,count-1))
            {
                var committed=s.Contracts.Where(x=>x.Team==team).Sum(x=>ProEconomy.CapHit(x,year))+s.DeadCap.Where(d=>d.Team==team&&d.Year==year).Sum(d=>d.Amount);
                maximum=Math.Min(maximum,Math.Max(0,ProFinance.Limit(s,team,year)-committed-Math.Max(15_000_000,DepthReserve(s,team,year))-ProEconomy.PendingForYear(s,team,year))*count);
            }
            var needed=(long)Math.Ceiling((targetRoom-ProEconomy.Room(s,team))/(1-1d/count));
            var convert=Math.Min(maximum,ProOfferTerms.RoundUp(needed));convert=convert/ProOfferTerms.MoneyStep*ProOfferTerms.MoneyStep;if(convert<=0)continue;
            try{s=ProNegotiation.Restructure(s,team,c.PlayerId,convert);}catch(InvalidOperationException){continue;}
            if(ProEconomy.Room(s,team)>=targetRoom)break;
        }
        return s;
    }
}
