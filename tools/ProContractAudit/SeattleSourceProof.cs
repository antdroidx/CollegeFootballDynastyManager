using System.Text.Json.Nodes;
using ProFootball.Core;
public static class SeattleSourceProof
{
    public static object Create(ProFranchise state)
    {
        var ledger=JsonNode.Parse(File.ReadAllText("docs/data/pro-opening-ledger-followup-2026-10-27.json"))!["teams"]!.AsArray().Single(t=>t!["team"]!.GetValue<string>()=="SEA")!;
        var reviews=JsonNode.Parse(File.ReadAllText("docs/data/pro-roster-status-review-2026-10-27.json"))!["reviews"]!.AsArray();
        var foster=Guid.Parse(reviews.Single(r=>r!["player"]!.GetValue<string>().StartsWith("Montorie"))!["playerId"]!.GetValue<string>());
        var maranges=Guid.Parse(reviews.Single(r=>r!["player"]!.GetValue<string>().StartsWith("Federico"))!["playerId"]!.GetValue<string>());
        var opening=JsonNode.Parse(File.ReadAllText(".artifacts/pro-opening-contract-page-evidence-fixed.json"))!["pages"]!.AsArray();
        var status=JsonNode.Parse(File.ReadAllText(".artifacts/pro-status-contract-page-evidence.json"))!["pages"]!.AsArray();
        var before=ProFinance.Budget(state with{Phase=ProPhase.RegularSeason},state.UserTeam);
        var prototype=state with{Contracts=state.Contracts.Where(c=>c.PlayerId!=foster).ToList(),Players=state.Players.Select(p=>p.Football.PlayerId==foster?p with{Football=p.Football with{TeamName="Free agent"},Reserve=false,PracticeSquad=false}:p).ToList()};
        var pp=prototype.Contracts.Single(c=>c.PlayerId==maranges);
        // In-memory illustration of the verified international salary exclusion only.
        // Production should use a typed exemption and preserve unknown cash as unknown.
        prototype=prototype with{Contracts=prototype.Contracts.Select(c=>c==pp?c with{Years=c.Years.Select(y=>y.Year==2026?y with{OtherCompensation=-y.Salary-ProEconomy.BonusCharge(c,2026)}:y).ToList()}:c).ToList()};
        ProContract CurrentClub(int otc,JsonArray pages,bool practice)
        {
            var row=ledger["currentRows"]!.AsArray().Single(r=>r!["otcId"]?.GetValue<int>()==otc)!;
            var id=Guid.Parse(row["playerId"]!.GetValue<string>());var page=pages.Single(p=>p!["otcId"]!.GetValue<int>()==otc)!;
            var years=page["current"]!.AsArray().Where(y=>y!["year"]!.GetValue<int>()>=2026).Select(y=>new ContractYear(y!["year"]!.GetValue<int>(),y["salary"]!.GetValue<long>(),Math.Min(y["salary"]!.GetValue<long>(),y["reportedGuaranteedCash"]?.GetValue<long>()??0)){ProratedBonus=(y["signingProration"]?.GetValue<long>()??0)+(y["optionProration"]?.GetValue<long>()??0),SourceGuaranteed=y["reportedGuaranteedCash"]?.GetValue<long>(),OtherCash=0,OtherCompensation=y["cap"]!.GetValue<long>()-y["salary"]!.GetValue<long>()}).ToList();
            if(years.Any(y=>y.ProratedBonus!=0)||page["current"]!.AsArray().Any(y=>y!["year"]!.GetValue<int>()>=2026&&y["preJune1TradeDead"]?.GetValue<long>()!=0))throw new InvalidDataException("Cannot exclude a current-club paid bonus");
            var c=new ProContract(id,state.UserTeam,2026,years.Max(y=>y.Year)-2026+1,0,years){AccountingVersion=1,BonusCash=years.ToDictionary(y=>y.Year,_=>0L),BonusPayments=[],Estimated=false,Source="Reviewed current-club source proof"};
            if(ProEconomy.CapHit(c,2026)!=row["reportedCap"]!.GetValue<long>())throw new InvalidDataException("Current-club charge differs from source row");
            prototype=prototype with{Contracts=prototype.Contracts.Where(x=>x.PlayerId!=id).Append(c).ToList(),Players=prototype.Players.Select(p=>p.Football.PlayerId==id?p with{Football=p.Football with{TeamName=state.UserTeam},PracticeSquad=practice,Reserve=false}:p).ToList()};return c;
        }
        var avery=CurrentClub(13599,opening,false);var diggs=CurrentClub(8791,status,true);
        var debt=JsonNode.Parse(File.ReadAllText("docs/data/pro-opening-dead-cap-2026-10-27.json"))!["teams"]!.AsArray().Single(t=>t!["team"]!.GetValue<string>()=="SEA")!;
        prototype=prototype with{DeadCap=debt["entries"]!.AsArray().Select(d=>new DeadCapCharge(state.UserTeam,2026,d!["playerId"] is null?Guid.Empty:Guid.Parse(d["playerId"]!.GetValue<string>()),d["amount"]!.GetValue<long>())).ToList()};
        var after=ProFinance.Budget(prototype with{Phase=ProPhase.RegularSeason},state.UserTeam);var reported=ledger["reportedLiabilities"]!.GetValue<long>();
        return new{scope="Read-only in-memory source proof, not application data or a save migration. No reported room is an input; allowance remains NFLPA-based. Uses the verified waiver/international designation, current-club salary tables/zero trade-bonus assessments, and the separately reported debt ledger. Unknown international cash remains the original generated estimate; only the verified cap exclusion is illustrated. Production needs typed exemption/status rules and safe new-franchise initialization.",before,after,reportedLiabilities=reported,difference=after.TotalCommitments-reported,exactCurrentCap=after.TotalCommitments==reported,averyCurrentClubCash=ProFinance.Cash(prototype with{Contracts=[avery],DeadCap=[]},state.UserTeam,2026),diggsAnnualSalaryRate=diggs.Years.Single().Salary,diggsReportedCap=ProEconomy.CapHit(diggs,2026)};
    }
}
