namespace DynastyManager.Core.Models;

public static class DynastyRosterRules
{
    public const int MaximumRosterSize = 85;

    // Modern position split of the legacy game's starter counts:
    // QB1/RB2/WR3/TE1/OL5/K1/DL4/LB3/CB3/S2.
    public static readonly IReadOnlyDictionary<Position, int> StarterPositionCounts =
        new Dictionary<Position, int>
        {
            [Position.QB] = 1,
            [Position.RB] = 2,
            [Position.WR] = 3,
            [Position.TE] = 1,
            [Position.OL] = 5,
            [Position.K] = 1,
            [Position.DE] = 2,
            [Position.DT] = 2,
            [Position.OLB] = 2,
            [Position.MLB] = 1,
            [Position.CB] = 3,
            [Position.FS] = 1,
            [Position.SS] = 1
        };

    // Modern position split of the legacy minimum playable depth:
    // QB3/RB5/WR8/TE3/OL11/K2/DL9/LB7/CB7/S5.
    public static readonly IReadOnlyDictionary<Position, int> MinimumPositionCounts =
        new Dictionary<Position, int>
        {
            [Position.QB] = 3,
            [Position.RB] = 5,
            [Position.WR] = 8,
            [Position.TE] = 3,
            [Position.OL] = 11,
            [Position.K] = 2,
            [Position.DE] = 5,
            [Position.DT] = 4,
            [Position.OLB] = 4,
            [Position.MLB] = 3,
            [Position.CB] = 7,
            [Position.FS] = 3,
            [Position.SS] = 2
        };

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
