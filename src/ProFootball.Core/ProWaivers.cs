namespace ProFootball.Core;

/// <summary>Assignment of the recorded Paragraph5 salary, retaining the signing club's other obligations.</summary>
public static class ProWaivers
{
    static ProFranchise Proposal(ProFranchise s,string team,WaiverEntry entry)
    {
        if(entry.ReleaseId is not Guid release||entry.OriginalContracts is not {Count:>0} originals)throw new InvalidOperationException("Original waiver contract is unavailable.");
        if(entry.ReleasedYear!=s.Year)throw new InvalidOperationException("This waiver belongs to a prior league year.");
        if(!s.League.Teams.Any(t=>t.Name==team)||originals.Any(c=>c.Team==team))throw new InvalidOperationException("Choose another valid team to claim this contract.");
        var p=s.Players.Single(p=>p.Football.PlayerId==entry.PlayerId);
        if(p.Retired||p.Football.TeamName!="Free agent")throw new InvalidOperationException("This player is no longer available on waivers.");
        if(ProRoster.CountForLimit(s,team)>=ProRoster.Limit(s))throw new InvalidOperationException("No roster space for a waiver claim.");
        if(originals.Any(c=>!c.Years.Any(y=>y.Year==s.Year&&!y.IsVoid)))throw new InvalidOperationException("No current playing salary is available to assign.");
        var contracts=s.Contracts.ToList();var debt=s.DeadCap.Where(d=>d.ReleaseId!=release).ToList();
        foreach(var c in originals)
        {
            var current=c.Years.Single(y=>y.Year==s.Year);
            var earned=(long)(current.Salary*entry.ReleasedFraction);
            var currentOther=current.OtherCash??Math.Max(0,current.OtherCompensation);
            // Remove only the uniquely tagged release assessment. Unrelated player/team debt is retained.
            var bonus=ProTermination.BonusSchedule(s,c);
            bonus[s.Year]=bonus.GetValueOrDefault(s.Year)+ProBonusAccounting.UnpaidGuaranteed(s,c)+c.Years.Where(y=>y.Year>s.Year).Sum(y=>y.GuaranteedOtherCash);
            foreach(var item in bonus)
                if(item.Value+(item.Key==s.Year?earned+currentOther:0)>0)debt.Add(new(c.Team,item.Key,c.PlayerId,item.Value+(item.Key==s.Year?earned+currentOther:0)){ReleaseId=release});
            foreach(var row in c.Years.Where(y=>y.Year>s.Year&&!y.IsVoid))
            {
                var other=row.OtherCash??Math.Max(0,row.OtherCompensation);var bonusCash=c.BonusCash?.GetValueOrDefault(row.Year)??0;
                if(other!=0||row.OtherCompensation!=0)debt.Add(new(c.Team,row.Year,c.PlayerId,row.OtherCompensation-row.GuaranteedOtherCash){CashAmount=other,ReleaseId=release,RetainedContract=row.GuaranteedOtherCash==0,FundingYear=row.Year});
                if(bonusCash>0&&c.BonusPayments is null)debt.Add(new(c.Team,row.Year,c.PlayerId,0){CashAmount=bonusCash,ReleaseId=release});
            }
            foreach(var payment in c.BonusPayments?.Where(b=>b.Year>s.Year)??[])
            {
                if(payment.GuaranteedAtSigning)debt.Add(new(c.Team,payment.Year,c.PlayerId,0){CashAmount=payment.Amount,ReleaseId=release});
                else for(var i=0;i<payment.AllocationYears;i++)
                {
                    var year=payment.Year+i;
                    if(c.Years.FirstOrDefault(y=>y.Year==year)?.IsVoid==true)year=c.Years.Where(y=>y.IsVoid).Min(y=>y.Year);
                    debt.Add(new(c.Team,year,c.PlayerId,payment.Amount/payment.AllocationYears+(i<payment.Amount%payment.AllocationYears?1:0)){CashAmount=i==0?payment.Amount:0,ReleaseId=release,RetainedContract=true,FundingYear=payment.Year,RetainedBonus=true});
                }
            }
            // Historical cash remains on the former team. The salary guarantee is now assumed by the claimant.
            contracts=contracts.Select(x=>x.PlayerId==c.PlayerId&&x.Team==c.Team&&!x.Active&&x.TerminatedYear==s.Year?x with{EarnedCash=earned+currentOther}:x).ToList();
            contracts.Add(c with{ReleaseId=null,Team=team,SigningBonus=0,BonusCash=[],TerminatedYear=null,EarnedCash=0,Active=true,Source="Waiver assignment",ReportedAnnualValue=null,
                BonusPayments=[],CashVesting=c.CashVesting?.Select(v=>v with{GuaranteedCash=Math.Min(c.Years.Single(y=>y.Year==v.PaymentYear).Salary,Math.Max(0,v.GuaranteedCash-c.Years.Single(y=>y.Year==v.PaymentYear).GuaranteedOtherCash-(c.BonusPayments?.Where(b=>b.Year==v.PaymentYear&&b.GuaranteedAtSigning).Sum(b=>b.Amount)??0)))}).ToList(),
                Years=c.Years.Where(y=>y.Year>=s.Year).Select(y=>y with{GuaranteedOtherCash=0,Salary=y.IsVoid?0:y.Salary-(y.Year==s.Year?earned:0),Guaranteed=y.IsVoid?0:Math.Max(0,y.Guaranteed-(y.Year==s.Year?earned:0)),ProratedBonus=0,OtherCompensation=0,OtherCash=0,SourceGuaranteed=null}).ToList()});
        }
        var next=s with{Contracts=contracts,DeadCap=debt,Players=s.Players.Select(x=>x.Football.PlayerId==entry.PlayerId?x with{Football=x.Football with{TeamName=team,DepthChartOrder=99},Reserve=false,PracticeSquad=false,TradeRequested=false,ContractConcernWeeks=0}:x).ToList()};
        if(ProEconomy.Room(next,team)-ProEconomy.Pending(next,team)<0)throw new InvalidOperationException("Insufficient cap space for the assumed contract and pending offers.");
        foreach(var year in originals.SelectMany(c=>c.Years).Where(y=>y.Year>s.Year).Select(y=>y.Year).Distinct())
            if(ProFinance.Budget(next,team,year).Available-ProEconomy.PendingForYear(next,team,year)<0)throw new InvalidOperationException($"The assumed contract exceeds {year} cap space, including pending offers.");
        return next;
    }
    static ProFranchise Reviewed(ProFranchise s,string team,WaiverEntry entry,bool staff)
    {
        if(team!=s.UserTeam||s.Career?.Unemployed==true)return Proposal(s,team,entry);
        return staff?ProManagement.SubmitStaff(s,"Staff waiver contract assumption",[entry.PlayerId],x=>Proposal(x,team,entry))
            :ProManagement.Submit(s,"Waiver contract assumption",[entry.PlayerId],0,x=>Proposal(x,team,entry));
    }
    public static ProFranchise Claim(ProFranchise s,string team,WaiverEntry entry,bool staff=false)
    {
        if(entry.Claims.Contains(team))return s;
        var preview=Reviewed(s,team,entry,staff);
        if(preview.Players.Single(p=>p.Football.PlayerId==entry.PlayerId).Football.TeamName!=team)
            return s with{GmDecisions=preview.GmDecisions};
        return s with{GmDecisions=preview.GmDecisions,Waivers=s.Waivers.Select(w=>w.PlayerId==entry.PlayerId?w with{Claims=w.Claims.Append(team).ToList()}:w).ToList()};
    }
    public static ProFranchise Award(ProFranchise s,string team,WaiverEntry entry)
    {
        var p=s.Players.Single(p=>p.Football.PlayerId==entry.PlayerId);
        var staff=team==s.UserTeam&&s.Career?.Unemployed!=true&&!ProCareer.Controls(s,p.Football.Position);
        var next=Reviewed(s,team,entry,staff);
        return next.Players.Single(p=>p.Football.PlayerId==entry.PlayerId).Football.TeamName==team
            ?ProEconomy.Log(next,$"{team} claimed {p.Football.FullName}, assuming the remaining salary contract."):next;
    }
    public static long CurrentSalary(WaiverEntry entry,int year)=>entry.OriginalContracts?.Sum(c=>c.Years.Where(y=>y.Year==year&&!y.IsVoid).Sum(y=>y.Salary-(year==entry.ReleasedYear?(long)(y.Salary*entry.ReleasedFraction):0)))??0;
}
