namespace ProFootball.Core;
public static class ProRetention
{
    public static IReadOnlyList<ProPlayer> Expiring(ProFranchise s,string team) => s.Players.Where(p=>!p.Retired&&p.Football.TeamName==team&&s.Contracts.Any(c=>c.Active&&c.PlayerId==p.Football.PlayerId&&c.Team==team&&ProContractInsights.PlayingEnd(c)<=s.Year)).OrderByDescending(p=>p.Football.OverallRating).ToArray();
    public static ProFranchise Negotiate(ProFranchise s,string team)
    {
        if(s.Phase!=ProPhase.Offseason||s.OffseasonStep!=OffseasonStep.Retention)throw new InvalidOperationException("Retention negotiations open at the end of the season.");
        foreach(var p in Expiring(s,team).Where(p=>!s.LetExpire.Contains(p.Football.PlayerId)&&(team!=s.UserTeam||ProCareer.Controls(s,p.Football.Position))))
        {
            var terms=ProOfferTerms.AgentDemand(s,p,true);var c=s.Contracts.Last(c=>c.Active&&c.PlayerId==p.Football.PlayerId);
            if(c.LastChangedYear>=s.Year)continue;
            var first=ProContractInsights.PlayingEnd(c)+1;
            var proposal=ProContractInsights.ProjectOffer(s,p,terms.Years,terms.Salary,terms.Bonus,true);
            if(Enumerable.Range(first,terms.Years).Any(year=>{
                var before=ProFinance.Budget(s,team,year).TotalCommitments;
                var futureIds=s.Contracts.Where(deal=>deal.Active&&deal.Team==team&&deal.PlayerId!=p.Football.PlayerId&&ProContractInsights.PlayingEnd(deal)>=year).Select(deal=>deal.PlayerId).ToHashSet();
                if(ProContractInsights.PlayingEnd(proposal)>=year)futureIds.Add(p.Football.PlayerId);
                var futurePlayers=s.Players.Count(x=>!x.Retired&&!x.PracticeSquad&&!x.Reserve&&x.Football.TeamName==team&&futureIds.Contains(x.Football.PlayerId));
                var depthReserve=(long)(Math.Max(0,53-futurePlayers)*1_500_000*(ProFinance.ForecastCap(s,year)/(double)ProFinance.Base2026));
                var planningLimit=ProFinance.Limit(s,team,year)-depthReserve;
                return before-ProEconomy.CapHit(c,year)+ProEconomy.CapHit(proposal,year)>Math.Max(planningLimit,before);
            }))continue;
            try{
                s=team==s.UserTeam?ProManagement.Submit(s,"Staff retention negotiation",[p.Football.PlayerId],0,x=>ProEconomy.Extend(x,team,p.Football.PlayerId,terms.Years,terms.Salary,terms.Bonus)):ProEconomy.Extend(s,team,p.Football.PlayerId,terms.Years,terms.Salary,terms.Bonus);
            }catch(InvalidOperationException){ /* Unresolved players remain visible until expiration. */ }
        }
        return s;
    }
}
