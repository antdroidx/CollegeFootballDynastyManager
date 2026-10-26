using System.Text.Json;
using ProFootball.Core;
var state=ProLeagueService.Create("Seattle Seahawks",sourcedContracts:true);
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
        caveat="Seasonal game budget; excluded camp salary is separate. Not reconciled to dated source liabilities or departed-player dead money."
    };
}).ToArray();
var json=JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true,PropertyNamingPolicy=JsonNamingPolicy.CamelCase});
if(args.Length==0)Console.WriteLine(json);else File.WriteAllText(args[0],json);
