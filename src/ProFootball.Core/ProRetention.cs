using DynastyManager.Core.Models;
namespace ProFootball.Core;
public static class ProRetention
{
    public static IReadOnlyList<ProPlayer> Expiring(ProFranchise s,string team) => s.Players.Where(p=>!p.Retired&&p.Football.TeamName==team&&s.Contracts.Any(c=>c.Active&&c.PlayerId==p.Football.PlayerId&&c.Team==team&&ProContractInsights.PlayingEnd(c)<=s.Year)).OrderByDescending(p=>p.Football.OverallRating).ToArray();
    public static string? ExtensionIssue(ProFranchise s, ProPlayer p)
    {
        var c=s.Contracts.LastOrDefault(c=>c.Active&&c.PlayerId==p.Football.PlayerId&&c.Team==p.Football.TeamName);
        if(c is null||p.Retired)return "No active playing contract.";
        if(c.LastChangedYear>=s.Year)return "Already changed this contract this season.";
        if(ProContractInsights.PlayingEnd(c)+1+1-s.Year>6)return "Too many contract years remain to extend.";
        var rookie=c.OriginalRookieYear??p.RookieYear;
        if(rookie is int r && ProContractInsights.RookieDeal(s,p,c))
        {
            var drafted=p.DraftOverall is >0||s.DraftHistory.Any(d=>d.Player.Football.PlayerId==p.Football.PlayerId);
            var last=r+(drafted?2:1);
            var regularComplete=s.Phase is ProPhase.Playoffs or ProPhase.Offseason || s.Phase==ProPhase.RegularSeason&&s.Week==18&&s.Games.Where(g=>g.Phase==ProPhase.RegularSeason&&g.Football.InvolvesTeam(p.Football.TeamName)).All(g=>g.Football.HasPlayed);
            if(s.Year<last||s.Year==last&&!regularComplete)return $"Rookie deal: negotiations open after the {last} regular season.";
        }
        return null;
    }
    public static ProFranchise Negotiate(ProFranchise s,string team)=>Negotiate(s,team,false);
    public static ProFranchise NegotiateStaffUnit(ProFranchise s)=>s.Career?.Unemployed==true?Negotiate(s,s.UserTeam,false)
        :ProCareer.Role(s)==StaffRole.HeadCoach?s:Negotiate(s,s.UserTeam,true);
    static ProFranchise Negotiate(ProFranchise s,string team,bool staffUnit)
    {
        if(s.Phase==ProPhase.Offseason&&s.OffseasonStep!=OffseasonStep.Retention)throw new InvalidOperationException("Use the retention stage for offseason negotiations.");
        foreach(var p in Expiring(s,team).Where(p=>!s.LetExpire.Contains(p.Football.PlayerId)&&(team!=s.UserTeam||s.Career?.Unemployed==true||(ProCareer.Controls(s,p.Football.Position)!=staffUnit))))
        {
            if(ExtensionIssue(s,p) is not null)continue;
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
                if(team!=s.UserTeam||s.Career?.Unemployed==true)s=ProEconomy.Extend(s,team,p.Football.PlayerId,terms.Years,terms.Salary,terms.Bonus);
                else if(staffUnit)s=ProManagement.SubmitStaff(s,"Staff retention · "+p.Football.FullName,[p.Football.PlayerId],x=>ProEconomy.Extend(x,team,p.Football.PlayerId,terms.Years,terms.Salary,terms.Bonus));
                else s=ProManagement.Submit(s,"Staff retention negotiation",[p.Football.PlayerId],0,x=>ProEconomy.Extend(x,team,p.Football.PlayerId,terms.Years,terms.Salary,terms.Bonus));
            }catch(InvalidOperationException){ /* Unresolved players remain visible until expiration. */ }
        }
        return s;
    }
}
