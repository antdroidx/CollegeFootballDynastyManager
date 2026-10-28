using System.Reflection;
using System.Text.Json;
using DynastyManager.Core.Models;
namespace ProFootball.Core;

public sealed record ProCompensatoryCandidate(int MarketYear,Guid PlayerId,string FormerTeam,int Seasons) {public bool Eligible {get;init;}=true;}
public sealed record ProCompensatorySigning(int MarketYear,Guid PlayerId,string FormerTeam,string NewTeam,long AnnualValue,int Seasons,Position Position) {public bool Eligible {get;init;}=true;}
public sealed record ProCompensatoryProjection(int Year,int Round,string Team,string Reason,bool Special,int Priority,string Source) {public Guid? PlayerId {get;init;} }

/// <summary>Transparent game approximation of Appendix V; not the NFL Management Council's private calculation.</summary>
public static class ProCompensatoryDraft
{
    sealed record Pack(int Version,string ObservedOn,List<ProCompensatoryProjection> Projections);
    public static ProFranchise InitializeNew(ProFranchise s)
    {
        using var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("ProFootball.Core.pro-compensatory-projections.json")!;
        var pack=JsonSerializer.Deserialize<Pack>(stream)!;
        if(pack.Version!=1||pack.Projections.Any(p=>p.Round is <3 or >7))throw new InvalidDataException("Invalid compensatory baseline");
        var clubs=s.League.Teams.ToDictionary(t=>t.Abbreviation,t=>t.Name);
        return s with{CompensatoryProjections=pack.Projections.Select(p=>p with{Team=clubs[p.Team]}).ToList()};
    }
    internal static ProFranchise Expired(ProFranchise s,ProPlayer p,string formerTeam)
    {
        var accrued=p.CreditedSeasons??p.Experience;
        var played=s.Games.Count(g=>g.Phase==ProPhase.RegularSeason&&g.Football.HasPlayed&&g.Football.PlayerStats.Any(l=>l.PlayerId==p.Football.PlayerId));
        var seasons=accrued+(played>=3?1:0);
        if(p.PracticeSquad||p.Retired||seasons<4)return s;
        var item=new ProCompensatoryCandidate(s.Year+1,p.Football.PlayerId,formerTeam,seasons);
        return s with{CompensatoryCandidates=s.CompensatoryCandidates.Where(c=>c.MarketYear!=item.MarketYear||c.PlayerId!=item.PlayerId).Append(item).ToList()};
    }
    internal static ProFranchise Signed(ProFranchise before,ProFranchise after,ProPlayer player,ProContract c,bool rookie)
    {
        if(rookie||before.Phase!=ProPhase.Offseason||before.OffseasonStep is not (OffseasonStep.FreeAgency or OffseasonStep.Draft))return after;
        var candidate=before.CompensatoryCandidates.LastOrDefault(x=>x.MarketYear==before.Year&&x.PlayerId==c.PlayerId&&x.Eligible);
        if(candidate is null||candidate.FormerTeam==c.Team||before.CompensatorySignings.Any(x=>x.MarketYear==before.Year&&x.PlayerId==c.PlayerId))return after;
        var annual=(c.Years.Where(y=>!y.IsVoid).Sum(y=>y.Salary)+c.SigningBonus)/Math.Max(1,c.Years.Count(y=>!y.IsVoid));
        return after with{CompensatorySignings=after.CompensatorySignings.Append(new(before.Year,c.PlayerId,candidate.FormerTeam,c.Team,annual,candidate.Seasons,player.Football.Position)).ToList()};
    }
    internal static ProFranchise Released(ProFranchise s,Guid playerId)
    {
        // A pre-midseason release does not remain a qualifying signed CFA. Later releases retain the earned credit.
        if(s.Phase==ProPhase.Playoffs||s.Phase==ProPhase.RegularSeason&&s.Week>=10)return s;
        return s with{CompensatorySignings=s.CompensatorySignings.Select(x=>x.MarketYear==s.Year&&x.PlayerId==playerId?x with{Eligible=false}:x).ToList(),
            CompensatoryCandidates=s.CompensatoryCandidates.Select(x=>x.MarketYear==s.Year&&x.PlayerId==playerId?x with{Eligible=false}:x).ToList()};
    }
    public static IReadOnlyList<ProCompensatoryProjection> Project(ProFranchise s,int draftYear)
    {
        var opening=s.CompensatoryProjections.Where(p=>p.Year==draftYear).ToArray();
        if(opening.Any(p=>!p.Special))return opening.OrderBy(p=>p.Special).ThenBy(p=>p.Priority).ToArray();
        var events=s.CompensatorySignings.Where(x=>x.MarketYear==draftYear-1&&x.Eligible).ToArray();
        var salaries=s.Contracts.Where(c=>c.Years.Any(y=>y.Year==draftYear-1&&!y.IsVoid)).GroupBy(c=>c.PlayerId)
            .Select(g=>g.OrderByDescending(c=>c.SignedYear).First()).Select(c=>ProContractInsights.AverageCash(c)).Where(v=>v>0).OrderDescending().ToArray();
        int Round(ProCompensatorySigning e)
        {
            var percentile=salaries.Length==0?1:(salaries.Count(v=>v>e.AnnualValue)+.5)/salaries.Length;
            // Public contract-percentile bands. Full snap counts and the private NFL scoring model are not inferred.
            var round=percentile<=.05?3:percentile<=.10?4:percentile<=.15?5:percentile<=.25?6:percentile<=.35?7:0;
            return round>0&&e.Seasons>=10&&e.Position!=Position.QB?Math.Max(5,round):round;
        }
        var qualified=events.Select(e=>(Event:e,Round:Round(e))).Where(x=>x.Round>0).ToArray();var awards=new List<(ProCompensatoryProjection Award,long Annual)>();
        foreach(var club in s.League.Teams)
        {
            var lost=qualified.Where(x=>x.Event.FormerTeam==club.Name).OrderBy(x=>x.Round).ThenByDescending(x=>x.Event.AnnualValue).ToList();
            foreach(var gain in qualified.Where(x=>x.Event.NewTeam==club.Name).OrderBy(x=>x.Round).ThenByDescending(x=>x.Event.AnnualValue))
            {
                var index=lost.FindIndex(x=>x.Round==gain.Round);if(index<0)index=lost.FindIndex(x=>x.Round>gain.Round);if(index<0&&lost.Count>0)index=0;
                if(index>=0)lost.RemoveAt(index);
            }
            foreach(var loss in lost.Take(4))
            {
                var name=s.Players.FirstOrDefault(p=>p.Football.PlayerId==loss.Event.PlayerId)?.Football.FullName??"Departed free agent";
                awards.Add((new ProCompensatoryProjection(draftYear,loss.Round,club.Name,name,false,0,"Simulated free-agent compensation"){PlayerId=loss.Event.PlayerId},loss.Event.AnnualValue));
            }
        }
        // Strongest remaining values earn the league's 32 slots. Stable identity resolves equal values.
        return awards.OrderBy(p=>p.Award.Round).ThenByDescending(p=>p.Annual)
            .ThenBy(p=>p.Award.Team,StringComparer.Ordinal).ThenBy(p=>p.Award.Reason,StringComparer.Ordinal).Take(32).Select((p,i)=>p.Award with{Priority=i}).Concat(opening.Where(p=>p.Special)).ToArray();
    }
    public static ProFranchise Award(ProFranchise s)
    {
        if(s.CompensatoryAwardYears.Contains(s.Year))return s;
        if(s.Phase!=ProPhase.Offseason||s.DraftIndex!=0||s.DraftHistory.Any(h=>h.Year==s.Year))return s;
        var awards=Project(s,s.Year);var picks=s.Picks.ToList();
        foreach(var group in awards.GroupBy(a=>a.Round))
        {
            var slot=32;
            foreach(var award in group.OrderBy(a=>a.Special).ThenBy(a=>a.Priority).ThenBy(a=>a.Team,StringComparer.Ordinal))
            {
                var id=new Guid(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"comp-{s.Year}-{award.Team}-{award.Round}-{award.Special}-{award.PlayerId?.ToString()??award.Reason}"))[..16]);
                slot++;
                if(picks.All(p=>p.Id!=id))picks.Add(new(id,s.Year,award.Round,slot,award.Team,award.Team){Compensatory=true,SpecialCompensatory=award.Special,CompensationFor=award.Reason});
            }
        }
        var next=s with{Picks=picks,CompensatoryAwardYears=s.CompensatoryAwardYears.Append(s.Year).ToList()};
        return awards.Count==0?next:ProEconomy.Log(next,$"{s.Year} compensatory selections allocated: {awards.Count}. See Draft picks for team awards.");
    }
}
