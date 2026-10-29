using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public sealed class OpeningLedgerTests
{
    [Fact] public void SourcedOpeningDebtContainsAllObservedTeamsWithoutInventedCash()
    {
        var s=ProLeagueService.Create("Seattle Seahawks",sourcedContracts:true,applyOpeningTrades:false);
        Assert.Equal("2026-10-27",s.OpeningFinancialSnapshot);
        Assert.Equal(32,s.DeadCap.Select(d=>d.Team).Distinct().Count());
        Assert.Equal(735,s.DeadCap.Count);
        Assert.Equal(1_591_660_715,s.DeadCap.Sum(d=>d.Amount));
        Assert.Equal(s.DeadCap.Count,s.DeadCap.Select(d=>d.SourceKey).Distinct().Count());
        Assert.All(s.DeadCap,d=>{Assert.Equal(2026,d.Year);Assert.Equal(0,d.CashAmount);Assert.Null(d.ReleaseId);Assert.False(d.RetainedContract);Assert.NotEmpty(d.SourceName!);Assert.Equal(64,d.SourceSha256!.Length);});
        Assert.Contains(s.DeadCap,d=>d.PlayerId==Guid.Empty&&d.SourceKind=="reported_aggregate_remainder");
        Assert.Equal(4_844_656,ProFinance.Budget(s,s.UserTeam).DeadMoney);
        Assert.Equal(0,ProFinance.Budget(s,s.UserTeam,2027).DeadMoney);
        Assert.Equal(ProFinance.Cash(s with{DeadCap=[]},s.UserTeam,2026),ProFinance.Cash(s,s.UserTeam,2026));
        var generated=ProLeagueService.Create("Seattle Seahawks",applyOpeningTrades:false);Assert.Empty(generated.DeadCap);Assert.Null(generated.OpeningFinancialSnapshot);
    }
    [Fact] public void ReleasingAnActivePlayerDoesNotEraseSourceAssessments()
    {
        var s=ProLeagueService.Create("Seattle Seahawks",sourcedContracts:true,applyOpeningTrades:false);
        var p=s.Players.Single(p=>p.Football.FullName=="Jaxon Smith-Njigba");
        var released=ProEconomy.Release(s,s.UserTeam,p.Football.PlayerId,false);
        Assert.Equal(s.DeadCap,released.DeadCap.Where(d=>d.SourceKey!=null).ToList());
        Assert.Equal(4_844_656,s.DeadCap.Where(d=>d.Team==s.UserTeam).Sum(d=>d.Amount));
        Assert.True(ProFinance.Budget(released,s.UserTeam).DeadMoney>4_844_656);
    }
    [Fact] public async Task SaveLoadingPreservesSourceProvenanceAndNeverBackfillsOldSaves()
    {
        var directory=Path.Combine(Path.GetTempPath(),"pro-opening-"+Guid.NewGuid());
        try
        {
            var store=new ProSaveStore(directory);var s=ProLeagueService.Create("Seattle Seahawks",sourcedContracts:true,applyOpeningTrades:false);
            await store.Save(s);var loaded=await store.Load(s.Id);
            Assert.Equal(s.OpeningFinancialSnapshot,loaded.OpeningFinancialSnapshot);Assert.Equal(s.DeadCap,loaded.DeadCap);
            var original=s with{OpeningFinancialSnapshot=null,DeadCap=[new(s.UserTeam,2026,Guid.Empty,123456){CashAmount=456}]};
            await store.Save(original);var old=await store.Load(original.Id);
            Assert.Null(old.OpeningFinancialSnapshot);Assert.Equal(original.DeadCap,old.DeadCap);Assert.Equal(System.Text.Json.JsonSerializer.Serialize(original.Contracts),System.Text.Json.JsonSerializer.Serialize(old.Contracts));
        }
        finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
    }
    [Fact] public void ReviewedSeattleMatchesDatedLiabilitiesWithoutAReportedRoomInput()
    {
        var s=ProLeagueService.Create("Seattle Seahawks",sourcedContracts:true,applyOpeningTrades:false) with{Phase=ProPhase.RegularSeason};
        var b=ProFinance.Budget(s,s.UserTeam);
        Assert.Equal(300_971_343,b.TotalCommitments);Assert.Equal(315_053_262,b.AdjustedCap);Assert.Equal(14_081_919,b.Available);
        var foster=s.Players.Single(p=>p.Football.FullName.StartsWith("Montorie Foster"));
        Assert.Equal("Free agent",foster.Football.TeamName);Assert.DoesNotContain(s.Contracts,c=>c.PlayerId==foster.Football.PlayerId);
        var avery=s.Players.Single(p=>p.Football.FullName=="Avery Smith");var c=s.Contracts.Single(c=>c.PlayerId==avery.Football.PlayerId);
        Assert.Equal(885_000,ProEconomy.CapHit(c,2026));Assert.All(c.BonusCash!.Values,v=>Assert.Equal(0,v));
        Assert.Equal(3,c.Years.Count);Assert.Equal(247_500,c.Years[0].Guaranteed);
        var diggs=s.Players.Single(p=>p.Football.FullName=="Trevon Diggs");Assert.True(diggs.PracticeSquad);
        Assert.Equal(220_200,ProEconomy.CapHit(s.Contracts.Single(c=>c.PlayerId==diggs.Football.PlayerId),2026));
    }
    static (ProFranchise S,Guid Id) International()
    {
        var s=ProLeagueService.Create("Seattle Seahawks",applyOpeningTrades:false);var p=s.Players.First(p=>p.Football.TeamName==s.UserTeam) with{PracticeSquad=true,Reserve=false,Retired=false};
        var id=p.Football.PlayerId;var c=new ProContract(id,s.UserTeam,2026,1,0,[new(2026,247_500,0)]){AccountingVersion=1,BonusCash=[],BonusPayments=[]};
        return(s with{Players=[p],Contracts=[c],Games=[],Phase=ProPhase.RegularSeason,Week=10,InternationalPracticeDesignations=[new(s.UserTeam,2026,id)]},id);
    }
    [Fact] public void InternationalExemptionEndsOnPromotionAndCannotReappearByDemotion()
    {
        var(s,id)=International();var b=ProFinance.Budget(s,s.UserTeam);Assert.Equal(247_500,b.InternationalPracticeSalaryExcluded);Assert.Equal(0,b.CommittedSalary);
        Assert.Equal(0,ProFinance.Budget(s,s.UserTeam,2027).InternationalPracticeSalaryExcluded);
        var poor=s with{SalaryCap=100_000,TeamCapAllocations=[]};Assert.Throws<InvalidOperationException>(()=>ProRoster.Practice(poor,s.UserTeam,id,false));
        var active=ProRoster.Practice(s,s.UserTeam,id,false);Assert.Empty(active.InternationalPracticeDesignations);
        Assert.Equal(0,ProFinance.Budget(active,s.UserTeam).InternationalPracticeSalaryExcluded);Assert.True(ProFinance.Budget(active,s.UserTeam).CommittedSalary>=885_000);
        var practice=ProRoster.Practice(active,s.UserTeam,id,true);Assert.Equal(0,ProFinance.Budget(practice,s.UserTeam).InternationalPracticeSalaryExcluded);
    }
    [Fact] public void ExemptPracticeReleaseKeepsEarnedCashWithoutInventingCapDebtOrTradeRights()
    {
        var(s,id)=International();var other=s.League.Teams.First(t=>t.Name!=s.UserTeam).Name;
        Assert.Throws<InvalidOperationException>(()=>ProTrade.Propose(s,s.UserTeam,other,[id],[],[],[]));
        var cut=ProEconomy.Release(s,s.UserTeam,id);Assert.Empty(cut.Waivers);Assert.Empty(cut.DeadCap);Assert.Empty(cut.InternationalPracticeDesignations);
        Assert.Equal(123_750,ProFinance.Cash(cut,s.UserTeam,2026));Assert.Equal(0,ProFinance.Budget(cut,s.UserTeam).TotalCommitments);
    }

}
