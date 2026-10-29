using System.Text.Json;
using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public sealed class GuaranteeAllocationTests
{
    static readonly Guid Id=Guid.Parse("76903ad1-85a3-5d80-9c79-d60f3ccdac2b");
    static ProContract Original()
    {
        using var stream=typeof(ProLeagueService).Assembly.GetManifestResourceStream("ProFootball.Core.nfl-contracts.json")!;
        return JsonSerializer.Deserialize<List<ProContract>>(stream)!.Single(c=>c.PlayerId==Id);
    }
    [Fact] public void ReviewReclassifiesPayWithoutChangingAnyAnnualCashOrCap()
    {
        var old=Original();var actual=ProContractData.Load().Single(c=>c.PlayerId==Id);
        Assert.Equal(0,actual.Years.Single(y=>y.Year==2026).Guaranteed);Assert.Equal(4_000_000,actual.Years.Single(y=>y.Year==2026).GuaranteedOtherCash);
        Assert.Empty(actual.BonusPayments!);Assert.Equal(0,actual.BonusCash![2027]);Assert.Equal(5_000_000,actual.Years.Single(y=>y.Year==2027).OtherCash);
        foreach(var y in old.Years){Assert.Equal(ProEconomy.CapHit(old,y.Year),ProEconomy.CapHit(actual,y.Year));Assert.Equal(ProContractInsights.AnnualCash(old,y),ProContractInsights.AnnualCash(actual,actual.Years.Single(r=>r.Year==y.Year)));}
    }
    [Fact] public void BuyerDoesNotInheritASecondGuaranteeOnAlreadyPaidRosterCash()
    {
        var s=ProLeagueService.Create("Baltimore Ravens",sourcedContracts:true);var p=s.Players.Single(p=>p.Football.PlayerId==Id);var c=s.Contracts.Single(c=>c.Active&&c.PlayerId==Id);
        s=s with{Players=[p],Contracts=[c],DeadCap=[],TeamCapAllocations=[],SalaryCap=1_000_000_000};var buyer=s.League.Teams.First(t=>t.Name!=s.UserTeam).Name;
        var moved=ProTrade.Propose(s,s.UserTeam,buyer,[Id],[],[],[]);
        var acquired=moved.Contracts.Single(c=>c.Active);Assert.Equal(0,acquired.Years.Single(y=>y.Year==2026).Guaranteed);
        foreach(var year in new[]{2026,2027}){Assert.Equal(ProFinance.Cash(s,s.UserTeam,year),ProFinance.Cash(moved,s.UserTeam,year)+ProFinance.Cash(moved,buyer,year));Assert.Equal(ProFinance.Budget(s,s.UserTeam,year).TotalCommitments,ProFinance.Budget(moved,s.UserTeam,year).TotalCommitments+ProFinance.Budget(moved,buyer,year).TotalCommitments);}
        var cut=ProEconomy.Release(moved,buyer,Id,false);Assert.Equal(0,ProFinance.Cash(cut,buyer,2026));Assert.Equal(4_000_000,ProFinance.Cash(cut,s.UserTeam,2026));
        Assert.Equal(29_437_500,ProTermination.Charges(s with{Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.FreeAgency},c,Guid.Empty).Where(d=>d.Year==2026).Sum(d=>d.Amount));
        Assert.Equal(16_718_750,ProTermination.Charges(s,c,Guid.Empty).Where(d=>d.Year==2026).Sum(d=>d.Amount));
    }
    [Fact] public void MigrationRequiresExactUnchangedSourceBasisAndNeverRewritesBookedDebt()
    {
        var s=ProLeagueService.Create("Baltimore Ravens");var raw=Original();var debt=new DeadCapCharge(s.UserTeam,2026,Id,123456){CashAmount=9876};
        var waiver=new WaiverEntry(Id,1,[]){OriginalContracts=[raw],ReleaseId=Guid.NewGuid(),ReleasedYear=2026};
        var migrated=ProContractData.MigrateAccounting(s with{Contracts=[raw],DeadCap=[debt],Waivers=[waiver]});
        Assert.Equal(0,migrated.Contracts.Single().Years.Single(y=>y.Year==2026).Guaranteed);Assert.Equal(5_000_000,migrated.Waivers.Single().OriginalContracts!.Single().Years.Single(y=>y.Year==2027).OtherCash);Assert.Same(debt,migrated.DeadCap.Single());
        foreach(var changed in new[]{raw with{LastChangedYear=2026},raw with{Source="Trade assignment"},raw with{Active=false,TerminatedYear=2026},raw with{Years=raw.Years.Select(y=>y with{Salary=y.Salary+1}).ToList()},raw with{BonusCash=new(){{2026,1},{2027,1_000_000}}}})
        {var preserved=ProContractData.MigrateAccounting(s with{Contracts=[changed],DeadCap=[debt]});Assert.Same(changed,preserved.Contracts.Single());Assert.Same(debt,preserved.DeadCap.Single());}
    }
    [Fact] public async Task RealSaveLoaderAppliesOnlyTheGuardedClassificationCorrection()
    {
        var directory=Path.Combine(Path.GetTempPath(),"pro-guarantee-"+Guid.NewGuid());
        try{var store=new ProSaveStore(directory);var raw=Original();var s=ProLeagueService.Create("Baltimore Ravens") with{Contracts=[raw],DeadCap=[new("Baltimore Ravens",2026,Id,789)]};
            await store.Save(s);var loaded=await store.Load(s.Id);Assert.Equal(s.DeadCap,loaded.DeadCap);Assert.Equal(s.Career,loaded.Career);
            var c=loaded.Contracts.Single();Assert.Equal(0,c.Years.Single(y=>y.Year==2026).Guaranteed);Assert.Equal(20_000_000,ProFinance.Cash(loaded,s.UserTeam,2027));
            await store.Save(loaded);var again=await store.Load(s.Id);Assert.Equal(JsonSerializer.Serialize(c),JsonSerializer.Serialize(again.Contracts.Single()));}
        finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
    }
}
