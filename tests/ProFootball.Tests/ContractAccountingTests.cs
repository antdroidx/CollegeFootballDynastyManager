using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public sealed class ContractAccountingTests
{
    static (ProFranchise State,ProPlayer Player,ProContract Contract) Jsn()
    {
        var s=ProLeagueService.Create("Seattle Seahawks",sourcedContracts:true);
        var p=s.Players.Single(p=>p.Football.FullName=="Jaxon Smith-Njigba");
        return(s,p,s.Contracts.Single(c=>c.PlayerId==p.Football.PlayerId));
    }
    [Fact] public void SourceCashVoidYearsAndGuaranteesRemainDistinct()
    {
        var(s,p,c)=Jsn();
        Assert.Equal(2031,ProContractInsights.PlayingEnd(c));
        Assert.Equal(42_150_000,c.ReportedAnnualValue);
        Assert.Equal(36_500_000,ProContractInsights.AnnualCash(c,c.Years.Single(y=>y.Year==2026)));
        Assert.Equal(10_371_329,ProEconomy.CapHit(c,2026));
        Assert.Equal(4_000_000,ProEconomy.CapHit(c,2032));
        Assert.All(c.Years.Where(y=>y.Year>=2032),y=>{Assert.True(y.IsVoid);Assert.Equal(0,y.Salary);Assert.Equal(0,ProContractInsights.AnnualCash(c,y));});
        Assert.All(c.Years,y=>Assert.InRange(y.Guaranteed,0,y.Salary));
        var isolated=s with{Contracts=[c]};
        Assert.Equal(36_500_000,ProFinance.Cash(isolated,s.UserTeam,2026));
        Assert.Equal(0,ProFinance.Cash(isolated,s.UserTeam,2032));
        Assert.InRange(ProContractInsights.AverageCash(c),30_000_000,33_000_000);
        Assert.Equal(32_630_996,c.Years.Single(y=>y.Year==2027).SourceGuaranteed);
    }
    [Fact] public void VoidYearsDoNotDelayRetentionAndExtensionReusesTheirBonusAllocations()
    {
        var(s,p,c)=Jsn();s=s with{Year=2031,Contracts=[c],TeamCapAllocations=[],SalaryCap=1_000_000_000,Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.Retention};
        Assert.Contains(p,ProRetention.Expiring(s,s.UserTeam));
        var salary=Math.Max(100_000_000,ProOfferTerms.RoundUp(ProEconomy.MarketSalary(s,p)));
        var quote=ProContractInsights.ProjectOffer(s,p,2,salary,0,true);
        var changed=ProEconomy.Extend(s,s.UserTeam,p.Football.PlayerId,2,salary).Contracts.Single();
        Assert.Equal(2033,ProContractInsights.PlayingEnd(changed));
        Assert.Equal(changed.Years.Count,changed.Years.Select(y=>y.Year).Distinct().Count());
        Assert.Equal(ProEconomy.BonusCharge(c,2032),ProEconomy.BonusCharge(changed,2032));
        Assert.Equal(ProEconomy.CapHit(quote,2032),ProEconomy.CapHit(changed,2032));
        Assert.Null(changed.ReportedAnnualValue);
    }
    [Fact] public void AccountingMigrationConservesCapAndLeavesChangedDealsUntouched()
    {
        var(s,p,c)=Jsn();
        var legacy=c with{AccountingVersion=0,BonusCash=null,SigningBonus=c.Years.Sum(y=>y.ProratedBonus??0),Years=c.Years.Select(y=>y.IsVoid?y with{Salary=50_000_000,OtherCompensation=y.OtherCompensation-50_000_000,IsVoid=false}:y).ToList()};
        s=s with{Contracts=[legacy]};var migrated=ProContractData.MigrateAccounting(s).Contracts.Single();
        Assert.Equal(1,migrated.AccountingVersion);
        Assert.All(c.Years,y=>Assert.Equal(ProEconomy.CapHit(legacy,y.Year),ProEconomy.CapHit(migrated,y.Year)));
        var changed=legacy with{LastChangedYear=2026};
        Assert.Same(changed,ProContractData.MigrateAccounting(s with{Contracts=[changed]}).Contracts.Single());
        var mismatched=legacy with{Years=legacy.Years.Select(y=>y with{Salary=y.Salary+250_000}).ToList()};
        Assert.Same(mismatched,ProContractData.MigrateAccounting(s with{Contracts=[mismatched]}).Contracts.Single());
    }
    [Fact] public void SourceRestructureConservesCashAndMissingTermsUseKnownPlayingEnd()
    {
        var(s,p,c)=Jsn();s=s with{Year=2028,TeamCapAllocations=[],SalaryCap=1_000_000_000,Contracts=[c]};
        var cash=ProFinance.Cash(s,s.UserTeam,2028);
        var changed=ProNegotiation.Restructure(s,s.UserTeam,p.Football.PlayerId,1_000_000);
        Assert.Equal(cash,ProFinance.Cash(changed,s.UserTeam,2028));
        var generated=ProLeagueService.Create("Seattle Seahawks");
        var fallback=generated.Contracts.Single(x=>x.PlayerId==p.Football.PlayerId);
        Assert.Equal(2031,ProContractInsights.PlayingEnd(fallback));
    }
    [Fact] public void NaturalExpirationKeepsCompletedCashAndMovesVoidDebtToFollowingYear()
    {
        var(s,p,c)=Jsn();s=s with{Year=2031,Contracts=[c]};
        var cash=ProFinance.Cash(s,s.UserTeam,2031);
        var expired=ProEconomy.Expire(s,s.UserTeam,p.Football.PlayerId);
        Assert.False(expired.Contracts.Single().Active);
        Assert.Equal("Free agent",expired.Players.Single(x=>x.Football.PlayerId==p.Football.PlayerId).Football.TeamName);
        var debt=Assert.Single(expired.DeadCap);
        Assert.Equal(2032,debt.Year);Assert.Equal(4_000_000,debt.Amount);
        Assert.Equal(cash,ProFinance.Cash(expired,s.UserTeam,2031));
        Assert.Equal(0,ProFinance.Cash(expired,s.UserTeam,2032));
    }
    [Fact] public void ZeroCashExemptCurrentRowsAreNotVoidButTrailingCapOnlyRowsAre()
    {
        var s=ProLeagueService.Create("Seattle Seahawks",sourcedContracts:true);
        var exempt=s.Players.Single(p=>p.Football.FullName=="Federico Maranges");
        var exemptDeal=s.Contracts.Single(c=>c.PlayerId==exempt.Football.PlayerId);
        Assert.False(exemptDeal.Years.Single(y=>y.Year==2026).IsVoid);
        var henry=s.Players.Single(p=>p.Football.FullName=="Derrick Henry");
        var deal=s.Contracts.Single(c=>c.PlayerId==henry.Football.PlayerId);
        Assert.True(deal.Years.Single(y=>y.Year==2028).IsVoid);
    }
    [Fact] public void ExtensionCanReduceAnOverBudgetVoidChargeWithoutForgivingDebt()
    {
        var(s,p,source)=Jsn();s=s with{Year=2031,Contracts=[],TeamCapAllocations=[],SalaryCap=100_000_000,Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.Retention};
        var c=new ProContract(p.Football.PlayerId,s.UserTeam,2031,3,0,[new(2031,1_000_000,0){ProratedBonus=0},new(2032,0,0){IsVoid=true,ProratedBonus=20_000_000,OtherCompensation=80_000_000},new(2033,0,0){IsVoid=true,ProratedBonus=80_000_000,OtherCompensation=-80_000_000}]);
        var other=c with{PlayerId=Guid.NewGuid(),Years=[new(2032,60_000_000,0)]};s=s with{Contracts=[c,other]};
        var before=ProFinance.Budget(s,s.UserTeam,2032).TotalCommitments;
        var changed=ProEconomy.Extend(s,s.UserTeam,p.Football.PlayerId,2,Math.Max(10_000_000,ProOfferTerms.RoundUp(ProEconomy.MarketSalary(s,p))),2_000_000);
        Assert.True(ProFinance.Budget(changed,s.UserTeam,2032).TotalCommitments<before);
        Assert.Equal(100_000_000,changed.Contracts.Single(x=>x.PlayerId==p.Football.PlayerId).Years.Sum(y=>y.ProratedBonus??0)-2_000_000);
        Assert.Equal(2_000_000,changed.Contracts.Single(x=>x.PlayerId==p.Football.PlayerId).BonusCash![2032]);
    }
    [Fact] public void SourceTradeTransfersFutureCashButNeverNegativeVoidCapOffsets()
    {
        var(s,p,c)=Jsn();var other=s.League.Teams.First(t=>t.Name!=s.UserTeam).Name;
        s=s with{Contracts=[c],TeamCapAllocations=[],SalaryCap=1_000_000_000,Players=s.Players.Select(x=>x.Football.PlayerId==p.Football.PlayerId?x:x with{Football=x.Football with{TeamName="Free agent"}}).ToList()};
        var moved=ProTrade.Propose(s,s.UserTeam,other,[p.Football.PlayerId],[],[],[]);
        var acquired=moved.Contracts.Single(x=>x.Active);
        Assert.All(acquired.Years,y=>Assert.True(ProEconomy.CapHit(acquired,y.Year)>=0));
        foreach(var year in Enumerable.Range(2026,9))
            Assert.Equal(ProFinance.Cash(s,s.UserTeam,year),ProFinance.Cash(moved,s.UserTeam,year)+ProFinance.Cash(moved,other,year));
        Assert.Equal(30_000_000,ProFinance.Cash(moved,s.UserTeam,2027));
    }
    [Fact] public void RestructuredVoidRowsAccelerateOnceAndExtensionDefersTheirTrigger()
    {
        var(s,p,c)=Jsn();s=s with{Year=2030,Contracts=[c],TeamCapAllocations=[],SalaryCap=1_000_000_000};
        var changed=ProNegotiation.Restructure(s,s.UserTeam,p.Football.PlayerId,4_000_000).Contracts.Single();
        var voidRows=changed.Years.Where(y=>y.IsVoid).OrderBy(y=>y.Year).ToArray();
        Assert.Equal(voidRows.Sum(y=>ProEconomy.BonusCharge(changed,y.Year)),ProEconomy.CapHit(changed,voidRows[0].Year));
        Assert.All(voidRows.Skip(1),y=>Assert.Equal(0,ProEconomy.CapHit(changed,y.Year)));
        var extended=ProContractInsights.ProjectOffer(s with{Year=2031},p,1,100_000_000,0,true);
        Assert.Equal(2_000_000,ProEconomy.CapHit(extended,2033));
    }
    [Fact] public void ReleasedYoungPlayersNewSourceDealsAreNotExtendedBackToOriginalRookieTerms()
    {
        var s=ProLeagueService.Create("Seattle Seahawks",sourcedContracts:true);
        var p=s.Players.Single(p=>p.Football.FullName=="Jack Kiser");
        var c=s.Contracts.Single(c=>c.PlayerId==p.Football.PlayerId);
        Assert.Equal(2026,c.SignedYear);Assert.Equal(2026,ProContractInsights.PlayingEnd(c));
        Assert.False(c.FutureTermsEstimated);
    }
}
