using System.Text.Json.Nodes;
using ProFootball.Core;
public static class TerminationAudit
{
    public static object Create(ProFranchise state,string pagesFile,string identitiesFile)
    {
        var pages=JsonNode.Parse(File.ReadAllText(pagesFile))!["pages"]!.AsArray();
        var identities=JsonNode.Parse(File.ReadAllText(identitiesFile))!["contracts"]!.AsArray().Where(n=>n?["otcId"] is not null).ToDictionary(n=>n!["otcId"]!.GetValue<int>(),n=>n!);
        var source=ProContractData.Load().ToDictionary(c=>c.PlayerId);
        var results=new List<object>();var compared=0;var exact=0;
        foreach(var page in pages)
        {
            var otc=page!["otcId"]!.GetValue<int>();if(!identities.TryGetValue(otc,out var identity))continue;
            var id=Guid.Parse(identity["playerId"]!.GetValue<string>());if(!source.TryGetValue(id,out var contract))continue;
            foreach(var row in page["current"]!.AsArray().Where(n=>n!["year"]!.GetValue<int>()>=state.Year&&n["year"]!.GetValue<int>()<=2030&&n["preJune1CutDead"] is not null))
            {
                var year=row!["year"]!.GetValue<int>();var recorded=row["cap"]?.GetValue<long>();
                var pre=state with{Year=year,Phase=ProPhase.Offseason,OffseasonStep=OffseasonStep.FreeAgency};
                var post=pre with{Phase=ProPhase.Preseason};
                var preAmount=ProTermination.Charges(pre,contract,Guid.Empty).Where(d=>d.Year==year).Sum(d=>d.Amount);
                var postAmount=ProTermination.Charges(post,contract,Guid.Empty).Where(d=>d.Year==year).Sum(d=>d.Amount);
                var preSource=row["preJune1CutDead"]!.GetValue<long>();var postSource=row["postJune1CutDead"]?.GetValue<long>();
                var sameCap=recorded==ProEconomy.CapHit(contract,year);var sameCut=preAmount==preSource&&postAmount==postSource;
                if(year==state.Year&&sameCap){compared++;if(sameCut)exact++;}
                results.Add(new{player=identity["player"]?.GetValue<string>(),id,otcId=otc,year,sourceUrl=page["sourceUrl"]?.GetValue<string>(),sourceSha256=page["sha256"]?.GetValue<string>(),sourceObservedOn=page["observedOn"]?.GetValue<string>(),sameAnnualCap=sameCap,sourcePreJune1Cut=preSource,gamePreJune1Cut=preAmount,sourcePostJune1Cut=postSource,gamePostJune1Cut=postAmount,sameCut,reviewedPayments=contract.BonusPayments is not null});
            }
        }
        return new{scope="Developer comparison only. Published future cut figures are forecasts under source-specific option/guarantee assumptions; game pre-June1 uses free agency and post-June1 uses preseason payment status. Differences require source/payment-date review, never a balancing override or guessed cash liability. No game data changed.",currentSameCapCases=compared,currentExactCutCases=exact,currentCutReviewCases=compared-exact,rows=results};
    }
}
