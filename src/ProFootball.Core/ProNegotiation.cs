namespace ProFootball.Core;
public sealed record ContractResponse(bool Accepted,long CounterSalary,string Message);
public static class ProNegotiation
{
    public static ContractResponse Evaluate(ProPlayer player,int years,long annual,long bonus)
    {
        if(years is <1 or >5||annual<300_000||bonus<0||bonus>annual*years) return new(false,Math.Max(1_000_000,ProEconomy.MarketSalary(player)),"Invalid terms: 1–5 years, salary at least $300,000, bonus no greater than total base salary.");
        var market=Math.Max(1_000_000,ProEconomy.MarketSalary(player));
        var threshold=.88+(player.Football.PlayerId.ToByteArray()[0]%9)/100d+(player.Age>=31&&years>2?.03:0);
        var effective=annual+bonus/(double)years*.65;
        var counter=(long)Math.Ceiling(Math.Max(300_000,market*threshold-bonus/(double)years*.65)/1000)*1000;
        return effective>=market*threshold ? new(true,counter,"The player considers these terms acceptable. Competing offers and roster/cap approval still apply.")
            : new(false,counter,effective<market*.65?"The player rejects this low offer. Increase salary or guaranteed bonus.":"The player counters with a higher annual salary for these years and bonus.");
    }
    public static ProFranchise Restructure(ProFranchise s,string team,Guid id,long convert)
    {
        var c=s.Contracts.LastOrDefault(c=>c.Active&&c.PlayerId==id&&c.Team==team)??throw new InvalidOperationException("No active contract.");
        if(c.LastChangedYear==s.Year)throw new InvalidOperationException("This player already completed a contract change this season.");
        var current=c.Years.SingleOrDefault(y=>y.Year==s.Year)??throw new InvalidOperationException("No current salary to restructure.");
        var last=Math.Min(s.Year+4,c.Years.Max(y=>y.Year));var count=last-s.Year+1;
        if(count<2||convert<=0||convert>current.Salary-1_000_000)throw new InvalidOperationException("Keep at least $1,000,000 base salary and at least two contract years. Enter a valid conversion amount.");
        if(Enumerable.Range(s.Year,count).Any(year=>!c.Years.Any(y=>y.Year==year)))throw new InvalidOperationException("Contract has missing annual accounting; reconcile it before restructuring.");
        var changed=c with {LastChangedYear=s.Year,SigningBonus=c.SigningBonus+convert,BonusYears=Math.Max(c.BonusYears,last-c.SignedYear+1),Estimated=false,
            Years=c.Years.Select(y=>y with {Salary=y.Year==s.Year?y.Salary-convert:y.Salary,Guaranteed=y.Year==s.Year?Math.Max(0,y.Guaranteed-convert):y.Guaranteed,
                ProratedBonus=ProEconomy.BonusCharge(c,y.Year)+(y.Year>=s.Year&&y.Year<=last?convert/count+(y.Year-s.Year<convert%count?1:0):0)}).ToList()};
        return ProEconomy.Log(s with{Contracts=s.Contracts.Select(x=>x==c?changed:x).ToList()},$"{team} restructured {s.Players.Single(p=>p.Football.PlayerId==id).Football.FullName}: converted ${convert:N0} salary to bonus, moving cap charges to future years.");
    }
}
