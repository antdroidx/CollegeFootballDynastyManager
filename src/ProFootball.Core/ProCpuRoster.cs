using DynastyManager.Core.Models;
namespace ProFootball.Core;

/// <summary>Use signed internal depth before buying replacements. Imported restricted reserves stay restricted.</summary>
public static class ProCpuRoster
{
    static bool Healthy(ProFranchise s,ProPlayer p)=>ProReserve.MedicallyAvailable(s,p);
    public static ProFranchise Manage(ProFranchise s,string team,Func<ProPlayer,bool>? eligible=null)
    {
        bool Allowed(ProPlayer p)=>eligible is null||eligible(p);
        if(!ProRoster.IsCamp(s))s=ProCpu.Trim(s,team,eligible);
        var candidates=s.Players.Where(p=>p.Football.TeamName==team&&!p.Retired&&Healthy(s,p)&&Allowed(p)&&(p.PracticeSquad||p.Reserve))
            .OrderByDescending(p=>p.Football.OverallRating).ThenBy(p=>p.Football.PlayerId).Select(p=>p.Football.PlayerId).ToArray();
        foreach(var id in candidates)
        {
            var p=s.Players.Single(p=>p.Football.PlayerId==id);
            var active=s.Players.Where(x=>x.Football.TeamName==team&&!x.Retired&&!x.Reserve&&!x.PracticeSquad).ToArray();
            var same=active.Where(x=>x.Football.Position==p.Football.Position).ToArray();
            var needed=ProCpu.Need(s,team,p.Football.Position)>0||same.Any(x=>!Healthy(s,x));
            var weakest=same.Where(x=>Allowed(x)&&Healthy(s,x)).OrderBy(x=>Healthy(s,x)?x.Football.OverallRating:-1).ThenBy(x=>x.Football.PlayerId).FirstOrDefault();
            if(!needed&&(weakest is null||p.Football.OverallRating<weakest.Football.OverallRating+3))continue;
            var proposal=s;
            if(ProRoster.Active(s,team)>=53)
            {
                if(weakest is null||p.Football.OverallRating<weakest.Football.OverallRating+3)continue;
                // A practice promotion frees its own squad slot. Build the swap immutably and publish only after both operations pass.
                if(p.PracticeSquad)proposal=proposal with{Players=proposal.Players.Select(x=>x.Football.PlayerId==id?x with{PracticeSquad=false}:x).ToList()};
                try{proposal=ProRoster.Practice(proposal,team,weakest.Football.PlayerId,true);}
                catch(InvalidOperationException)
                {
                    if(p.PracticeSquad||!ProCpuBudget.ReleaseKeepsFuturePlan(s,weakest))continue;
                    proposal=ProEconomy.Release(s,team,weakest.Football.PlayerId);
                }
                if(p.PracticeSquad)proposal=proposal with{Players=proposal.Players.Select(x=>x.Football.PlayerId==id?x with{PracticeSquad=true}:x).ToList()};
            }
            try{proposal=p.Reserve?ProReserve.Activate(proposal,team,id):ProRoster.Practice(proposal,team,id,false);}
            catch(InvalidOperationException){continue;}
            s=proposal;
        }
        // Imported/legacy squads may already exceed the game's limits. Do not perpetuate illegal veteran/ordinary counts.
        for(var i=0;i<32;i++)
        {
            var veteranExcess=s.Players.Count(p=>p.Football.TeamName==team&&p.PracticeSquad&&!p.Retired&&p.Experience>2)>6;
            if(!veteranExcess&&ProInternationalPractice.OrdinaryPlayers(s,team)<=16)break;
            var excess=s.Players.Where(p=>p.Football.TeamName==team&&p.PracticeSquad&&!p.Retired&&Allowed(p)
                &&(veteranExcess?p.Experience>2:!ProInternationalPractice.Exempt(s,team,p.Football.PlayerId,s.Year)))
                .OrderBy(p=>p.Football.OverallRating).ThenBy(p=>p.Football.PlayerId).FirstOrDefault();
            if(excess is null)break;
            s=ProEconomy.Release(s,team,excess.Football.PlayerId,false);
        }
        return s;
    }
    public static ProFranchise FillPractice(ProFranchise s,string team,Func<ProPlayer,bool>? eligible=null)
    {
        if(ProRoster.IsCamp(s))return s;
        for(var i=ProInternationalPractice.OrdinaryPlayers(s,team);i<16;i++)
        {
            var candidate=s.Players.Where(p=>p.Football.TeamName=="Free agent"&&!p.Retired&&Healthy(s,p)&&(eligible is null||eligible(p))
                &&p.Football.OverallRating<=65&&ProEconomy.MarketSalary(s,p)<=2_000_000
                &&!s.Contracts.Any(c=>c.Active&&c.PlayerId==p.Football.PlayerId)&&!s.Waivers.Any(w=>w.PlayerId==p.Football.PlayerId)&&!s.Bids.Any(b=>b.PlayerId==p.Football.PlayerId)
                &&(p.Experience<=2||s.Players.Count(x=>x.Football.TeamName==team&&x.PracticeSquad&&x.Experience>2)<6))
                .OrderByDescending(p=>p.Football.OverallRating-8*s.Players.Count(x=>x.Football.TeamName==team&&x.PracticeSquad&&x.Football.Position==p.Football.Position)).ThenBy(p=>p.Football.PlayerId).FirstOrDefault();
            if(candidate is null)break;
            var cost=ProFinance.Minimum(s,candidate,practice:true);
            if(ProEconomy.Room(s,team)-ProEconomy.Pending(s,team)-ProCpuBudget.FutureRolloverNeed(s,team)<cost)break;
            s=ProRoster.SignPractice(s,team,candidate.Football.PlayerId);
        }
        return s;
    }
    public static ProFranchise RetainCut(ProFranchise s,string team,Guid id)
    {
        // Keep signed development depth when a squad slot is available; never discard existing salary/bonus obligations.
        try{return ProRoster.Practice(s,team,id,true);}catch(InvalidOperationException){return ProEconomy.Release(s,team,id,false);}
    }
}
