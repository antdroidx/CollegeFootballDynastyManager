namespace DynastyManager.Core.Models;

public static class DynastyRosterRules
{
    public const int MaximumRosterSize = 85;

    public static readonly IReadOnlyDictionary<Position, int> TargetPositionCounts =
        new Dictionary<Position, int>
        {
            [Position.QB] = 4,
            [Position.RB] = 7,
            [Position.WR] = 11,
            [Position.TE] = 4,
            [Position.OL] = 16,
            [Position.K] = 3,
            [Position.DE] = 7,
            [Position.DT] = 6,
            [Position.OLB] = 6,
            [Position.MLB] = 4,
            [Position.CB] = 10,
            [Position.FS] = 4,
            [Position.SS] = 3
        };
}
