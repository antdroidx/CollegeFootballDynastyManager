using DynastyManager.Core.Models;
using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;

public class RookieOptionTests
{
    static (ProFranchise State,ProPlayer Player,ProContract Contract) Setup()
    {
        var s=ProLeagueService.Create("Seattle Seahawks");
        var p=s.Players.First(p=>p.Football.Position==Position.QB&&p.Football.TeamName==s.UserTeam) with{Age=24,Experience=2,CreditedSeasons=2,RookieYear=2026,DraftOverall=1};
        var c=new ProContract(p.Football.PlayerId,s.UserTeam,2026,4,2_000_000,Enumerable.Range(2026,4).Select(y=>new ContractYear(y,1_000_000,0)).ToList()){OriginalRookieYear=2026};
        s=s with{Year=2028,Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.Retention,Players=[p],Contracts=[c],Games=[],
            OptionTenders=[new(2029,"QB",ProOptionTier.Base,12_000_000,true)]};
        return(s,p,c);
    }
    [Fact] public void ExerciseGuaranteesFourthAndFifthYearsWithoutChangingEarlierCashOrNegotiationLock()
    {
        var(s,p,c)=Setup();var result=ProRookieOptions.Decide(s,s.UserTeam,p.Football.PlayerId,true);var changed=Assert.Single(result.Contracts);
        Assert.Equal(2030,ProContractInsights.PlayingEnd(changed));Assert.Equal(1_000_000,changed.Years.Single(y=>y.Year==2029).Guaranteed);
        var fifth=changed.Years.Single(y=>y.Year==2030);Assert.Equal(12_000_000,fifth.Salary);Assert.Equal(fifth.Salary,fifth.Guaranteed);
        Assert.Equal(c.SigningBonus,changed.SigningBonus);Assert.Equal(c.LastChangedYear,changed.LastChangedYear);
        Assert.Equal(ProEconomy.CapHit(c,2028),ProEconomy.CapHit(changed,2028));
        Assert.Equal(12_000_000,ProEconomy.CapHit(changed,2030));
        Assert.True(Assert.Single(result.RookieOptionDecisions).Exercised);
        Assert.False(ProContractInsights.ConcernEligible(result with{Year=2030},p,changed));
        Assert.Throws<InvalidOperationException>(()=>ProRookieOptions.Decide(result,s.UserTeam,p.Football.PlayerId,true));
        var extended=ProEconomy.Extend(result,s.UserTeam,p.Football.PlayerId,1,60_000_000);
        Assert.Equal(2031,ProContractInsights.PlayingEnd(extended.Contracts.Single()));
    }
    [Fact] public void DecliningKeepsTheFourYearDealAndCannotBeUndone()
    {
        var(s,p,c)=Setup();var result=ProRookieOptions.Decide(s,s.UserTeam,p.Football.PlayerId,false);
        Assert.Same(s.Contracts,result.Contracts);Assert.Equal(2029,ProContractInsights.PlayingEnd(c));
        Assert.False(Assert.Single(result.RookieOptionDecisions).Exercised);Assert.Empty(ProRookieOptions.Pending(result));
        Assert.Throws<InvalidOperationException>(()=>ProRookieOptions.Decide(result,s.UserTeam,p.Football.PlayerId,true));
    }
    [Fact] public void OnlyOriginalFirstRoundDealsQualifyAndTradedOriginSurvivesRebasedSignedYear()
    {
        var(s,p,c)=Setup();Assert.Single(ProRookieOptions.Pending(s));
        Assert.Empty(ProRookieOptions.Pending(s with{Players=[p with{DraftOverall=33}]}));
        Assert.Empty(ProRookieOptions.Pending(s with{Contracts=[c with{OriginalRookieYear=null,SignedYear=2028}]}));
        Assert.Single(ProRookieOptions.Pending(s with{Contracts=[c with{SignedYear=2028}]}));
        Assert.Empty(ProRookieOptions.Pending(s with{Contracts=[c with{Years=c.Years.Append(new(2030,20_000_000,0)).ToList()}]}));
        var changed=ProRookieOptions.Decide(s with{Contracts=[c with{LastChangedYear=2028}]},s.UserTeam,p.Football.PlayerId,true);
        Assert.Equal(2028,changed.Contracts.Single().LastChangedYear);
    }
    [Fact] public void WindowIncludesFollowingDraftAndBlocksUnresolvedUserDecisionAtItsEnd()
    {
        var(s,p,_)=Setup();Assert.False(ProRookieOptions.InWindow(s with{Phase=ProPhase.RegularSeason},2026));
        var draft=s with{Year=2029,OffseasonStep=OffseasonStep.Draft,DraftIndex=224,Prospects=[]};
        Assert.True(ProRookieOptions.InWindow(draft,2026));
        Assert.Throws<InvalidOperationException>(()=>ProOffseason.Advance(draft));
        var declined=ProRookieOptions.Decide(draft,draft.UserTeam,p.Football.PlayerId,false);
        Assert.Equal(OffseasonStep.RosterCuts,ProOffseason.Advance(declined).OffseasonStep);
        Assert.False(ProRookieOptions.InWindow(draft with{OffseasonStep=OffseasonStep.RosterCuts},2026));
        Assert.False(ProRookieOptions.InWindow(s with{Year=2030},2026));
    }
    [Fact] public void CapFailureDoesNotCreatePartialDecisionAndStaffCanDeclineInstead()
    {
        var(s,p,_)=Setup();var cap=ProFinance.Limit(s,s.UserTeam,2030);
        s=s with{Contracts=s.Contracts.Append(new(Guid.NewGuid(),s.UserTeam,2030,1,0,[new(2030,cap-1_000_000,0)])).ToList()};
        Assert.Throws<InvalidOperationException>(()=>ProRookieOptions.Decide(s,s.UserTeam,p.Football.PlayerId,true));
        Assert.Empty(s.RookieOptionDecisions);Assert.Equal(2029,ProContractInsights.PlayingEnd(s.Contracts[0]));
        var managed=ProRookieOptions.ManageCpu(s,true,s.UserTeam);
        Assert.False(Assert.Single(managed.RookieOptionDecisions).Exercised);Assert.Equal(s.Contracts,managed.Contracts);
    }
    [Fact] public void OppositeUnitIsStaffManagedAndManualCoordinatorExerciseIsRestricted()
    {
        var(s,p,_)=Setup();s=s with{Career=s.Career! with{Role=StaffRole.DefensiveCoordinator}};
        Assert.Throws<InvalidOperationException>(()=>ProManagement.Submit(s,"Rookie option",[p.Football.PlayerId],0,x=>ProRookieOptions.Decide(x,x.UserTeam,p.Football.PlayerId,true)));
        var managed=ProRookieOptions.ManageCpu(s);
        Assert.True(Assert.Single(managed.RookieOptionDecisions).Exercised);
    }
    [Fact] public void CoordinatorFutureUnitBudgetCanRejectAndStaffThenDeclinesWithoutAddingDebt()
    {
        var(s,p,_)=Setup();s=s with{Career=s.Career! with{Role=StaffRole.OffensiveCoordinator}};
        var receiver=p with{Football=p.Football with{PlayerId=Guid.NewGuid(),Position=Position.WR}};
        var ceiling=(long)(ProFinance.Limit(s,s.UserTeam,2030)*.55);
        s=s with{Players=s.Players.Append(receiver).ToList(),Contracts=s.Contracts.Append(new(receiver.Football.PlayerId,s.UserTeam,2030,1,0,[new(2030,ceiling-1_000_000,0)])).ToList()};
        var result=ProManagement.Submit(s,"Rookie option",[p.Football.PlayerId],0,x=>ProRookieOptions.Decide(x,x.UserTeam,p.Football.PlayerId,true));
        Assert.False(Assert.Single(result.GmDecisions).Approved);Assert.Empty(result.RookieOptionDecisions);Assert.Same(s.Contracts,result.Contracts);
        var staff=ProRookieOptions.ManageCpu(s,true,s.UserTeam);
        Assert.False(Assert.Single(staff.RookieOptionDecisions).Exercised);Assert.Same(s.Contracts,staff.Contracts);
    }
    [Fact] public void ConvertingVoidYearKeepsBonusDebtAndDoesNotCarryOldAccelerationIntoSalary()
    {
        var(s,p,c)=Setup();
        c=c with{SigningBonus=0,BonusYears=6,Years=c.Years.Select(y=>y with{ProratedBonus=1_000_000}).Concat(new[]{
            new ContractYear(2030,0,0){IsVoid=true,ProratedBonus=1_000_000,OtherCompensation=1_000_000},
            new ContractYear(2031,0,0){IsVoid=true,ProratedBonus=1_000_000,OtherCompensation=-1_000_000}}).ToList()};
        var result=ProRookieOptions.Decide(s with{Contracts=[c]},s.UserTeam,p.Football.PlayerId,true);var deal=result.Contracts.Single();
        Assert.Equal(13_000_000,ProEconomy.CapHit(deal,2030));Assert.Equal(1_000_000,ProEconomy.CapHit(deal,2031));
        Assert.Equal(12_000_000,ProContractInsights.AnnualCash(deal,deal.Years.Single(y=>y.Year==2030)));
        Assert.True(deal.Years.Single(y=>y.Year==2031).IsVoid);
    }
    [Fact] public async Task SavedDecisionAndTenderSurviveReloadWithoutRepricingOrDuplicatingLiability()
    {
        var(s,p,_)=Setup();var result=ProRookieOptions.Decide(s,s.UserTeam,p.Football.PlayerId,true);
        var root=Path.Combine(Path.GetTempPath(),"pro-option-save-"+Guid.NewGuid());
        try
        {
            var store=new ProSaveStore(root);await store.Save(result);var loaded=await store.Load(result.Id);
            Assert.Equal(result.RookieOptionDecisions,loaded.RookieOptionDecisions);Assert.Equal(result.OptionTenders,loaded.OptionTenders);
            Assert.Equal(result.Contracts.Sum(c=>ProEconomy.CapHit(c,2030)),loaded.Contracts.Sum(c=>ProEconomy.CapHit(c,2030)));
            Assert.Empty(ProRookieOptions.Pending(loaded));
        }
        finally{if(Directory.Exists(root))Directory.Delete(root,true);}
    }
    [Fact] public void HistoricalTenderUsesCapPercentagesAndIsFrozenAcrossSubsequentOffers()
    {
        var(s,p,_)=Setup();s=s with{OptionTenders=[],SalarySamples=Enumerable.Range(2024,5).Select(y=>new ProSalarySample(y,s.SalaryCap,new(){["QB"]=Enumerable.Range(1,30).Select(i=>i*1_000_000L).ToList()})).ToList()};
        var prepared=ProRookieOptions.Prepare(s);var quote=ProRookieOptions.Quote(prepared,p);
        var expected=(long)Math.Ceiling(17_000_000d/s.SalaryCap*ProFinance.ForecastCap(s,2029)/250_000)*250_000;
        Assert.Equal(expected,quote.Salary);Assert.False(quote.EstimatedHistory);
        Assert.Equal(quote,ProRookieOptions.Quote(prepared with{SalarySamples=[]},p));
        var prices=prepared.OptionTenders.OrderBy(q=>q.Tier).Select(q=>q.Salary).ToArray();Assert.Equal(prices.Order(),prices);
        Assert.True(ProRookieOptions.Quote(s with{SalarySamples=[]},p).EstimatedHistory);
    }
    [Fact] public void OriginalBallotGameSelectionsSetProBowlTierAndVoidRowsAreNotSalarySamples()
    {
        var(s,p,c)=Setup();s=s with{Honors=[new(2026,[],[new(p,"NFC")])]};
        Assert.Equal(ProOptionTier.OneProBowl,ProRookieOptions.Tier(s,p,2026));
        s=s with{Honors=s.Honors.Append(new(2027,[],[new(p,"NFC")])).ToList()};
        Assert.Equal(ProOptionTier.MultipleProBowls,ProRookieOptions.Tier(s,p,2026));
        var voidOnly=c with{Years=[new(2028,90_000_000,0){IsVoid=true}]};
        Assert.Empty(ProRookieOptions.Sample(s with{Contracts=[voidOnly]}).Salaries);
        var captured=ProRookieOptions.CaptureYear(s);Assert.Single(ProRookieOptions.CaptureYear(captured).SalarySamples);
    }
}
