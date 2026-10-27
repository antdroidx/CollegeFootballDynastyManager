namespace ProFootball.Core;
public sealed record ContractSeasonView(int Year,long Salary,long Bonus,long Other,long Guaranteed,long CapHit);
public sealed record ReleaseSeasonView(int Year,long Before,long DeadMoney,long Savings);
public sealed record ContractComparable(Guid PlayerId,string Name,string Team,int Overall,long AverageCash,long CurrentCap);
public static class ProContractInsights
{
    public static bool RookieDeal(ProFranchise s,ProPlayer p,ProContract c)
    {
        var rookie=p.RookieYear??s.DraftHistory.FirstOrDefault(d=>d.Player.Football.PlayerId==p.Football.PlayerId)?.Year;
        if(rookie is int r && c.OriginalRookieYear==r && s.Year<=r+4 && PlayingEnd(c)==r+4)return true;
        if(rookie is int year)return s.Year<year+(p.DraftOverall is >0||s.DraftHistory.Any(d=>d.Player.Football.PlayerId==p.Football.PlayerId)?4:3);
        return p.Experience<4&&c.SignedYear>=s.Year-p.Experience;
    }
    public static bool ConcernEligible(ProFranchise s,ProPlayer p,ProContract c)=>!RookieDeal(s,p,c)&&s.Year-Math.Max(c.SignedYear,c.LastChangedYear)>=2;
    public static int PlayingEnd(ProContract c)=>c.Years.Where(y=>!y.IsVoid).Select(y=>y.Year).DefaultIfEmpty(c.SignedYear-1).Max();
    public static ProContract NormalizeVoidCharges(ProContract c)
    {
        var voidRows=c.Years.Where(y=>y.IsVoid).OrderBy(y=>y.Year).ToArray();
        if(voidRows.Length==0)return c;
        var first=voidRows[0].Year;var acceleration=voidRows.Sum(y=>ProEconomy.BonusCharge(c,y.Year));
        return c with{Years=c.Years.Select(y=>y.IsVoid?y with{Salary=0,Guaranteed=0,OtherCash=0,OtherCompensation=(y.Year==first?acceleration:0)-ProEconomy.BonusCharge(c,y.Year)}:y).ToList()};
    }
    public static long AnnualCash(ProContract c,ContractYear y)=>y.IsVoid?0:y.Salary+(y.OtherCash??Math.Max(0,y.OtherCompensation))+(c.BonusCash?.GetValueOrDefault(y.Year)??(y.Year==c.SignedYear?c.SigningBonus:0));
    public static long AverageCash(ProContract c)
    {
        var rows=c.Years.Where(y=>!y.IsVoid).ToArray();
        return rows.Length==0?0:(long)rows.Average(y=>(double)AnnualCash(c,y));
    }
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
        var first=old is null?s.Year:PlayingEnd(old)+1;
        var rows=(old?.Years.Where(y=>!y.IsVoid).ToList()??[]).Concat(Enumerable.Range(0,years).Select(i=>new ContractYear(first+i,Math.Max(annual,ProFinance.Minimum(s,p,first+i,practice:false)),i==0?annual/(old is null?3:4):0))).Concat(old?.Years.Where(y=>y.IsVoid&&y.Year>=first+years)??[]).ToList();
        var changeYear=extension?ProFinance.ContractChangeYear(s):s.Year;
        var count=old is null?years:Math.Min(5,rows.Max(y=>y.Year)-changeYear+1);
        return NormalizeVoidCharges(new(p.Football.PlayerId,old?.Team??s.UserTeam,old?.SignedYear??s.Year,Math.Max(old?.BonusYears??0,changeYear+count-(old?.SignedYear??s.Year)),(old?.SigningBonus??0)+bonus,
            rows.Select(y=>y with{ProratedBonus=(old is null?0:ProEconomy.BonusCharge(old,y.Year))+(y.Year>=changeYear&&y.Year<changeYear+count?bonus/count+(y.Year-changeYear<bonus%count?1:0):0)}).ToList()){Estimated=false,AccountingVersion=1,BonusCash=old is null?new Dictionary<int,long>{{s.Year,bonus}}:ProEconomy.AddBonusCash(old,changeYear,bonus)});
    }
    public static long EarnedCash(ProFranchise s,ProContract c)
    {
        var row=c.Years.FirstOrDefault(y=>y.Year==s.Year);if(row is null)return 0;
        return Math.Max(row.Guaranteed,(long)(row.Salary*ProFinance.EarnedFraction(s)))+(row.OtherCash??Math.Max(0,row.OtherCompensation));
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
