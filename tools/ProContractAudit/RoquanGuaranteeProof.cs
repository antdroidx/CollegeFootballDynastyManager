using ProFootball.Core;
public static class RoquanGuaranteeProof
{
    public static object Create(ProFranchise state)
    {
        var p=state.Players.Single(p=>p.Football.FullName=="Roquan Smith");
        var c=state.Contracts.Single(c=>c.Active&&c.PlayerId==p.Football.PlayerId);
        var corrected=c with{BonusPayments=[],BonusCash=c.BonusCash!.ToDictionary(k=>k.Key,k=>0L),Years=c.Years.Select(y=>y.Year==2026?y with{Guaranteed=0,GuaranteedOtherCash=4_000_000}:y.Year==2027?y with{OtherCash=5_000_000}:y).ToList()};
        var s=state with{Players=[p],Contracts=[corrected],DeadCap=[],TeamCapAllocations=[],SalaryCap=1_000_000_000,Phase=ProPhase.Preseason};
        var pre=ProTermination.Charges(s with{Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.FreeAgency},corrected,Guid.Empty).Where(d=>d.Year==2026).Sum(d=>d.Amount);
        var post=ProTermination.Charges(s,corrected,Guid.Empty).Where(d=>d.Year==2026).Sum(d=>d.Amount);
        var buyer=state.League.Teams.First(t=>t.Name!=c.Team&&t.Name!=state.UserTeam).Name;
        var moved=ProTrade.Propose(s,c.Team,buyer,[c.PlayerId],[],[],[]);
        var acquired=moved.Contracts.Single(x=>x.Active);var released=ProEconomy.Release(moved,buyer,c.PlayerId,false);
        var years=Enumerable.Range(2026,2).Select(year=>new{year,oldCash=ProFinance.Cash(s,c.Team,year),assignedCash=ProFinance.Cash(moved,c.Team,year)+ProFinance.Cash(moved,buyer,year),oldCap=ProFinance.Budget(s,c.Team,year).TotalCommitments,assignedCap=ProFinance.Budget(moved,c.Team,year).TotalCommitments+ProFinance.Budget(moved,buyer,year).TotalCommitments}).ToArray();
        if(pre!=29_437_500||post!=16_718_750||years.Any(y=>y.oldCash!=y.assignedCash||y.oldCap!=y.assignedCap)||acquired.Years.Single(y=>y.Year==2026).Guaranteed!=0||ProFinance.Cash(released,buyer,2026)!=0)throw new InvalidDataException("Guarantee allocation proof failed");
        return new{scope="Read-only in-memory classification proof, not app/source/save changes. Published roster due date identifies2026 guaranteed4m as other pay, not a second guaranteed P5 salary.2027 published cash remainder1m is unallocated non-prorated other pay, not a new signing/option allocation (published proration does not increase). Annual cash/cap are unchanged. Current source cut figures are validation, not inputs to invent guarantees.",sourceUrl="https://overthecap.com/player/roquan-smith/6893/",pre,post,years,buyerGuaranteedBase=acquired.Years.Single(y=>y.Year==2026).Guaranteed,buyerImmediateReleaseCash=ProFinance.Cash(released,buyer,2026)};
    }
}
