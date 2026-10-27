using System.Text.Json.Nodes;
using ProFootball.Core;
public static class SeattleSourceProof
{
    public static object Create(ProFranchise state)
    {
        var ledger=JsonNode.Parse(File.ReadAllText("docs/data/pro-opening-ledger-followup-2026-10-27.json"))!["teams"]!.AsArray().Single(t=>t!["team"]!.GetValue<string>()=="SEA")!;
        var budget=ProFinance.Budget(state with{Phase=ProPhase.RegularSeason},state.UserTeam);
        var reported=ledger["reportedLiabilities"]!.GetValue<long>();
        var avery=state.Contracts.Single(c=>c.PlayerId==state.Players.Single(p=>p.Football.FullName=="Avery Smith").Football.PlayerId);
        var diggs=state.Contracts.Single(c=>c.PlayerId==state.Players.Single(p=>p.Football.FullName=="Trevon Diggs").Football.PlayerId);
        return new{scope="Actual new sourced-franchise initialization. No reported room is an input; allowance remains NFLPA-based. Verified roster events/current-club salary tables, typed international exemption and independently reported opening debt reconcile Seattle. Unknown international pay retains its generated estimate; other-team current-contract/guarantee review remains open, so sourced default is not promoted. Existing saves are not initialized or overwritten.",budget,reportedLiabilities=reported,difference=budget.TotalCommitments-reported,exactCurrentCap=budget.TotalCommitments==reported,averyCurrentClubCash=ProFinance.Cash(state with{Contracts=[avery],DeadCap=[]},state.UserTeam,2026),diggsAnnualSalaryRate=diggs.Years.Single().Salary,diggsReportedCap=ProEconomy.CapHit(diggs,2026)};
    }
}
