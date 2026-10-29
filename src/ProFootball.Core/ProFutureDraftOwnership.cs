using System.Reflection;
using System.Text.Json;
namespace ProFootball.Core;

public sealed record ProPickAllocationCondition(int Year,int Round,string FirstOriginal,string SecondOriginal,string EarlierOwner,string LaterOwner,string Source,string Reason) {public bool Resolved {get;init;} }
/// <summary>Dated opening rights, never a save-load migration. Unknown private clauses use the disclosed-round game baseline.</summary>
public static class ProFutureDraftOwnership
{
    sealed record Transfer(int Year,int Round,string OriginalTeam,string Owner,string Source,string? Reservation=null,int MinimumGames=0,string? Player=null,string? FallbackOwner=null);
    sealed record Pack(int Version,string ObservedOn,List<string> Teams,List<int> Years,List<Transfer> Transfers,List<ProPickAllocationCondition> Conditions);
    static readonly Lazy<Pack> Source=new(()=>{
        using var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("ProFootball.Core.pro-future-pick-ownership.json")!;
        var data=JsonSerializer.Deserialize<Pack>(stream)!;
        if(data.Version!=1||data.Teams.Count!=32||data.Teams.Distinct().Count()!=32||!data.Years.SequenceEqual(new[]{2027,2028,2029})
            ||data.Transfers.Select(t=>(t.Year,t.Round,t.OriginalTeam)).Distinct().Count()!=data.Transfers.Count
            ||data.Transfers.Any(t=>!data.Years.Contains(t.Year)||t.Round is <1 or >7||!data.Teams.Contains(t.OriginalTeam)||!data.Teams.Contains(t.Owner)||!Uri.TryCreate(t.Source,UriKind.Absolute,out _)))
            throw new InvalidDataException("Invalid opening draft ownership ledger");
        var keys=data.Conditions.SelectMany(c=>new[]{(c.Year,c.Round,c.FirstOriginal),(c.Year,c.Round,c.SecondOriginal)}).ToArray();
        if(keys.Distinct().Count()!=keys.Length||data.Conditions.Any(c=>!data.Years.Contains(c.Year)||c.Round is <1 or >7||new[]{c.FirstOriginal,c.SecondOriginal,c.EarlierOwner,c.LaterOwner}.Any(t=>!data.Teams.Contains(t))))
            throw new InvalidDataException("Invalid paired draft rights");
        return data;
    });
    internal static ProFranchise InitializeNew(ProFranchise s)
    {
        if(s.OpeningPickSnapshot==Source.Value.ObservedOn)return s;
        if(s.Year!=2026||s.History.Count!=0||s.TradeHistory.Count!=0||s.Games.Any(g=>g.Football.HasPlayed)||s.Contracts.Any(c=>c.LastChangedYear!=0))
            throw new InvalidOperationException("Opening draft rights require a new franchise.");
        var clubs=s.League.Teams.ToDictionary(t=>t.Abbreviation,t=>t.Name);
        if(!clubs.Keys.ToHashSet().SetEquals(Source.Value.Teams))throw new InvalidDataException("Draft ledger club coverage differs");
        var picks=s.Picks.ToList();
        foreach(var t in Source.Value.Transfers)
        {
            var index=picks.FindIndex(p=>!p.Compensatory&&p.Year==t.Year&&p.Round==t.Round&&p.OriginalTeam==clubs[t.OriginalTeam]);
            if(index<0)throw new InvalidDataException("Missing standard opening selection");
            var player=t.Player is null?null:s.Players.SingleOrDefault(p=>p.Football.FullName==t.Player);
            if(t.Player is not null&&player is null)throw new InvalidDataException("Conditional pick player is missing");
            picks[index]=picks[index] with{Owner=clubs[t.Owner],AllocationSource=t.Source,AllocationPending=t.Reservation is not null,AllocationNote=t.Reservation,
                ConditionPlayerId=player?.Football.PlayerId,MinimumGames=t.MinimumGames,ConditionFallbackOwner=t.FallbackOwner is null?null:clubs[t.FallbackOwner]};
        }
        var rules=Source.Value.Conditions.Select(c=>c with{FirstOriginal=clubs[c.FirstOriginal],SecondOriginal=clubs[c.SecondOriginal],EarlierOwner=clubs[c.EarlierOwner],LaterOwner=clubs[c.LaterOwner]}).ToList();
        var next=s with{Picks=picks,PickAllocationConditions=rules,OpeningPickSnapshot=Source.Value.ObservedOn};
        // Forecast provisional ownership by roster/performance strength, never alphabetical order. Both sides remain encumbered.
        foreach(var rule in rules)next=AssignPair(next,rule,false);
        if(next.Picks.Where(p=>!p.Compensatory&&Source.Value.Years.Contains(p.Year)).Select(p=>(p.Year,p.Round,p.OriginalTeam)).Distinct().Count()!=672)
            throw new InvalidDataException("Opening standard inventory must contain 672 unique rights");
        return next;
    }
    static ProFranchise AssignPair(ProFranchise s,ProPickAllocationCondition rule,bool final)
    {
        var members=s.Picks.Where(p=>!p.Compensatory&&p.Year==rule.Year&&p.Round==rule.Round&&(p.OriginalTeam==rule.FirstOriginal||p.OriginalTeam==rule.SecondOriginal)).ToArray();
        if(members.Length!=2)return s; // Partial synthetic/legacy inventories do not justify inventing another asset.
        var ordered=members.OrderBy(p=>final?p.Slot:ProPickProjection.Slot(s,p)).ThenBy(p=>p.Slot).ToArray();
        return s with{Picks=s.Picks.Select(p=>p.Id==ordered[0].Id?p with{Owner=rule.EarlierOwner,AllocationPending=!final,AllocationNote=rule.Reason,AllocationSource=rule.Source}:
            p.Id==ordered[1].Id?p with{Owner=rule.LaterOwner,AllocationPending=!final,AllocationNote=rule.Reason,AllocationSource=rule.Source}:p).ToList()};
    }
    public static ProFranchise ResolveDraftYear(ProFranchise s)
    {
        if(s.Phase!=ProPhase.Offseason||s.DraftIndex!=0||s.DraftHistory.Any(h=>h.Year==s.Year))return s;
        foreach(var rule in s.PickAllocationConditions.Where(c=>c.Year==s.Year&&!c.Resolved).ToArray())
        {
            s=AssignPair(s,rule,true);
            s=s with{PickAllocationConditions=s.PickAllocationConditions.Select(c=>c==rule?c with{Resolved=true}:c).ToList()};
        }
        var season=s.History.LastOrDefault(h=>h.Year==s.Year-1);
        return s with{Picks=s.Picks.Select(p=>{
            if(p.Year!=s.Year||!p.AllocationPending)return p;
            var owner=p.Owner;
            if(p.ConditionPlayerId is Guid id&&p.MinimumGames>0&&season is not null)
            {
                var games=season.Games.Count(g=>g.Phase==ProPhase.RegularSeason&&g.Football.HasPlayed&&g.Football.PlayerStats.Any(line=>line.PlayerId==id));
                if(games<p.MinimumGames)owner=p.ConditionFallbackOwner??p.OriginalTeam;
            }
            return p with{Owner=owner,AllocationPending=false};
        }).ToList()};
    }
}
