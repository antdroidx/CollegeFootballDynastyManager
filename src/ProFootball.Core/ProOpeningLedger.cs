using System.Reflection;
using System.Text.Json;
namespace ProFootball.Core;

/// <summary>Observed opening cap assessments. Never run as a save migration or infer historical cash.</summary>
public static class ProOpeningLedger
{
    sealed record Entry(string Team,int Year,Guid? PlayerId,long Amount,string SourceKey,string SourceName,string SourceKind,string SourceUrl,string SourceSha256,string ObservedOn);
    sealed record Pack(int Version,string ObservedOn,List<Entry> Entries);
    static readonly Lazy<Pack> Source=new(()=>{
        using var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("ProFootball.Core.pro-opening-ledger.json")!;
        var pack=JsonSerializer.Deserialize<Pack>(stream)??throw new InvalidDataException("Missing opening ledger");
        if(pack.Version!=1||pack.ObservedOn!="2026-10-27"||pack.Entries.Select(e=>e.Team).Distinct().Count()!=32
            ||pack.Entries.Any(e=>e.Year!=2026||e.Amount<0||e.ObservedOn!=pack.ObservedOn||e.SourceSha256.Length!=64)
            ||pack.Entries.Select(e=>e.SourceKey).Distinct().Count()!=pack.Entries.Count)
            throw new InvalidDataException("Invalid dated opening ledger");
        return pack;
    });
    // Only the new-franchise factory calls this. Existing saves retain every booked assessment.
    internal static ProFranchise InitializeNew(ProFranchise s)
    {
        if(s.Year!=2026||s.DeadCap.Count!=0||s.FinancialHistory.Count!=0||s.History.Count!=0
            ||s.Games.Any(g=>g.Football.HasPlayed)||s.Contracts.Any(c=>c.LastChangedYear!=0||!c.Active))
            throw new InvalidOperationException("Opening assessments require a new untouched2026 franchise.");
        var teams=s.League.Teams.ToDictionary(t=>t.Abbreviation,t=>t.Name);
        if(!teams.Keys.ToHashSet().SetEquals(Source.Value.Entries.Select(e=>e.Team)))
            throw new InvalidDataException("Opening ledger does not match the league");
        return s with{OpeningFinancialSnapshot=Source.Value.ObservedOn,DeadCap=Source.Value.Entries.Select(e=>new DeadCapCharge(teams[e.Team],e.Year,e.PlayerId??Guid.Empty,e.Amount){
            SourceKey=e.SourceKey,SourceName=e.SourceName,SourceKind=e.SourceKind,SourceUrl=e.SourceUrl,SourceSha256=e.SourceSha256,ObservedOn=e.ObservedOn
        }).ToList()};
    }
}
