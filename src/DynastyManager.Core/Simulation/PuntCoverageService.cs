using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public sealed record PuntCoverage(Guid? DefenderId, double DistanceAtCatch, bool FairCatch);

public static class PuntCoverageService
{
    public static PuntCoverage Evaluate(int distance, double hangTime, IReadOnlyList<DynastyPlayer> unit,
        Guid? kickerId, int returnerAwareness)
    {
        var nearest = unit.Where(p => p.PlayerId != kickerId).Select(p =>
        {
            var releaseDelay = p.Position is Position.WR or Position.CB ? .65 : p.Position == Position.OL ? 2.2 : 1.2;
            var speed = 6 + Math.Clamp(p.SpeedRating, 1, 99) / 20d;
            var gap = Math.Max(1.5, distance + 1 - speed * Math.Max(0, hangTime - releaseDelay));
            return (Player: p, Gap: gap);
        }).OrderBy(p => p.Gap).FirstOrDefault();
        if (nearest.Player is null) return new(null, distance, false);
        var safeThreshold = 5 + Math.Clamp(returnerAwareness, 1, 99) / 33d;
        return new(nearest.Player.PlayerId, nearest.Gap, nearest.Gap <= safeThreshold);
    }
}
