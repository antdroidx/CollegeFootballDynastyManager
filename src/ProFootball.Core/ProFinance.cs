namespace ProFootball.Core;
public sealed record ProFinanceYear(string Team,int Year,long BaseCap,long CarryIn,long CapSpent,long CashSpent,long CarryOut,long FloorPayment) {public long OtherAdjustments {get;init;} public long AdjustedCap=>BaseCap+CarryIn+OtherAdjustments;}
public static class ProFinance
{
    public const long Base2026=301_200_000;
    static (long Base,long Rollover,long Adjustments) Allowance(ProFranchise s,string team,int year)
    {
        var allocation=ProCapInitialization.Allocation(s,team,year);
        return (allocation?.BaseCap??ForecastCap(s,year),allocation?.Rollover??s.FinancialHistory.LastOrDefault(h=>h.Team==team&&h.Year==year-1)?.CarryOut??s.RolloverElections.LastOrDefault(e=>e.Team==team&&e.Year==year-1)?.Amount??0,allocation?.OtherAdjustments??0);
    }
    public static long Limit(ProFranchise s,string team,int? year=null){var a=Allowance(s,team,year??s.Year);return checked(a.Base+a.Rollover+a.Adjustments);}
    public static ProCapBudget Budget(ProFranchise s,string team,int? year=null)
    {
        var y=year??s.Year;var a=Allowance(s,team,y);var rows=s.Contracts.Where(c=>c.Active&&c.Team==team).ToArray();
        var excluded=ProCapCounting.ExcludedSalary(s,team,y);
        return new(a.Base,a.Rollover,a.Adjustments,checked(a.Base+a.Rollover+a.Adjustments),rows.Sum(c=>c.Years.FirstOrDefault(r=>r.Year==y)?.Salary??0)-excluded,rows.Sum(c=>ProEconomy.BonusCharge(c,y)),rows.Sum(c=>c.Years.FirstOrDefault(r=>r.Year==y)?.OtherCompensation??0),s.DeadCap.Where(d=>d.Team==team&&d.Year==y).Sum(d=>d.Amount),s.RolloverElections.LastOrDefault(e=>e.Team==team&&e.Year==y)?.Amount??0){OffseasonSalaryExcluded=excluded};
    }
    public static int ContractChangeYear(ProFranchise s)=>s.Phase==ProPhase.Offseason&&s.OffseasonStep==OffseasonStep.Retention?s.Year+1:s.Year;
    public static double Growth(ProFranchise s,int year)
    {
        var seed=System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"revenue/{s.Id}/{year}"));
        var cycle=Math.Sin((year-2026)*.7)*.01;var variation=(seed[0]/255d-.5)*.04;var shock=seed[1]<10?-.04:0;
        return Math.Clamp(.05+cycle+variation+shock,-.02,.10);
    }
    public static long ForecastCap(ProFranchise s,int year)
    {
        if(year<=s.Year)return year==s.Year?s.SalaryCap:s.FinancialHistory.FirstOrDefault(h=>h.Year==year)?.BaseCap??s.SalaryCap;
        var cap=s.SalaryCap;for(var y=s.Year+1;y<=year;y++)cap=(long)Math.Round(cap*(1+Growth(s,y))/100_000,MidpointRounding.AwayFromZero)*100_000;return cap;
    }
    public static long Minimum(ProFranchise s,ProPlayer p,int? year=null,bool? practice=null)
    {
        var y=year??s.Year;var exp=Math.Max(0,(p.CreditedSeasons??p.Experience)+y-s.Year);
        if(practice??p.PracticeSquad)
        {
            var weekly=exp<=2?13_750L+Math.Clamp(y-2026,0,4)*750:18_350L+Math.Clamp(y-2026,0,4)*850;
            if(y>2030)weekly=(long)Math.Ceiling(weekly*Math.Pow(1.04,y-2030));return weekly*18;
        }
        var baseline=exp switch{0=>885_000L,1=>1_005_000L,2=>1_075_000L,3=>1_145_000L,<=6=>1_215_000L,_=>1_300_000L};
        var salary=baseline+Math.Clamp(y-2026,-1,4)*45_000;
        if(y>2030)salary=(long)Math.Ceiling(salary*Math.Pow(1.04,y-2030)/5_000)*5_000;return salary;
    }
    public static double EarnedFraction(ProFranchise s)
    {
        if(s.Phase==ProPhase.Playoffs||s.Phase==ProPhase.Offseason&&s.OffseasonStep==OffseasonStep.Retention&&s.Games.Any(g=>g.Phase==ProPhase.RegularSeason&&g.Football.HasPlayed))return 1;
        return s.Phase==ProPhase.RegularSeason?Math.Clamp((s.Week-1)/18d,0,1):0;
    }
    public static long Cash(ProFranchise s,string team,int year)
    {
        long cash=0;
        foreach(var c in s.Contracts.Where(c=>c.Team==team))
        {
            if(c.TerminatedYear is int stopped&&year>stopped)continue;
            cash+=c.BonusCash is {} bonuses?bonuses.GetValueOrDefault(year):c.SignedYear==year?c.SigningBonus:0;
            var row=c.Years.FirstOrDefault(y=>y.Year==year);if(row is null)continue;
            if(c.TerminatedYear==year)cash+=c.EarnedCash;
            else if(c.TerminatedYear is int ended&&year>ended)continue;
            else if(c.Active||c.TerminatedYear is int)cash+=row.IsVoid?0:row.Salary+(row.OtherCash??Math.Max(0,row.OtherCompensation));
        }
        return cash+s.DeadCap.Where(d=>d.Team==team&&d.Year==year).Sum(d=>d.CashAmount);
    }
    public static (int Start,int End) FloorWindow(int year)=>year<=2026?(Math.Max(2024,year-2),2026):year<=2030?(2027,2030):(2031+(year-2031)/4*4,2034+(year-2031)/4*4);
    public static (long Paid,long Required,int End) Floor(ProFranchise s,string team)
    {
        var window=FloorWindow(s.Year);var past=s.FinancialHistory.Where(h=>h.Team==team&&h.Year>=window.Start&&h.Year<s.Year).ToArray();
        var caps=past.Sum(h=>h.BaseCap)+s.SalaryCap;var paid=past.Sum(h=>h.CashSpent+h.FloorPayment)+Cash(s,team,s.Year);
        return(paid,(long)Math.Ceiling(caps*.90),window.End);
    }
    public static ProFranchise CloseYear(ProFranchise s)
    {
        if(s.FinancialHistory.Any(h=>h.Year==s.Year))return s;
        s=ProRookieOptions.CaptureYear(s);
        s=ProRollover.MeasureCompletedTeams(s);
        var history=s.FinancialHistory.ToList();
        foreach(var team in s.League.Teams)
        {
            var room=Math.Max(0,ProEconomy.Room(s,team.Name));var cash=Cash(s,team.Name,s.Year);var floor=Floor(s,team.Name);var payment=s.Year==floor.End?Math.Max(0,floor.Required-floor.Paid):0;
            var budget=Budget(s,team.Name);var election=s.RolloverElections.LastOrDefault(e=>e.Team==team.Name&&e.Year==s.Year);
            var carry=election?.Amount??(long)(room*(team.Name==s.UserTeam?s.CarryoverPercentage:100)/100m);
            history.Add(new(team.Name,s.Year,budget.BaseCap,budget.Rollover,budget.TotalCommitments-budget.RolloverReserved,cash,carry,payment){OtherAdjustments=budget.OtherAdjustments});
            if(payment>0)s=ProEconomy.Log(s,$"{team.Name} paid ${payment:N0} to satisfy the league cash-spending floor.");
        }
        return s with{FinancialHistory=history};
    }
    public static ProFranchise CpuBonuses(ProFranchise s,string? onlyTeam=null)
    {
        foreach(var team in s.League.Teams.Where(t=>(onlyTeam is null||t.Name==onlyTeam)&&(t.Name!=s.UserTeam||s.Career?.Unemployed==true)))
        {
            var room=Math.Max(0,ProEconomy.Room(s,team.Name));var invest=Math.Max(0,(long)(Limit(s,team.Name)*.90)-ProEconomy.Used(s,team.Name));invest=Math.Min(invest,room);if(invest==0)continue;
            var ids=s.Players.Where(p=>p.Football.TeamName==team.Name&&!p.Retired&&!p.PracticeSquad&&!p.Reserve).Select(p=>p.Football.PlayerId).ToHashSet();
            var candidates=s.Contracts.Where(c=>c.Active&&ids.Contains(c.PlayerId)&&c.Years.Any(y=>y.Year==s.Year)).ToArray();if(candidates.Length==0)continue;
            var share=invest/candidates.Length;var remainder=invest%candidates.Length;var bonus=candidates.Select((c,i)=>(c.PlayerId,Amount:share+(i<remainder?1:0))).ToDictionary(x=>x.PlayerId,x=>x.Amount);
            s=s with{Contracts=s.Contracts.Select(c=>c.Active&&bonus.TryGetValue(c.PlayerId,out var amount)?c with{Years=c.Years.Select(y=>y.Year==s.Year?y with{OtherCompensation=y.OtherCompensation+amount,OtherCash=y.OtherCash is long cash?cash+amount:null}:y).ToList()}:c).ToList()};
        }
        return s;
    }
}
