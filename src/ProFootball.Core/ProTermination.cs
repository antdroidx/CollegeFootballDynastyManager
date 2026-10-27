namespace ProFootball.Core;

/// <summary>League-year termination timing. Recorded historical assessments are never recalculated.</summary>
public static class ProTermination
{
    public static bool AfterJune1(ProFranchise s)=>s.Phase is ProPhase.Preseason or ProPhase.RegularSeason or ProPhase.Playoffs||s.Phase==ProPhase.Offseason&&s.OffseasonStep==OffseasonStep.Retention;
    public static bool CanDesignate(ProFranchise s,string team,Guid id)=>s.Contracts.Any(c=>c.Active&&c.PlayerId==id&&c.Team==team)&&!AfterJune1(s)&&s.Year!=2030&&s.June1Designations.Count(d=>d.Team==team&&d.Year==s.Year)<2
        &&s.Contracts.Where(c=>c.Active&&c.PlayerId==id&&c.Team==team).All(c=>c.LastChangedYear<s.Year);
    public static Dictionary<int,long> BonusSchedule(ProFranchise s,ProContract c,bool designated=false)
    {
        var current=ProBonusAccounting.PaidCharge(s,c,s.Year);
        var future=Enumerable.Range(s.Year+1,Math.Max(0,c.SignedYear+c.BonusYears-s.Year-1)).Sum(y=>ProBonusAccounting.PaidCharge(s,c,y));
        if(s.Year==2030||!AfterJune1(s)&&!designated)return new(){{s.Year,current+future}};
        return new(){{s.Year,current},{s.Year+1,future}};
    }
    public static List<DeadCapCharge> Charges(ProFranchise s,ProContract c,Guid releaseId,bool designated=false)
    {
        var bonus=BonusSchedule(s,c,designated);var earned=ProContractInsights.EarnedCash(s,c);
        var guaranteedBonus=ProBonusAccounting.UnpaidGuaranteed(s,c);
        var futureGuarantees=c.Years.Where(y=>y.Year>s.Year).Sum(y=>ProBonusAccounting.NonBonusGuarantee(s,c,y));
        var rows=new List<DeadCapCharge>();
        // Future guaranteed salary is a current cap assessment, with its separate actual cash schedule.
        foreach(var item in bonus)
        {
            var amount=item.Value+(item.Key==s.Year?earned+futureGuarantees+guaranteedBonus:0);
            if(amount>0)rows.Add(new(c.Team,item.Key,c.PlayerId,amount){ReleaseId=releaseId});
        }
        foreach(var row in c.Years.Where(y=>y.Year>s.Year))
        {
            var cash=ProBonusAccounting.NonBonusGuarantee(s,c,row)+ProBonusAccounting.UnpaidGuaranteed(s,c,row.Year);
            if(cash>0)rows.Add(new(c.Team,row.Year,c.PlayerId,0){CashAmount=cash,ReleaseId=releaseId});
        }
        if(designated)
        {
            var hold=Math.Max(0,ProEconomy.CapHit(c,s.Year)-(bonus.GetValueOrDefault(s.Year)+earned));
            if(hold>0)rows.Add(new(c.Team,s.Year,c.PlayerId,hold){ReleaseId=releaseId,June1Hold=true});
        }
        return rows;
    }
    public static ProFranchise ProcessJune1(ProFranchise s)=>AfterJune1(s)?s with{DeadCap=s.DeadCap.Where(d=>!d.June1Hold||d.Year!=s.Year).ToList()}:s;
    public static ProFranchise CloseRetained(ProFranchise s,Guid id)
    {
        var pending=s.DeadCap.Where(d=>d.PlayerId==id&&d.RetainedContract&&d.Year>=s.Year).ToArray();
        if(pending.Length==0)return s;
        var debt=s.DeadCap.Where(d=>!pending.Contains(d)).ToList();
        var protectedCash=new Dictionary<(string Team,int Year),long>();
        foreach(var group in pending.GroupBy(d=>(d.Team,d.ReleaseId,d.FundingYear,d.RetainedBonus)))
        {
            var funding=group.Key.FundingYear??s.Year;
            var paid=funding<s.Year||funding==s.Year&&(s.Phase!=ProPhase.Offseason||s.OffseasonStep!=OffseasonStep.FreeAgency||!group.Key.RetainedBonus);
            if(paid&&group.Key.RetainedBonus)
            {
                var current=group.Where(d=>d.Year==s.Year).Sum(d=>d.Amount);var future=group.Where(d=>d.Year>s.Year).Sum(d=>d.Amount);
                if(s.Year==2030||!AfterJune1(s)){current+=future;future=0;}
                if(current>0)debt.Add(new(group.Key.Team,s.Year,id,current){ReleaseId=group.Key.ReleaseId});
                if(future>0)debt.Add(new(group.Key.Team,s.Year+1,id,future){ReleaseId=group.Key.ReleaseId});
                foreach(var row in group.Where(d=>d.CashAmount>0))debt.Add(row with{Amount=0,RetainedContract=false});
            }
            else if(paid)debt.AddRange(group);
            else
            {
                var key=(group.Key.Team,funding);
                if(!protectedCash.TryGetValue(key,out var protection))
                {
                    var original=s.Contracts.FirstOrDefault(c=>c.PlayerId==id&&c.ReleaseId==group.Key.ReleaseId);
                    var buyer=s.Contracts.LastOrDefault(c=>c.PlayerId==id&&c.Active&&c.Team!=group.Key.Team);
                    var row=original?.Years.FirstOrDefault(y=>y.Year==funding);var acquired=buyer?.Years.FirstOrDefault(y=>y.Year==funding);
                    protection=row is null?0:Math.Max(0,ProBonusAccounting.TotalGuarantee(s,original!,row)-(acquired is null?0:ProBonusAccounting.TotalGuarantee(s,buyer!,acquired)));
                }
                var owed=Math.Min(protection,group.Sum(d=>d.CashAmount));protectedCash[key]=protection-owed;
                if(owed>0)
                {
                    debt.Add(new(group.Key.Team,s.Year,id,owed){ReleaseId=group.Key.ReleaseId,CashAmount=funding==s.Year?owed:0});
                    if(funding>s.Year)debt.Add(new(group.Key.Team,funding,id,0){ReleaseId=group.Key.ReleaseId,CashAmount=owed});
                }
            }
            // Only unearned, unprotected conditional compensation is cancelled.

        }
        return s with{DeadCap=debt};
    }

}
