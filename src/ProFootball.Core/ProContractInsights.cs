namespace ProFootball.Core;
public sealed record ContractSeasonView(int Year,long Salary,long Bonus,long Other,long Guaranteed,long CapHit);
public sealed record ReleaseSeasonView(int Year,long Before,long DeadMoney,long Savings);
public sealed record ContractComparable(Guid PlayerId,string Name,string Team,int Overall,long AverageCash,long CurrentCap);
public static class ProContractInsights
{
    public static bool RookieDeal(ProFranchise s,ProPlayer p,ProContract c)
    {
        var rookie=p.RookieYear??s.DraftHistory.FirstOrDefault(d=>d.Player.Football.PlayerId==p.Football.PlayerId)?.Year;
        if(rookie is int year)return s.Year<year+(p.DraftOverall is >0||s.DraftHistory.Any(d=>d.Player.Football.PlayerId==p.Football.PlayerId)?4:3);
        return p.Experience<4&&c.SignedYear>=s.Year-p.Experience;
    }
    public static bool ConcernEligible(ProFranchise s,ProPlayer p,ProContract c)=>!RookieDeal(s,p,c)&&s.Year-Math.Max(c.SignedYear,c.LastChangedYear)>=2;
    public static long AverageCash(ProContract c)=>c.Years.Count==0?0:(long)(c.Years.Average(y=>(double)y.Salary+Math.Max(0,y.OtherCompensation))+c.SigningBonus/(double)c.Years.Count);
    public static string Satisfaction(ProFranchise s,ProPlayer p)
    {
        var c=s.Contracts.LastOrDefault(c=>c.Active&&c.PlayerId==p.Football.PlayerId);if(c is null)return "No contract";
        if(!ConcernEligible(s,p,c))return "Content";
        if(p.ContractConcernWeeks>0)return "Wants a new deal";
        return AverageCash(c)>=ProEconomy.MarketSalary(s,p)?"Happy":"Content";
    }
    public static IReadOnlyList<ContractSeasonView> Seasons(ProContract c)=>c.Years.OrderBy(y=>y.Year).Select(y=>new ContractSeasonView(y.Year,y.Salary,ProEconomy.BonusCharge(c,y.Year),y.OtherCompensation,y.Guaranteed,ProEconomy.CapHit(c,y.Year))).ToArray();
    public static ProContract ProjectOffer(ProFranchise s,ProPlayer p,int years,long annual,long bonus,bool extension)
    {
        var old=extension?s.Contracts.LastOrDefault(c=>c.Active&&c.PlayerId==p.Football.PlayerId):null;
        var first=old is null?s.Year:old.Years.Max(y=>y.Year)+1;
        var rows=(old?.Years??[]).Concat(Enumerable.Range(0,years).Select(i=>new ContractYear(first+i,Math.Max(annual,ProFinance.Minimum(s,p,first+i,practice:false)),i==0?annual/(old is null?3:4):0))).ToList();
        var count=old is null?years:Math.Min(5,rows.Max(y=>y.Year)-s.Year+1);
        return new(p.Football.PlayerId,old?.Team??s.UserTeam,old?.SignedYear??s.Year,Math.Max(old?.BonusYears??0,s.Year+count-(old?.SignedYear??s.Year)),(old?.SigningBonus??0)+bonus,
            rows.Select(y=>y with{ProratedBonus=(old is null?0:ProEconomy.BonusCharge(old,y.Year))+(y.Year>=s.Year&&y.Year<s.Year+count?bonus/count+(y.Year-s.Year<bonus%count?1:0):0)}).ToList()){Estimated=false};
    }
    public static long EarnedCash(ProFranchise s,ProContract c)
    {
        var row=c.Years.FirstOrDefault(y=>y.Year==s.Year);if(row is null)return 0;
        return Math.Max(row.Guaranteed,(long)(row.Salary*ProFinance.EarnedFraction(s)))+Math.Max(0,row.OtherCompensation);
    }
    public static IReadOnlyList<ReleaseSeasonView> Release(ProFranchise s,Guid id)
    {
        var contracts=s.Contracts.Where(c=>c.Active&&c.PlayerId==id).ToArray();
        var last=contracts.SelectMany(c=>c.Years.Select(y=>y.Year).Append(c.SignedYear+c.BonusYears-1)).DefaultIfEmpty(s.Year).Max();
        return Enumerable.Range(s.Year,Math.Max(1,last-s.Year+1)).Select(year=>{
            var before=contracts.Sum(c=>ProEconomy.CapHit(c,year));
            var guarantees=year==s.Year?contracts.Sum(c=>EarnedCash(s,c)):contracts.Sum(c=>c.Years.Where(y=>y.Year==year).Sum(y=>y.Guaranteed));
            var bonus=year==s.Year?contracts.Sum(c=>Enumerable.Range(s.Year,Math.Max(0,c.SignedYear+c.BonusYears-s.Year)).Sum(y=>ProEconomy.BonusCharge(c,y))):0;
            return new ReleaseSeasonView(year,before,guarantees+bonus,before-guarantees-bonus);
        }).ToArray();
    }
    public static IReadOnlyList<ContractComparable> Comparables(ProFranchise s,ProPlayer player)=>s.Players.Where(p=>!p.Retired&&!p.PracticeSquad&&p.Football.PlayerId!=player.Football.PlayerId&&p.Football.Position==player.Football.Position&&Math.Abs(p.Football.OverallRating-player.Football.OverallRating)<=5)
        .Select(p=>(Player:p,Contract:s.Contracts.LastOrDefault(c=>c.Active&&c.PlayerId==p.Football.PlayerId))).Where(x=>x.Contract is not null).OrderBy(x=>Math.Abs(x.Player.Football.OverallRating-player.Football.OverallRating)).ThenBy(x=>x.Player.Football.FullName).Take(8)
        .Select(x=>new ContractComparable(x.Player.Football.PlayerId,x.Player.Football.FullName,x.Player.Football.TeamName,x.Player.Football.OverallRating,AverageCash(x.Contract!),ProEconomy.CapHit(x.Contract!,s.Year))).ToArray();
}
