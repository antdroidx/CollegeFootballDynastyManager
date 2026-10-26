namespace ProFootball.Core;
public sealed record ProOfferTerms(int Years,long Salary,long Bonus)
{
    public const long MoneyStep=250_000;
    public static long RoundUp(long value)=>Math.Max(0,(long)Math.Ceiling(value/(double)MoneyStep)*MoneyStep);
    public static ProOfferTerms AgentDemand(ProFranchise s,ProPlayer player,bool extension=false)
    {
        var years=player.Age>=33?1:player.Age>=30?2:player.Age<=25&&player.Football.PotentialRating>=player.Football.OverallRating+5?4:3;
        if(extension&&s.Contracts.LastOrDefault(c=>c.Active&&c.PlayerId==player.Football.PlayerId) is {} current)
            years=Math.Min(years,Math.Clamp(6-(current.Years.Max(y=>y.Year)-s.Year+1),1,5));
        var peers=s.Players.Where(p=>p.Football.Position==player.Football.Position&&Math.Abs(p.Football.OverallRating-player.Football.OverallRating)<=5).Select(p=>p.Football.PlayerId).ToHashSet();
        var comparables=s.Contracts.Where(c=>c.Active&&!c.Estimated&&c.SignedYear>=s.Year-1&&peers.Contains(c.PlayerId)&&c.Years.Count>0).Select(c=>c.Years.Average(y=>(double)y.Salary)).Order().ToArray();
        var market=Math.Max(ProFinance.Minimum(s,player,practice:false),ProEconomy.MarketSalary(s,player));
        var recent=comparables.Length==0?market:Math.Clamp(comparables[comparables.Length/2],market*.85,market*1.25);
        var competition=Math.Min(5,s.Bids.Where(b=>b.PlayerId==player.Football.PlayerId&&b.Team!=s.UserTeam).Select(b=>b.Team).Distinct().Count());
        var salary=RoundUp((long)(Math.Max(market,recent)*(1+competition*.04)));
        var bonus=RoundUp((long)(salary*years*(player.Age>=30?.10:.15)));
        return new(years,salary,Math.Min(bonus,salary*years));
    }
}
