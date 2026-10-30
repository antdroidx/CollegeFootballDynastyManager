namespace ProFootball.Core;
public static class ProFreeAgencyAssistance
{
    /// <summary>Propose affordable bids for the user unit only; never cut or trade players.</summary>
    public static ProFranchise Propose(ProFranchise s)
    {
        if(s.Career?.Unemployed==true||s.Phase==ProPhase.Playoffs||ProEconomy.Room(s,s.UserTeam)-ProEconomy.Pending(s,s.UserTeam)<750_000)return s;
        foreach(var player in s.Players.Where(p=>!p.Retired&&p.Football.TeamName=="Free agent"&&ProReserve.MedicallyAvailable(s,p)&&ProCareer.Controls(s,p.Football.Position)&&!s.Waivers.Any(w=>w.PlayerId==p.Football.PlayerId)&&!s.Bids.Any(b=>b.Team==s.UserTeam&&b.PlayerId==p.Football.PlayerId)).OrderByDescending(p=>ProCpu.Need(s,s.UserTeam,p.Football.Position)*20+p.Football.OverallRating).ToArray())
        {
            if(s.Bids.Count(b=>b.Team==s.UserTeam)>=3||ProRoster.CountForLimit(s,s.UserTeam)+s.Bids.Count(b=>b.Team==s.UserTeam)>=53)break;
            if(ProCpu.Need(s,s.UserTeam,player.Football.Position)<=0)continue;
            var terms=ProOfferTerms.AgentDemand(s,player);
            try{s=ProManagement.Submit(s,"Assisted free-agent offer",[player.Football.PlayerId],terms.Salary+terms.Bonus/terms.Years,x=>ProEconomy.Offer(x,x.UserTeam,player.Football.PlayerId,terms.Years,terms.Salary,terms.Bonus));}catch(InvalidOperationException){}
        }
        return s;
    }
}
