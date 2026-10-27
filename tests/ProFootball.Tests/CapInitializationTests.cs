using System.Reflection;
using System.Text.Json;
using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public sealed class CapInitializationTests
{
    static string Csv(){using var stream=typeof(ProCapInitialization).Assembly.GetManifestResourceStream("ProFootball.Core.nfl_2026_salary_caps.csv")!;using var reader=new StreamReader(stream);return reader.ReadToEnd();}
    static ProFranchise New()=>ProLeagueService.Create("Seattle Seahawks");
    [Fact] public void AllThirtyTwoCapsReconcileAndWashingtonAliasMatches()
    {
        var s=New();var rows=ProCapInitialization.Load(s.League);Assert.Equal(32,rows.Count);Assert.Equal(32,rows.Select(r=>r.TeamAbbreviation).Distinct().Count());Assert.Equal(4,rows.Count(r=>r.OtherAdjustments<0));
        Assert.All(rows,r=>{Assert.Equal(301_200_000,r.BaseCap);Assert.Equal(r.AdjustedCap,r.BaseCap+r.Rollover+r.OtherAdjustments);});
        Assert.Equal(315_053_262,ProFinance.Limit(s,s.UserTeam));Assert.Equal(305_612_517,ProFinance.Limit(s,"Atlanta Falcons"));Assert.Equal(352_979_254,ProFinance.Limit(s,"New England Patriots"));Assert.Equal(328_630_626,ProFinance.Limit(s,"Washington Commanders"));
    }
    [Fact] public async Task ExplicitCorrectionPreservesNegotiatedSpendingAndOldSaveUntilChosen()
    {
        var s=New() with{CapRulesVersion=2,TeamCapAllocations=ProCapInitialization.LoadOriginal(ProLeagueService.Load()).ToList()};
        var c=s.Contracts.First(c=>c.Team==s.UserTeam);
        s=s with{Contracts=s.Contracts.Select(x=>x==c?c with{LastChangedYear=2026}:x).ToList()};
        var root=Path.Combine(Path.GetTempPath(),"pro-cap-correction-"+Guid.NewGuid());
        try
        {
            var store=new ProSaveStore(root);await store.Save(s);var old=await store.Load(s.Id);
            Assert.Equal(302_100_000,ProFinance.Limit(old,old.UserTeam));Assert.Equal(2,old.CapRulesVersion);
            Assert.True(ProCapInitialization.CanCorrectOpeningBudgets(old));
            var corrected=ProCapInitialization.CorrectOpeningBudgets(old);
            Assert.Equal(3,corrected.CapRulesVersion);Assert.Equal(315_053_262,ProFinance.Limit(corrected,corrected.UserTeam));
            Assert.Same(old.Contracts,corrected.Contracts);Assert.Same(old.Players,corrected.Players);Assert.Same(old.DeadCap,corrected.DeadCap);
            Assert.Same(old.Bids,corrected.Bids);Assert.Same(old.TradeHistory,corrected.TradeHistory);Assert.Same(old.RolloverElections,corrected.RolloverElections);
            Assert.Same(old.FinancialHistory,corrected.FinancialHistory);
            await store.Save(corrected);var loaded=await store.Load(s.Id);
            Assert.Equal(corrected.TeamCapAllocations,loaded.TeamCapAllocations);
            Assert.False(ProCapInitialization.CanCorrectOpeningBudgets(loaded));
            Assert.Throws<InvalidOperationException>(()=>ProCapInitialization.CorrectOpeningBudgets(loaded));
        }
        finally{if(Directory.Exists(root))Directory.Delete(root,true);}
    }
    [Fact] public void CorrectionDoesNotTouchCustomLegacyOrClosedYearBudgets()
    {
        var s=New() with{CapRulesVersion=2,TeamCapAllocations=ProCapInitialization.LoadOriginal(ProLeagueService.Load()).ToList()};
        Assert.False(ProCapInitialization.CanCorrectOpeningBudgets(s with{Year=2027}));
        Assert.False(ProCapInitialization.CanCorrectOpeningBudgets(s with{CapRulesVersion=1}));
        Assert.False(ProCapInitialization.CanCorrectOpeningBudgets(s with{SalaryCap=s.SalaryCap+1}));
        Assert.False(ProCapInitialization.CanCorrectOpeningBudgets(s with{FinancialHistory=[new(s.UserTeam,2026,301_200_000,0,0,0,0,0)]}));
        var row=s.TeamCapAllocations[0];Assert.False(ProCapInitialization.CanCorrectOpeningBudgets(s with{TeamCapAllocations=s.TeamCapAllocations.Select(a=>a==row?a with{OtherAdjustments=a.OtherAdjustments+1,AdjustedCap=a.AdjustedCap+1}:a).ToList()}));
    }
    [Fact] public void ReportedRoomIsNotImportedAsGameRoomAndCorrectedSourceRetainsExactDollars()
    {
        var s=New() with{Contracts=[],DeadCap=[],Bids=[]};var b=ProFinance.Budget(s,s.UserTeam);
        Assert.Equal(3,s.CapRulesVersion);Assert.Equal(315_053_262,b.Available);Assert.NotEqual(12_376_689,b.Available);
        Assert.Equal(-1_176_444,b.OtherAdjustments);Assert.Equal(15_029_706,b.Rollover);
    }
    [Fact] public void InvalidDuplicateMissingUnknownFractionalAndUnreconciledRowsAreRejected()
    {
        var league=ProLeagueService.Load();var csv=Csv();var lines=csv.Trim().Split('\n');
        Assert.Throws<InvalidDataException>(()=>ProCapInitialization.Parse(string.Join('\n',lines.Take(32)),league));
        Assert.Throws<InvalidDataException>(()=>ProCapInitialization.Parse(csv.Replace("ATL,Atlanta","ARI,Atlanta"),league));
        Assert.Throws<InvalidDataException>(()=>ProCapInitialization.Parse(csv.Replace("ARI,Arizona","ZZZ,Arizona"),league));
        Assert.Throws<InvalidDataException>(()=>ProCapInitialization.Parse(csv.Replace("328107832","328107833"),league));
        Assert.Throws<InvalidDataException>(()=>ProCapInitialization.Parse(csv.Replace("12864463","12864463.5"),league));
    }
    [Fact] public void ImportAddsAllowanceWithoutInventingCommitmentsOrDeadMoney()
    {
        var s=New() with{TeamCapAllocations=[],CapRulesVersion=0};var before=JsonSerializer.Serialize(s.Contracts);var dead=JsonSerializer.Serialize(s.DeadCap);var imported=ProCapInitialization.Initialize(s);
        Assert.Equal(before,JsonSerializer.Serialize(imported.Contracts));Assert.Equal(dead,JsonSerializer.Serialize(imported.DeadCap));Assert.Equal(s.Players,imported.Players);Assert.Equal(s.FinancialHistory,imported.FinancialHistory);
        var b=ProFinance.Budget(imported,s.UserTeam);Assert.Equal(15_029_706,b.Rollover);Assert.Equal(-1_176_444,b.OtherAdjustments);Assert.Equal(b.AdjustedCap-b.PlayerCommitments-b.OtherCapCharges-b.DeadMoney,b.Available);
        Assert.Same(imported,ProCapInitialization.Initialize(imported));
    }
    [Fact] public async Task OldSavesKeepTheirBudgetAndNewSaveAllocationsPersist()
    {
        var directory=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString());try{
            var store=new ProSaveStore(directory);var legacy=New() with{TeamCapAllocations=[],CapRulesVersion=0};await store.Save(legacy);var loaded=await store.Load(legacy.Id);Assert.Empty(loaded.TeamCapAllocations);Assert.Equal(301_200_000,ProFinance.Limit(loaded,loaded.UserTeam));
            var current=New();await store.Save(current);var again=await store.Load(current.Id);Assert.Equal(current.TeamCapAllocations,again.TeamCapAllocations);Assert.Equal(315_053_262,ProFinance.Limit(again,again.UserTeam));
        }finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
    }
    [Fact] public void CommitmentsDeadMoneyAndOtherChargesRemainSeparateAtFullPrecision()
    {
        var s=New();var p=s.Players.First(p=>p.Football.TeamName==s.UserTeam);var c=new ProContract(p.Football.PlayerId,s.UserTeam,s.Year,2,3_000_002,[new(s.Year,12_345_678,0){OtherCompensation=123_456}]);
        s=s with{Contracts=[c],DeadCap=[new(s.UserTeam,s.Year,Guid.NewGuid(),987_654)]};var b=ProFinance.Budget(s,s.UserTeam);
        Assert.Equal(12_345_678,b.CommittedSalary);Assert.Equal(1_500_001,b.AllocatedBonus);Assert.Equal(123_456,b.OtherCapCharges);Assert.Equal(987_654,b.DeadMoney);Assert.Equal(315_053_262-12_345_678-1_500_001-123_456-987_654,b.Available);Assert.Equal(b.Available,ProEconomy.Room(s,s.UserTeam));
        Assert.Equal(15_000_000,ProCapInitialization.RoundForDisplay(15_030_000));Assert.Equal(-14_250_000,ProCapInitialization.RoundForDisplay(-14_130_000));Assert.Equal(-250_000,ProCapInitialization.RoundForDisplay(-125_000));
    }
    [Fact] public void RolloverIsReservedAtFinalRegularGameAndCreditedExactlyOnce()
    {
        var s=New() with{Contracts=[],DeadCap=[],CarryoverPercentage=50,Phase=ProPhase.RegularSeason,Week=18};
        s=s with{Games=s.Games.Select(g=>g.Phase==ProPhase.RegularSeason?g with{Football=g.Football with{HasPlayed=true,HomeScore=7,AwayScore=0}}:g).ToList()};
        var measured=ProRollover.MeasureCompletedTeams(s);var own=measured.RolloverElections.Single(e=>e.Team==s.UserTeam);Assert.Equal(157_526_631,own.Amount);Assert.Equal(157_526_631,ProEconomy.Room(measured,s.UserTeam));Assert.Equal(32,ProRollover.MeasureCompletedTeams(measured).RolloverElections.Count);
        var post=measured with{Contracts=[new(Guid.NewGuid(),s.UserTeam,s.Year,1,0,[new(s.Year,1_000_000,0)])],Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.Retention};var closed=ProFinance.CloseYear(post);Assert.Equal(own.Amount,closed.FinancialHistory.Single(h=>h.Team==s.UserTeam).CarryOut);
        var next=closed with{Year=2027,SalaryCap=320_000_000};var budget=ProFinance.Budget(next,s.UserTeam);Assert.Equal(own.Amount,budget.Rollover);Assert.Equal(0,budget.OtherAdjustments);Assert.Equal(477_526_631,budget.AdjustedCap);Assert.Equal(0,budget.RolloverReserved);
    }
    [Fact] public void RetentionBonusUsesNextLeagueYearWithoutSpendingReservedRollover()
    {
        var s=New();var p=s.Players.First(p=>p.Football.TeamName==s.UserTeam&&!p.PracticeSquad&&p.Experience>=4);var c=new ProContract(p.Football.PlayerId,s.UserTeam,s.Year-2,1,0,[new(s.Year,10_000_000,0)]);
        s=s with{Contracts=[c],Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.Retention,RolloverElections=[new(s.UserTeam,s.Year,ProFinance.Limit(s,s.UserTeam)-10_000_000,100)]};
        var salary=Math.Max(20_000_000,ProOfferTerms.AgentDemand(s,p,true).Salary);var projected=ProContractInsights.ProjectOffer(s,p,2,salary,2_000_000,true);var changed=ProEconomy.Extend(s,s.UserTeam,p.Football.PlayerId,2,salary,2_000_000);var contract=changed.Contracts[0];
        Assert.Equal(0,ProEconomy.Room(changed,s.UserTeam));Assert.Equal(0,ProEconomy.BonusCharge(contract,2026));Assert.Equal(1_000_000,ProEconomy.BonusCharge(contract,2027));Assert.Equal(2027,contract.LastChangedYear);Assert.Equal(ProContractInsights.Seasons(projected),ProContractInsights.Seasons(contract));
        Assert.Throws<InvalidOperationException>(()=>ProEconomy.Extend(changed,s.UserTeam,p.Football.PlayerId,1,salary,0));
    }
    [Fact] public void ThreeSeasonsPreserveImportedCapRolloverDraftAndCpuBudgets()
    {
        var s=ProLeagueService.Create("Seattle Seahawks",sourcedContracts:true) with{Id=Guid.Parse("53120000-0000-0000-0000-000000000001")};
        s=s with{AutoManageRookieOptions=true};
        for(var season=0;season<3;season++){
            s=ProCpu.Manage(s,true);
            for(var tick=0;tick<26&&s.Phase!=ProPhase.Offseason;tick++){
                if(s.Phase==ProPhase.Preseason&&s.Week==3)s=ProCpu.Trim(s,s.UserTeam);
                s=ProLeagueService.Advance(s);
            }
            Assert.Equal(ProPhase.Offseason,s.Phase);Assert.Equal(32,s.RolloverElections.Count(e=>e.Year==s.Year));
            s=ProOffseason.Advance(s);s=ProOffseason.Advance(s);s=ProDraft.AutoUntilUser(s,true);s=ProOffseason.Advance(s);s=ProCpu.Trim(s,s.UserTeam);s=ProOffseason.Advance(s);s=ProCpu.Manage(s,true);
            Assert.True(s.UnsignedRookies.Count==0,string.Join(";",s.UnsignedRookies.Select(r=>r.Team).Distinct().Select(t=>$"{s.Year} {t} room={ProEconomy.Room(s,t)} active={ProRoster.Active(s,t)} debt={s.DeadCap.Where(d=>d.Team==t&&d.Year==s.Year).Sum(d=>d.Amount)} charges={string.Join(",",s.DeadCap.Where(d=>d.Team==t&&d.Year==s.Year).Select(d=>$"{s.Players.FirstOrDefault(p=>p.Football.PlayerId==d.PlayerId)?.Football.FullName}:{d.Amount}"))}")));Assert.All(s.League.Teams,t=>{Assert.True(ProEconomy.Room(s,t.Name)>=0,t.Name);Assert.True(ProRoster.Active(s,t.Name)>=45&&ProRoster.Active(s,t.Name)<=90,$"{s.Year} {t.Name}: active={ProRoster.Active(s,t.Name)} room={ProEconomy.Room(s,t.Name)} debt={s.DeadCap.Where(d=>d.Team==t.Name&&d.Year==s.Year).Sum(d=>d.Amount)} carry={ProFinance.Budget(s,t.Name).Rollover} cash={ProFinance.Cash(s,t.Name,s.Year)} charges={string.Join(";",s.DeadCap.Where(d=>d.Team==t.Name&&d.Year==s.Year).Select(d=>$"{s.Players.FirstOrDefault(p=>p.Football.PlayerId==d.PlayerId)?.Football.FullName}:{d.Amount}"))}");});
            Assert.Equal((season+1)*32,s.FinancialHistory.Count);
        }
    }
}
