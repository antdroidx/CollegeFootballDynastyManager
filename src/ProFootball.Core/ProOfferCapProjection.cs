namespace ProFootball.Core;
public sealed record ProOfferCapYear(int Year,long Limit,long Before,long CapHit,long After);
public static class ProOfferCapProjection
{
    public static IReadOnlyList<ProOfferCapYear> Build(ProFranchise s,ProPlayer player,ProContract offer,bool extension)
    {
        var old=extension?s.Contracts.LastOrDefault(c=>c.Active&&c.PlayerId==player.Football.PlayerId&&c.Team==s.UserTeam):null;
        var start=s.Year;
        return Enumerable.Range(start,Math.Min(7,Math.Max(2,offer.Years.Max(y=>y.Year)-start+1))).Select(year=>{
            var budget=ProCapDisplay.Budget(s,s.UserTeam,year);var before=budget.Available-ProEconomy.PendingForYear(s,s.UserTeam,year);
            var hit=ProEconomy.CapHit(offer,year);return new ProOfferCapYear(year,budget.AdjustedCap,before,hit,before+(old is null?0:ProEconomy.CapHit(old,year))-hit);
        }).ToArray();
    }
}
