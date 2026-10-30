using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public sealed class OffseasonCapCountingTests
{
    static ProFranchise Camp(bool undrafted=false)
    {
        var s=ProLeagueService.Create("Seattle Seahawks",preserveLegacyPracticeForAudit:true);var template=s.Players.First(p=>p.Football.TeamName==s.UserTeam);
        var players=Enumerable.Range(0,52).Select(i=>template with{Football=template.Football with{PlayerId=Guid.Parse($"00000000-0000-0000-0000-{i+1:000000000000}"),TeamName=s.UserTeam},Experience=0,CreditedSeasons=0,RookieYear=s.Year,DraftOverall=undrafted?null:10,PracticeSquad=false,Reserve=false}).ToList();
        var contracts=players.Select((p,i)=>new ProContract(p.Football.PlayerId,s.UserTeam,s.Year,1,0,[new(s.Year,i<51?20_000_000:3_000_000,0){ProratedBonus=i<51?0:5_000_000,OtherCompensation=i<51?0:200_000}])).ToList();
        return s with{Players=players,Contracts=contracts,TeamCapAllocations=[],SalaryCap=2_000_000_000,Phase=ProPhase.Preseason,CapRulesVersion=2};
    }
    [Theory][InlineData(false,1_770_000)][InlineData(true,885_000)]
    public void NonTopSalaryRetainsRequiredExcessAndEveryBonus(bool undrafted,long excluded)
    {
        var s=Camp(undrafted);var b=ProFinance.Budget(s,s.UserTeam);
        Assert.Equal(excluded,b.OffseasonSalaryExcluded);
        Assert.Equal(1_023_000_000-excluded,b.CommittedSalary);Assert.Equal(1_023_000_000,b.RosterSalary);
        Assert.Equal(5_000_000,b.AllocatedBonus);Assert.Equal(200_000,b.OtherCapCharges);
        Assert.Equal(b.AdjustedCap-b.CommittedSalary-b.AllocatedBonus-b.OtherCapCharges,b.Available);
    }
    [Fact] public void RankingsUseFullAnnualContractValueRatherThanBaseSalaryAlone()
    {
        var s=Camp();var last=s.Contracts.Last();
        s=s with{Players=s.Players.Select(p=>p.Football.PlayerId==last.PlayerId?p with{DraftOverall=null}:p).ToList(),Contracts=s.Contracts.Select(c=>c==last?c with{Years=[last.Years[0] with{ProratedBonus=30_000_000}]}:c).ToList()};
        // The bonus-heavy deal belongs to the top 51; a $20m-base deal is now outside it.
        Assert.Equal(1_770_000,ProFinance.Budget(s,s.UserTeam).OffseasonSalaryExcluded);
    }
    [Fact] public void SeasonEntryUsesFullSalaryEvenWhenPreseasonBudgetFits()
    {
        var s=Camp();var camp=ProEconomy.Used(s,s.UserTeam);s=s with{SalaryCap=camp};
        Assert.Equal(0,ProEconomy.Room(s,s.UserTeam));
        Assert.Contains("over the salary cap",ProSeasonEligibility.Issue(s,s.UserTeam));
        Assert.Contains(ProInbox.Build(s),n=>n.Id.StartsWith("cap-"));
        Assert.Equal(0,ProFinance.Budget(s with{Phase=ProPhase.RegularSeason},s.UserTeam).OffseasonSalaryExcluded);
    }
    [Fact] public void LegacySavesAndFuturePlanningKeepTheirOriginalFullSalaryBasis()
    {
        var s=Camp();Assert.Equal(0,ProFinance.Budget(s with{CapRulesVersion=1},s.UserTeam).OffseasonSalaryExcluded);
        Assert.Equal(0,ProFinance.Budget(s with{Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.Retention},s.UserTeam).OffseasonSalaryExcluded);
        Assert.Equal(0,ProFinance.Budget(s,s.UserTeam,s.Year+1).OffseasonSalaryExcluded);
    }
    [Fact] public async Task LoadingAnExistingRulesOneSaveDoesNotRebudgetIt()
    {
        var s=Camp() with{CapRulesVersion=1};var before=ProFinance.Budget(s,s.UserTeam);
        var directory=Path.Combine(Path.GetTempPath(),"pro-cap-counting-"+Guid.NewGuid());
        try{
            var store=new ProSaveStore(directory);await store.Save(s);var loaded=await store.Load(s.Id);
            Assert.Equal(1,loaded.CapRulesVersion);Assert.Equal(0,ProFinance.Budget(loaded,loaded.UserTeam).OffseasonSalaryExcluded);
            Assert.Equal(before.Available,ProFinance.Budget(loaded,loaded.UserTeam).Available);
        }finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
    }
    [Fact] public void ExistingSourceSalaryCreditsCannotBeExcludedTwice()
    {
        var s=Camp();var last=s.Contracts.Last();
        s=s with{Contracts=s.Contracts.Select(c=>c==last?c with{Years=[last.Years[0] with{Salary=247_500,OtherCompensation=-247_500,ProratedBonus=0}]}:c).ToList()};
        var b=ProFinance.Budget(s,s.UserTeam);
        Assert.Equal(0,b.OffseasonSalaryExcluded);Assert.Equal(1_020_000_000,b.TotalCommitments);
    }
    [Fact] public void CpuFinalCutsDoNotEarnRegularSeasonSalaryDuringPreseasonWeekThree()
    {
        var s=Camp();var template=s.Players.First();
        var extra=Enumerable.Range(100,3).Select(i=>template with{Football=template.Football with{PlayerId=Guid.Parse($"00000000-0000-0000-0000-{i:000000000000}")}}).ToArray();
        s=s with{Week=3,Games=[],Career=s.Career! with{Unemployed=true},Players=s.Players.Concat(extra).ToList(),Contracts=s.Contracts.Select(c=>c with{Years=c.Years.Select(y=>y with{OtherCompensation=0}).ToList()}).Concat(extra.Select(p=>new ProContract(p.Football.PlayerId,s.UserTeam,s.Year,1,0,[new(s.Year,1_000_000,0)]))).ToList()};
        // Force real cuts (rather than retained practice depth), to test preseason termination cash.
        var practice=Enumerable.Range(200,16).Select(i=>template with{PracticeSquad=true,Football=template.Football with{PlayerId=Guid.Parse($"00000000-0000-0000-0000-{i:000000000000}")}}).ToArray();
        s=s with{Players=s.Players.Concat(practice).ToList(),Contracts=s.Contracts.Concat(practice.Select(p=>new ProContract(p.Football.PlayerId,s.UserTeam,s.Year,1,0,[new(s.Year,300_000,0)]))).ToList()};
        var advanced=ProLeagueService.Advance(s);
        Assert.Equal(ProPhase.RegularSeason,advanced.Phase);Assert.Equal(1,advanced.Week);
        Assert.True(ProRoster.Active(advanced,s.UserTeam)<=53);
        Assert.Contains(advanced.Contracts,c=>!c.Active);
        Assert.All(advanced.Contracts.Where(c=>!c.Active),c=>Assert.Equal(0,c.EarnedCash));
    }
}
