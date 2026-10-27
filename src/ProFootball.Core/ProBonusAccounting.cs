namespace ProFootball.Core;

/// <summary>Explicit reviewed payment events; legacy contracts retain recorded proration without guessed guarantees.</summary>
public static class ProBonusAccounting
{
    static long Allocated(ProBonusPayment p,int year)
    {
        var offset=year-p.Year;return offset<0||offset>=p.AllocationYears?0:p.Amount/p.AllocationYears+(offset<p.Amount%p.AllocationYears?1:0);
    }
    public static bool Paid(ProFranchise s,ProBonusPayment p)=>p.PaidAtExecution&&p.Year<=s.Year||p.Year<s.Year||p.Year==s.Year&&(s.Phase!=ProPhase.Offseason||s.OffseasonStep!=OffseasonStep.FreeAgency);
    public static long PaidCharge(ProFranchise s,ProContract c,int year)
    {
        var published=ProEconomy.BonusCharge(c,year);if(c.BonusPayments is null)return published;
        var residual=published-c.BonusPayments.Sum(p=>Allocated(p,year));
        if(residual<0)throw new InvalidOperationException("Reviewed bonus allocation exceeds the recorded annual charge.");
        return residual+c.BonusPayments.Where(p=>Paid(s,p)).Sum(p=>Allocated(p,year));
    }
    public static long UnpaidGuaranteed(ProFranchise s,ProContract c,int? year=null)=>c.BonusPayments?.Where(p=>!Paid(s,p)&&p.GuaranteedAtSigning&&(year is null||p.Year==year)).Sum(p=>p.Amount)??0;
    public static long UnpaidConditionalCharge(ProFranchise s,ProContract c,int year)=>c.BonusPayments?.Where(p=>!Paid(s,p)&&!p.GuaranteedAtSigning).Sum(p=>Allocated(p,year))??0;
    public static long NonBonusGuarantee(ProFranchise s,ProContract c,ContractYear row)
    {
        var baseCash=row.Guaranteed+row.GuaranteedOtherCash;
        if(c.BonusPayments is null)return baseCash;
        var recognized=c.BonusPayments.Where(p=>p.Year==row.Year&&(Paid(s,p)||p.GuaranteedAtSigning)).Sum(p=>p.Amount);
        var vested=c.CashVesting?.Where(v=>v.PaymentYear==row.Year&&(s.Year>v.AfterSeason||s.Year==v.AfterSeason&&s.Phase==ProPhase.Offseason&&s.OffseasonStep==OffseasonStep.Retention)).Select(v=>v.GuaranteedCash).DefaultIfEmpty(0).Max()??0;
        return Math.Max(baseCash,Math.Max(0,vested-recognized));
    }
    public static long TotalGuarantee(ProFranchise s,ProContract c,ContractYear row)=>NonBonusGuarantee(s,c,row)+(c.BonusPayments?.Where(p=>p.Year==row.Year&&(Paid(s,p)||p.GuaranteedAtSigning)).Sum(p=>p.Amount)??0);
    public static Dictionary<int,long>? TerminatedCash(ProFranchise s,ProContract c)
    {
        if(c.BonusCash is null)return null;
        return c.BonusCash.ToDictionary(x=>x.Key,x=>x.Value-(x.Key==s.Year?c.BonusPayments?.Where(p=>p.Year==s.Year&&!Paid(s,p)&&!p.GuaranteedAtSigning).Sum(p=>p.Amount)??0:0));
    }

}
