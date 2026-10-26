namespace ProFootball.Core;
/// <summary>NFL-specific offseason Paragraph 5 counting; bonuses/other charges remain in the ledger.</summary>
public static class ProCapCounting
{
    public static bool Offseason(ProFranchise s,int year)=>s.CapRulesVersion>=2&&year==s.Year&&
        (s.Phase==ProPhase.Preseason||s.Phase==ProPhase.Offseason&&s.OffseasonStep!=OffseasonStep.Retention);
    public static long ExcludedSalary(ProFranchise s,string team,int year)
    {
        if(!Offseason(s,year))return 0;
        var contracts=s.Contracts.Where(c=>c.Active&&c.Team==team).ToArray();
        var highest=contracts.GroupBy(c=>c.PlayerId).OrderByDescending(g=>g.Sum(c=>ProEconomy.CapHit(c,year))).ThenBy(g=>g.Key).Take(51).Select(g=>g.Key).ToHashSet();
        var players=s.Players.ToDictionary(p=>p.Football.PlayerId);long excluded=0;
        foreach(var c in contracts.Where(c=>!highest.Contains(c.PlayerId)))
        {
            var row=c.Years.FirstOrDefault(y=>y.Year==year);
            if(row is null||row.IsVoid||!players.TryGetValue(c.PlayerId,out var player))continue;
            var rookieFreeAgent=player.RookieYear==year&&player.Experience==0&&player.DraftOverall is not >0;
            var threshold=ProFinance.Minimum(s,player,year,practice:false)*(rookieFreeAgent?1:2);
            // A source credit may already exclude part of base salary (e.g. an exempt contract).
            // Never apply the same relief again or turn that salary into a negative cap charge.
            var remainingSalaryCharge=Math.Max(0,row.Salary+Math.Min(0,row.OtherCompensation));
            excluded+=Math.Min(remainingSalaryCharge,threshold);
        }
        return excluded;
    }
}
