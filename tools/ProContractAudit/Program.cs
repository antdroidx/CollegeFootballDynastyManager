using System.Text.Json;
using ProFootball.Core;
var state=ProLeagueService.Create("Seattle Seahawks",sourcedContracts:true);
if(args.FirstOrDefault()=="--roquan-guarantee-proof")
{
    if(args.Length!=2)throw new ArgumentException("--roquan-guarantee-proof OUTPUT");
    File.WriteAllText(args[1],JsonSerializer.Serialize(RoquanGuaranteeProof.Create(state),new JsonSerializerOptions{WriteIndented=true}));return;
}
if(args.FirstOrDefault()=="--opening-reconciliation")
{
    if(args.Length!=2)throw new ArgumentException("--opening-reconciliation OUTPUT");
    var observed=System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText("docs/data/pro-opening-ledger-followup-2026-10-27.json"))!["teams"]!.AsArray().ToDictionary(t=>t!["team"]!.GetValue<string>(),t=>t!);
    var rows=state.League.Teams.Select(team=>{
        var b=ProFinance.Budget(state with{Phase=ProPhase.RegularSeason},team.Name);
        var source=observed[team.Abbreviation];var expected=source["reportedLiabilities"]!.GetValue<long>();
        return new{team=team.Abbreviation,budget=b,reportedLiabilities=expected,difference=b.TotalCommitments-expected,exact=b.TotalCommitments==expected,
            generatedFallbacks=state.Contracts.Where(c=>c.Active&&c.Team==team.Name&&c.Estimated).Select(c=>new{id=c.PlayerId,name=state.Players.Single(p=>p.Football.PlayerId==c.PlayerId).Football.FullName,cap=ProEconomy.CapHit(c,2026)}),
            observedSourceSha256=source["sourceSha256"]!.GetValue<string>()};
    }).ToArray();
    File.WriteAllText(args[1],JsonSerializer.Serialize(new{scope="Actual new sourced2026 runtime regular-season counting with observed opening debt and reviewed roster/exemption changes. Reported liabilities are comparison evidence only, never a budget or balancing input. Different source allowance bases remain separate. Unresolved contract/status/guarantee review prevents default promotion.",teams=32,exactTeams=rows.Count(r=>r.exact),rows},new JsonSerializerOptions{WriteIndented=true,PropertyNamingPolicy=JsonNamingPolicy.CamelCase}));return;
}
if(args.FirstOrDefault()=="--seattle-source-proof")
{
    if(args.Length!=2)throw new ArgumentException("--seattle-source-proof OUTPUT");
    File.WriteAllText(args[1],JsonSerializer.Serialize(SeattleSourceProof.Create(state),new JsonSerializerOptions{WriteIndented=true,PropertyNamingPolicy=JsonNamingPolicy.CamelCase}));return;
}
if(args.FirstOrDefault()=="--termination-evidence")
{
    if(args.Length is not (4 or 5))throw new ArgumentException("--termination-evidence PARSED_PAGES NOTE_IDENTITIES OUTPUT [REVIEW_CANDIDATES]");
    var terminationReport=TerminationAudit.Create(state,args[1],args[2],args.Length==5?args[4]:null);
    File.WriteAllText(args[3],JsonSerializer.Serialize(terminationReport,new JsonSerializerOptions{WriteIndented=true,PropertyNamingPolicy=JsonNamingPolicy.CamelCase}));return;
}
if(args.FirstOrDefault()=="--market")
{
    var marketReport=MarketAudit.Create(state);
    var marketJson=JsonSerializer.Serialize(marketReport,new JsonSerializerOptions{WriteIndented=true,PropertyNamingPolicy=JsonNamingPolicy.CamelCase});
    if(args.Length<2)Console.WriteLine(marketJson);else File.WriteAllText(args[1],marketJson);
    return;
}
var report=state.League.Teams.Select(team=>{
    var contracts=state.Contracts.Where(c=>c.Active&&c.Team==team.Name).ToArray();
    var source=contracts.Where(c=>!c.Estimated&&c.Source is not null).ToArray();
    return new{
        team=team.Abbreviation,
        budget=ProFinance.Budget(state,team.Name),
        sourceContracts=source.Length,
        generatedFallbacks=contracts.Count(c=>c.Estimated),
        generatedPlayers=contracts.Where(c=>c.Estimated).Select(c=>state.Players.Single(p=>p.Football.PlayerId==c.PlayerId).Football.FullName),
        capOnlyYears=source.Sum(c=>c.Years.Count(y=>y.IsVoid)),
        caveat="Seasonal game budget; excluded camp salary is separate. Dated opening departed debt is initialized separately; current-contract/status/guarantee reconciliation remains incomplete."
    };
}).ToArray();
var json=JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true,PropertyNamingPolicy=JsonNamingPolicy.CamelCase});
if(args.Length==0)Console.WriteLine(json);else File.WriteAllText(args[0],json);
