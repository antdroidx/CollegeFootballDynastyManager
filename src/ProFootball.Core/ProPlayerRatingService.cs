using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
namespace ProFootball.Core;

/// <summary>Professional lifecycle on the college game's canonical attribute and overall calculation.</summary>
public static class ProPlayerRatingService
{
    public const int Version = 1;
    static readonly Lazy<HashSet<string>> FullbackIds = new(() => ProLeagueService.Snapshot().Players.Where(p=>p.Football.Position==Position.FB).Select(p=>p.ExternalId).ToHashSet());
    public static ProPlayer Refresh(ProPlayer player, bool preserveHeadroom = false, bool resetBaseline = false)
    {
        var old = player.Football;
        var initialized = PlayerSkillService.EnsureInitialized(old);
        var overall = PlayerRatingService.CalculateOverall(initialized);
        var headroom = Math.Max(0, old.PotentialRating - old.OverallRating);
        var potential = Math.Clamp(preserveHeadroom ? overall + headroom : Math.Max(overall, old.PotentialRating), overall, 99);
        var baseline = resetBaseline ? overall : Math.Clamp(overall - (old.OverallRating - (old.SeasonStartOverall > 0 ? old.SeasonStartOverall : old.OverallRating)), 1, 99);
        return player with { Football = initialized with { OverallRating = overall, PotentialRating = potential, SeasonStartOverall = baseline } };
    }
    public static ProFranchise Migrate(ProFranchise state)
    {
        if(state.Players.Any(p=>!ReferenceEquals(p,ProBackfieldPositions.Correct(p)))||state.Prospects.Any(p=>!ReferenceEquals(p,ProBackfieldPositions.Correct(p))))
            state=state with{Players=state.Players.Select(ProBackfieldPositions.Correct).ToList(),Prospects=state.Prospects.Select(ProBackfieldPositions.Correct).ToList()};
        bool NeedsFullback(ProPlayer p) => p.Football.Position==Position.RB && (p.Football.NcaaPositionCode==2 || FullbackIds.Value.Contains(p.ExternalId));
        ProPlayer Restore(ProPlayer p) => NeedsFullback(p) ? Refresh(p with {Football=p.Football with {Position=Position.FB,NcaaPositionCode=2}},true) : p;
        if(state.Players.Any(NeedsFullback)||state.Prospects.Any(NeedsFullback))
            state=state with {Players=state.Players.Select(Restore).ToList(),Prospects=state.Prospects.Select(Restore).ToList()};
        if (state.RatingSystemVersion >= Version) return state;
        return state with { RatingSystemVersion = Version, Players = state.Players.Select(p => Refresh(p, true)).ToList(),
            Prospects = state.Prospects.Select(p => Refresh(p, true)).ToList(),
            DraftHistory = state.DraftHistory.Select(d => d with { Player = Refresh(d.Player, true) }).ToList() };
    }
}
