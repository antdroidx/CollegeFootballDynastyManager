namespace ProFootball.Core;
public static class ProRetention
{
    public static IReadOnlyList<ProPlayer> Expiring(ProFranchise s,string team) => s.Players.Where(p=>!p.Retired&&p.Football.TeamName==team&&s.Contracts.Any(c=>c.Active&&c.PlayerId==p.Football.PlayerId&&c.Team==team&&c.Years.Max(y=>y.Year)<=s.Year)).OrderByDescending(p=>p.Football.OverallRating).ToArray();
    public static ProFranchise Negotiate(ProFranchise s,string team)
    {
        if(s.Phase!=ProPhase.Offseason||s.OffseasonStep!=OffseasonStep.Retention)throw new InvalidOperationException("Retention negotiations open at the end of the season.");
        foreach(var p in Expiring(s,team).Where(p=>!s.LetExpire.Contains(p.Football.PlayerId)&&(team!=s.UserTeam||ProCareer.Controls(s,p.Football.Position))))
        {
            var terms=ProOfferTerms.AgentDemand(s,p,true);var c=s.Contracts.Last(c=>c.Active&&c.PlayerId==p.Football.PlayerId);
            if(c.LastChangedYear==s.Year)continue;
            var first=c.Years.Max(y=>y.Year)+1;
            if(Enumerable.Range(first,terms.Years).Any(year=>s.Contracts.Where(c=>c.Team==team).Sum(c=>ProEconomy.CapHit(c,year))+s.DeadCap.Where(d=>d.Team==team&&d.Year==year).Sum(d=>d.Amount)+terms.Salary+terms.Bonus/terms.Years>ProFinance.Limit(s,team,year)))continue;
            try{
                s=team==s.UserTeam?ProManagement.Submit(s,"Staff retention negotiation",[p.Football.PlayerId],0,x=>ProEconomy.Extend(x,team,p.Football.PlayerId,terms.Years,terms.Salary,terms.Bonus)):ProEconomy.Extend(s,team,p.Football.PlayerId,terms.Years,terms.Salary,terms.Bonus);
            }catch(InvalidOperationException){ /* Unresolved players remain visible until expiration. */ }
        }
        return s;
    }
}
