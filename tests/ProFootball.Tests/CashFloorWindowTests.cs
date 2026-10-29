using ProFootball.Core;
using Xunit;
namespace ProFootball.Tests;
public class CashFloorWindowTests
{
    static (ProFranchise State,string Team,ProContract Contract) Setup(int year,long priorCash=80_000_000)
    {
        var s=ProLeagueService.Create("Seattle Seahawks");var team=s.League.Teams.First(t=>t.Name!=s.UserTeam).Name;
        var p=s.Players.First(p=>p.Football.TeamName==team&&!p.Retired&&!p.PracticeSquad);
        var c=new ProContract(p.Football.PlayerId,team,year,1,0,[new(year,10_000_000,0)]);
        s=s with{Year=year,SalaryCap=100_000_000,TeamCapAllocations=[],Contracts=[c],Games=[],
            FinancialHistory=year==2030?Enumerable.Range(2027,3).Select(y=>new ProFinanceYear(team,y,100_000_000,0,priorCash,priorCash,0,0)).ToList():[]};
        return(s,team,c);
    }
    [Fact] public void UnknownPreFranchiseCashDoesNotCreateBonusesOrAnAssumedFloorBill()
    {
        var(s,team,c)=Setup(2026);Assert.False(ProFinance.HasFloorHistory(s,team));
        Assert.Same(s,ProFinance.CpuBonuses(s,team));var closed=ProFinance.CloseYear(s);
        var year=closed.FinancialHistory.Single(h=>h.Team==team);Assert.Equal(0,year.FloorPayment);Assert.Equal(90_000_000,year.CarryOut);
        Assert.Equal(c,closed.Contracts.Single());
    }
    [Fact] public void FullyTrackedWindowFundsActualCashGapThenPaysOnlyTheResidualOutsideCap()
    {
        var(s,team,_)=Setup(2030);Assert.True(ProFinance.HasFloorHistory(s,team));var bonus=ProFinance.CpuBonuses(s,team);
        Assert.Equal(100_000_000,ProFinance.Cash(bonus,team,2030));Assert.Equal(0,ProEconomy.Room(bonus,team));
        var closed=ProFinance.CloseYear(bonus);var year=closed.FinancialHistory.Single(h=>h.Team==team&&h.Year==2030);
        Assert.Equal(20_000_000,year.FloorPayment);Assert.Equal(100_000_000,year.CapSpent);
        Assert.Equal(0,ProEconomy.Room(closed,team));Assert.Equal(360_000_000,ProFinance.Floor(closed,team).Required);
    }
    [Fact] public void AlreadySatisfiedWindowDoesNotBurnRolloverJustToReachAnAnnualCapTarget()
    {
        var(s,team,c)=Setup(2030,130_000_000);Assert.Same(s,ProFinance.CpuBonuses(s,team));
        var closed=ProFinance.CloseYear(s);var year=closed.FinancialHistory.Single(h=>h.Team==team&&h.Year==2030);
        Assert.Equal(0,year.FloorPayment);Assert.Equal(90_000_000,year.CarryOut);Assert.Equal(c,closed.Contracts.Single());
        Assert.Equal(closed.FinancialHistory.Count,ProFinance.CloseYear(closed).FinancialHistory.Count);
    }
    [Fact] public void KnownFutureBillsReservePlannedRolloverWithoutBookingInventedCapCharges()
    {
        var(s,team,c)=Setup(2026);s=s with{SalaryCap=ProFinance.Base2026,Contracts=[c with{Years=[new(2026,10_000_000,0),new(2028,300_000_000,0)]}]};
        var room=ProEconomy.Room(s,team);var contracts=s.Contracts;var debt=s.DeadCap;
        Assert.True(ProCpuBudget.FutureRolloverNeed(s,team)>0);
        Assert.Equal(room,ProEconomy.Room(s,team));Assert.Same(contracts,s.Contracts);Assert.Same(debt,s.DeadCap);Assert.Empty(s.RolloverElections);
    }
}
