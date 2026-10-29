using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

/// <summary>One-time HS estimate for imported players without recorded recruiting history.</summary>
public static class ImportedRecruitingStarsService
{
    public static DynastyPlayer EstimateIfUnknown(DynastyPlayer player)
    {
        if (player.OriginalRecruitingStars.HasValue || player.IsWalkOn) return player;
        // Three OVR per completed college year, plus a previous redshirt season.
        // A current redshirt has not yet completed the extra development year.
        var seasons = Math.Clamp(player.ClassYear - 1, 0, 4) + (player.HasRedshirted && !player.IsRedshirted ? 1 : 0);
        var entryOverall = Math.Clamp(player.OverallRating - 3 * seasons, 40, 99);
        // Readiness bands align with the existing generated HS recruit ranges.
        var stars = entryOverall switch { >= 84 => 5, >= 76 => 4, >= 68 => 3, >= 60 => 2, _ => 1 };
        return player with { OriginalRecruitingStars = stars, RecruitingStarsEstimated = true };
    }
}
