using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

/// <summary>Corrected NCAA 06 POCI attribute weights with fixed NEXT26 score-to-OVR curves.</summary>
public static class Ncaa06RatingService
{
    // PLDH, PLDL, followed by PCAR PKAC PTHA PPBK PRBK PACC PAGI PTAK PINJ PKPR PSPD PTHP PBTK PCTH PSTR PJMP PAWR.
    private static readonly double[][] Weights =
    [
        [92, 43, 0, 0, 33, 0, 0, 3, 5, 0, 0, 0, 10, 27, 0, 0, 0, 0, 22],
        [90, 40, 14, 0, 0, 3, 0, 10, 14, 0, 0, 0, 16, 0, 20, 6, 4, 0, 13],
        [79, 40, 10, 0, 0, 8, 20, 7, 6, 0, 0, 0, 7, 0, 10, 10, 8, 0, 14],
        [93, 42, 0, 0, 0, 0, 0, 14, 14, 0, 0, 0, 14, 0, 5, 27, 3, 9, 14],
        [83, 40, 0, 0, 0, 15, 21, 5, 5, 0, 0, 0, 5, 0, 5, 20, 8, 3, 13],
        [96, 40, 0, 0, 0, 30, 25, 6, 3, 0, 0, 0, 3, 0, 0, 0, 15, 0, 18],
        [95, 40, 0, 0, 0, 27, 30, 7, 2, 0, 0, 0, 3, 0, 0, 0, 15, 0, 16],
        [96, 40, 0, 0, 0, 30, 30, 5, 2, 0, 0, 0, 2, 0, 0, 0, 15, 0, 16],
        [95, 40, 0, 0, 0, 27, 30, 7, 2, 0, 0, 0, 3, 0, 0, 0, 15, 0, 16],
        [96, 40, 0, 0, 0, 30, 25, 6, 3, 0, 0, 0, 3, 0, 0, 0, 15, 0, 18],
        [90, 41, 0, 0, 0, 0, 0, 16, 9, 28, 0, 0, 13, 0, 0, 0, 21, 0, 13],
        [90, 41, 0, 0, 0, 0, 0, 16, 9, 28, 0, 0, 13, 0, 0, 0, 21, 0, 13],
        [90, 41, 0, 0, 0, 0, 0, 14, 5, 33, 0, 0, 5, 0, 0, 0, 30, 0, 13],
        [88, 40, 0, 0, 0, 0, 0, 16, 9, 30, 0, 0, 15, 0, 0, 0, 14, 0, 16],
        [93, 40, 0, 0, 0, 0, 0, 15, 7, 35, 0, 0, 9, 0, 0, 0, 14, 0, 20],
        [88, 40, 0, 0, 0, 0, 0, 16, 9, 30, 0, 0, 15, 0, 0, 0, 14, 0, 16],
        [90, 40, 0, 0, 0, 0, 0, 16, 14, 12, 0, 0, 18, 0, 0, 8, 4, 10, 18],
        [88, 40, 0, 0, 0, 0, 0, 16, 10, 18, 0, 0, 20, 0, 0, 4, 5, 7, 20],
        [86, 40, 0, 0, 0, 0, 0, 16, 9, 24, 0, 0, 17, 0, 0, 2, 10, 4, 18],
        [93, 40, 0, 55, 0, 0, 0, 0, 0, 0, 0, 40, 0, 0, 0, 0, 0, 0, 5],
        [93, 40, 0, 40, 0, 0, 0, 0, 0, 0, 0, 55, 0, 0, 0, 0, 0, 0, 5],
    ];
    public static int PositionCode(DynastyPlayer player) => player.NcaaPositionCode ?? player.Position switch
    {
        Position.QB => 0, Position.RB => 1, Position.FB => 2, Position.WR => 3, Position.TE => 4, Position.OL => 7,
        Position.DE => 10, Position.DT => 12, Position.OLB => 13, Position.MLB => 14,
        Position.CB => 16, Position.FS => 17, Position.SS => 18, Position.K => 19,
        _ => throw new ArgumentOutOfRangeException(nameof(player))
    };
    public static int CalculateOverall(DynastyPlayer player) =>
        Next26OverallCalibration.Calculate(PositionCode(player), CalculateWeightedScore(player));

    public static double CalculateWeightedScore(DynastyPlayer player)
    {
        var skills = PlayerSkillService.EnsureInitialized(player).Skills!;
        var values = new[] { skills.BallCarrying, skills.KickAccuracy, skills.PassingAccuracy,
            skills.PassBlocking, skills.RunBlocking, skills.Acceleration, player.AgilityRating,
            skills.Tackling, player.InjuryRating, skills.KickPower, player.SpeedRating,
            skills.PassingPower, skills.BreakTackle, skills.Catching, player.StrengthRating,
            skills.Jumping, player.AwarenessRating };
        var position = PositionCode(player);
        if (position < 0 || position >= Weights.Length) throw new ArgumentOutOfRangeException(nameof(player.NcaaPositionCode));
        var row = Weights[position];
        var sum = row.Skip(2).Sum();
        var score = 0d;
        for (var i = 0; i < values.Length; i++)
            score += Math.Clamp(values[i], 1, 99) * row[i + 2] / sum;
        return score;
    }
}
