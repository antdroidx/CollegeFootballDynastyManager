using System.Reflection;
using System.Text.Json;
namespace ProFootball.Core;

/// <summary>Source snapshot only applies to newly created real-team franchises.</summary>
public static class ProContractData
{
    public static IReadOnlyList<ProContract> Load()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ProFootball.Core.nfl-contracts.json")!;
        return JsonSerializer.Deserialize<List<ProContract>>(stream)!;
    }
    public static ProFranchise Apply(ProFranchise state)
    {
        if (state.Year != 2026) return state;
        var source = Load().ToDictionary(c => c.PlayerId);
        return CompleteRookieTerms(state with { Contracts = state.Contracts.Select(c => source.TryGetValue(c.PlayerId, out var actual) && actual.Team == c.Team ? actual : c).ToList() });
    }
    public static ProFranchise CompleteRookieTerms(ProFranchise state)
    {
        var metadata=ProLeagueService.Snapshot().Players.Where(p=>!string.IsNullOrEmpty(p.ExternalId)).DistinctBy(p=>p.ExternalId).ToDictionary(p=>p.ExternalId);
        var players=state.Players.Select(p=>p.RookieYear is null&&metadata.TryGetValue(p.ExternalId,out var source)?p with{RookieYear=source.RookieYear,DraftOverall=source.DraftOverall}:p).ToList();
        var contracts=state.Contracts.Select(c=>{
            var p=players.FirstOrDefault(p=>p.Football.PlayerId==c.PlayerId);
            if(p?.PracticeSquad==true||!c.Active||c.LastChangedYear!=0||(!c.Estimated&&c.Source?.StartsWith("OverTheCap")!=true)||p?.RookieYear is not int rookie||state.Year<rookie)return c;
            var end=rookie+(p.DraftOverall is >0?4:3)-1;
            if(end<state.Year||c.Years.Max(y=>y.Year)>=end)return c;
            var current=c.Years.SingleOrDefault(y=>y.Year==state.Year);if(current is null)return c;
            var years=c.Years.Concat(Enumerable.Range(state.Year+1,end-state.Year).Where(year=>!c.Years.Any(y=>y.Year==year)).Select(year=>new ContractYear(year,current.Salary,0){ProratedBonus=current.ProratedBonus??0})).OrderBy(y=>y.Year).ToList();
            return c with{Years=years,BonusYears=Math.Max(c.BonusYears,end-c.SignedYear+1),SigningBonus=years.Sum(y=>y.ProratedBonus??0),FutureTermsEstimated=true};
        }).ToList();
        return state with{Players=players,Contracts=contracts};
    }
}
