using System.Reflection;
using System.Text.Json;
namespace ProFootball.Core;

/// <summary>Source snapshot only applies to newly created real-team franchises.</summary>
public static class ProContractData
{
    static readonly Lazy<Dictionary<string, ProPlayer>> RookieMetadata = new(() => ProLeagueService.Snapshot().Players.Where(p=>!string.IsNullOrEmpty(p.ExternalId)).DistinctBy(p=>p.ExternalId).ToDictionary(p=>p.ExternalId));
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
    public static ProFranchise MigrateAccounting(ProFranchise state)
    {
        var source=Load().ToDictionary(c=>c.PlayerId);
        return state with { Contracts=state.Contracts.Select(c=>{
            if(c.AccountingVersion!=0||c.Estimated||!c.Active||c.LastChangedYear!=0||c.BonusCash is not null||c.Source?.StartsWith("OverTheCap")!=true||!source.TryGetValue(c.PlayerId,out var actual)||actual.Team!=c.Team||actual.Years.Count!=c.Years.Count)return c;
            if(c.Years.Any(y=>actual.Years.FirstOrDefault(a=>a.Year==y.Year) is not {} row||ProEconomy.CapHit(c,y.Year)!=ProEconomy.CapHit(actual,y.Year)||(!row.IsVoid&&row.Salary!=y.Salary)))return c;
            return actual;
        }).ToList() };
    }
    public static ProFranchise CompleteRookieTerms(ProFranchise state)
    {
        var metadata=RookieMetadata.Value;
        var players=state.Players.Select(p=>p.RookieYear is null&&metadata.TryGetValue(p.ExternalId,out var source)?p with{RookieYear=source.RookieYear,DraftOverall=source.DraftOverall}:p).ToList();
        var byId=players.ToDictionary(p=>p.Football.PlayerId);
        var contracts=state.Contracts.Select(c=>{
            var p=byId.GetValueOrDefault(c.PlayerId);
            if(p?.PracticeSquad==true||!c.Active||c.LastChangedYear!=0||(!c.Estimated&&c.Source?.StartsWith("OverTheCap")!=true)||p?.RookieYear is not int rookie||state.Year<rookie)return c;
            if(c.AccountingVersion>0&&!c.Estimated&&c.SignedYear!=rookie)return c; // A later deal may follow a real release or extension.
            var end=rookie+(p.DraftOverall is >0?4:3)-1;
            if(end<state.Year||ProContractInsights.PlayingEnd(c)>=end)return c;
            var current=c.Years.SingleOrDefault(y=>y.Year==state.Year);if(current is null)return c;
            var years=c.Years.Select(y=>y with{ProratedBonus=ProEconomy.BonusCharge(c,y.Year)}).Concat(Enumerable.Range(state.Year+1,end-state.Year).Where(year=>!c.Years.Any(y=>y.Year==year)).Select(year=>new ContractYear(year,current.Salary,0){ProratedBonus=c.Estimated?0:current.ProratedBonus??0})).OrderBy(y=>y.Year).ToList();
            return c with{Years=years,BonusYears=Math.Max(c.BonusYears,end-c.SignedYear+1),SigningBonus=c.AccountingVersion>0?c.SigningBonus:years.Sum(y=>y.ProratedBonus??0),FutureTermsEstimated=true};
        }).ToList();
        return state with{Players=players,Contracts=contracts};
    }
}
