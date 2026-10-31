using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

/// <summary>How well a player's natural position transfers to another position.</summary>
public static class PositionFit
{
    public enum Fit { Same, Close, Medium, Far, Extreme }

    static readonly HashSet<(Position, Position)> Close = new()
    {
        (Position.RB, Position.FB), (Position.FB, Position.RB),
        (Position.TE, Position.FB), (Position.FB, Position.TE),
        (Position.DT, Position.DE), (Position.DE, Position.DT),
        (Position.SS, Position.FS), (Position.FS, Position.SS),
        (Position.K, Position.P), (Position.P, Position.K),
        (Position.OLB, Position.MLB), (Position.MLB, Position.OLB)
    };

    static readonly HashSet<(Position, Position)> Medium = new()
    {
        (Position.SS, Position.OLB), (Position.OLB, Position.SS),
        (Position.CB, Position.FS), (Position.FS, Position.CB),
        (Position.WR, Position.CB), (Position.CB, Position.WR),
        (Position.TE, Position.OL), (Position.OL, Position.TE),
        (Position.RB, Position.WR), (Position.WR, Position.RB),
        (Position.MLB, Position.DE), (Position.DE, Position.MLB),
        (Position.FB, Position.OL), (Position.OL, Position.FB)
    };

    static bool Line(Position p) => p is Position.OL;
    static bool Front(Position p) => p is Position.DE or Position.DT or Position.OLB or Position.MLB;
    static bool Back(Position p) => p is Position.CB or Position.FS or Position.SS;
    static bool Skill(Position p) => p is Position.QB or Position.RB or Position.FB or Position.WR or Position.TE;

    public static Fit Classify(Position natural, Position target)
    {
        if (natural == target) return Fit.Same;
        if (Close.Contains((natural, target))) return Fit.Close;
        if (Medium.Contains((natural, target))) return Fit.Medium;
        // Position families that are physically and conceptually adjacent.
        if (Line(natural) && Line(target)) return Fit.Same;
        if (Front(natural) && Front(target)) return Fit.Medium;
        if (Back(natural) && Back(target)) return Fit.Medium;
        if (Skill(natural) && Skill(target)) return Fit.Medium;
        // A lineman or front-seven player at a skill position is a major projection.
        if ((Line(natural) || Front(natural)) && Skill(target)) return Fit.Extreme;
        if ((Line(target) || Front(target)) && Skill(natural)) return Fit.Extreme;
        if ((natural is Position.K or Position.P) || (target is Position.K or Position.P)) return Fit.Extreme;
        if (natural == Position.QB || target == Position.QB) return Fit.Extreme;
        // Line vs skill, line vs secondary, specialists anywhere else.
        return Fit.Far;
    }

    public static double Multiplier(Position natural, Position target) => Classify(natural, target) switch
    {
        Fit.Same => 1.00,
        Fit.Close => 0.87,
        Fit.Medium => 0.68,
        Fit.Far => 0.48,
        _ => 0.30
    };

    public static int Apply(int rating, Position natural, Position target) =>
        Math.Clamp((int)Math.Round(rating * Multiplier(natural, target)), 1, 99);

    public static int AwarenessLoss(Position natural, Position target) => Classify(natural, target) switch
    {
        Fit.Same => 0,
        Fit.Close => 6,
        Fit.Medium => 12,
        Fit.Far => 18,
        _ => 24
    };

    public static string Label(Position natural, Position target) => Classify(natural, target) switch
    {
        Fit.Same => "Natural",
        Fit.Close => "Close fit",
        Fit.Medium => "Adapted",
        Fit.Far => "Out of position",
        _ => "Major projection"
    };
}