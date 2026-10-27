namespace ProFootball.Core;
/// <summary>CPU cap planning uses legal restructures before sacrificing core starters.</summary>
public static class ProCpuBudget
{
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
                maximum=Math.Min(maximum,Math.Max(0,ProFinance.Limit(s,team,year)-committed-15_000_000)*count);
            }
            var needed=(long)Math.Ceiling((targetRoom-ProEconomy.Room(s,team))/(1-1d/count));
            var convert=Math.Min(maximum,ProOfferTerms.RoundUp(needed));convert=convert/ProOfferTerms.MoneyStep*ProOfferTerms.MoneyStep;if(convert<=0)continue;
            try{s=ProNegotiation.Restructure(s,team,c.PlayerId,convert);}catch(InvalidOperationException){continue;}
            if(ProEconomy.Room(s,team)>=targetRoom)break;
        }
        return s;
    }
}
