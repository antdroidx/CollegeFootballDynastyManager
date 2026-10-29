using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
namespace ProFootball.Core;

/// <summary>Professional lifecycle on the college game's canonical attribute and overall calculation.</summary>
public static class ProPlayerRatingService
{
    public const int Version = 1;
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
        if (state.RatingSystemVersion >= Version) return state;
        return state with { RatingSystemVersion = Version, Players = state.Players.Select(p => Refresh(p, true)).ToList(),
            Prospects = state.Prospects.Select(p => Refresh(p, true)).ToList(),
            DraftHistory = state.DraftHistory.Select(d => d with { Player = Refresh(d.Player, true) }).ToList() };
    }
}
