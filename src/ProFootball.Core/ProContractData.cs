using System.Reflection;
using System.Text.Json;
namespace ProFootball.Core;

/// <summary>Reviewed source terms and guarded migration of unchanged source contracts.</summary>
public static class ProContractData
{
    static readonly Lazy<Dictionary<string, ProPlayer>> RookieMetadata = new(() => ProLeagueService.Snapshot().Players.Where(p=>!string.IsNullOrEmpty(p.ExternalId)).DistinctBy(p=>p.ExternalId).ToDictionary(p=>p.ExternalId));
    sealed record ReviewedContract(Guid PlayerId,int SignedYear,string Team,List<ProBonusPayment> BonusPayments,List<ProCashVesting> CashVesting,Dictionary<int,long> GuaranteedOtherCash,Dictionary<int,long>? GuaranteedBase=null,Dictionary<int,long>? OtherCash=null,Dictionary<int,long>? BonusCash=null,ProContract? ExpectedContract=null);
    sealed record ReviewPack(int Version,string SourcePackSha256,List<ReviewedContract> Contracts);
    static readonly Lazy<ReviewPack> Reviews=new(()=>{
        using var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("ProFootball.Core.pro-reviewed-contract-terms.json")!;
        return JsonSerializer.Deserialize<ReviewPack>(stream)!;
    });
    public static IReadOnlyList<ProContract> Load()
    {
        using var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("ProFootball.Core.nfl-contracts.json")!;
        using var bytes=new MemoryStream();stream.CopyTo(bytes);var data=bytes.ToArray();
        var source=JsonSerializer.Deserialize<List<ProContract>>(data)!;
        var pack=Reviews.Value;
        if(pack.Version!=1||!Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(data)).Equals(pack.SourcePackSha256,StringComparison.Ordinal))return source;
        var reviews=pack.Contracts.ToDictionary(r=>r.PlayerId);
        return source.Select(c=>reviews.TryGetValue(c.PlayerId,out var r)&&c.SignedYear==r.SignedYear&&c.Team==r.Team
            ?c with{BonusPayments=r.BonusPayments,CashVesting=r.CashVesting,BonusCash=r.BonusCash??c.BonusCash,Years=c.Years.Select(y=>y with{Guaranteed=r.GuaranteedBase?.GetValueOrDefault(y.Year,y.Guaranteed)??y.Guaranteed,OtherCash=r.OtherCash?.GetValueOrDefault(y.Year,y.OtherCash??0)??y.OtherCash,GuaranteedOtherCash=r.GuaranteedOtherCash.GetValueOrDefault(y.Year)}).ToList()}:c).ToArray();
    }
    public static ProFranchise Apply(ProFranchise state)
    {
        if (state.Year != 2026) return state;
        var source = Load().ToDictionary(c => c.PlayerId);
        return CompleteRookieTerms(state with { Contracts = state.Contracts.Select(c => source.TryGetValue(c.PlayerId, out var actual) && actual.Team == c.Team ? actual : c).ToList() });
    }
    static bool SameFinancialBasis(ProContract c,ProContract basis)=>c.Team==basis.Team&&c.SignedYear==basis.SignedYear&&c.BonusYears==basis.BonusYears&&c.SigningBonus==basis.SigningBonus&&c.Years.Count==basis.Years.Count
        &&c.Years.All(y=>basis.Years.FirstOrDefault(r=>r.Year==y.Year) is {} r&&r.Salary==y.Salary&&r.Guaranteed==y.Guaranteed&&r.SourceGuaranteed==y.SourceGuaranteed&&r.OtherCash==y.OtherCash&&r.OtherCompensation==y.OtherCompensation&&r.ProratedBonus==y.ProratedBonus&&r.IsVoid==y.IsVoid)
        &&(c.BonusCash??[]).OrderBy(x=>x.Key).SequenceEqual((basis.BonusCash??[]).OrderBy(x=>x.Key));
    public static ProFranchise MigrateAccounting(ProFranchise state)
    {
        var source=Load().ToDictionary(c=>c.PlayerId);var baselines=Reviews.Value.Contracts.Where(r=>r.ExpectedContract is not null).ToDictionary(r=>r.PlayerId,r=>r.ExpectedContract!);
        ProContract Upgrade(ProContract c)
        {
            if(c.AccountingVersion==1&&c.BonusPayments is null&&!c.Estimated&&c.Active&&c.LastChangedYear==0&&c.Source?.StartsWith("OverTheCap")==true&&source.TryGetValue(c.PlayerId,out var reviewed)&&reviewed.BonusPayments is not null&&reviewed.Team==c.Team&&reviewed.SignedYear==c.SignedYear&&(SameFinancialBasis(c,reviewed)||baselines.TryGetValue(c.PlayerId,out var baseline)&&SameFinancialBasis(c,baseline)))
                return c with{BonusPayments=reviewed.BonusPayments,CashVesting=reviewed.CashVesting,BonusCash=reviewed.BonusCash,Years=reviewed.Years};
            if(c.AccountingVersion!=0||c.Estimated||!c.Active||c.LastChangedYear!=0||c.BonusCash is not null||c.Source?.StartsWith("OverTheCap")!=true||!source.TryGetValue(c.PlayerId,out var actual)||actual.Team!=c.Team||actual.Years.Count!=c.Years.Count)return c;
            if(c.Years.Any(y=>actual.Years.FirstOrDefault(a=>a.Year==y.Year) is not {} row||ProEconomy.CapHit(c,y.Year)!=ProEconomy.CapHit(actual,y.Year)||(!row.IsVoid&&row.Salary!=y.Salary)))return c;
            return actual;
        }
        return state with{Contracts=state.Contracts.Select(Upgrade).ToList(),Waivers=state.Waivers.Select(w=>w.OriginalContracts is {} contracts?w with{OriginalContracts=contracts.Select(Upgrade).ToList()}:w).ToList()};
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
        return state with{Players=players,Contracts=contracts.Select(c=>byId.TryGetValue(c.PlayerId,out var p)&&p.RookieYear is int r&&p.DraftOverall is >0&&c.SignedYear==r&&ProContractInsights.PlayingEnd(c)<=r+4?c with{OriginalRookieYear=c.OriginalRookieYear??r}:c).ToList()};
    }
}
