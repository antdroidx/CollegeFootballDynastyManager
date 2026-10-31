using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

/// <summary>Permanent position changes: new role, awareness drop, and a fit-adjusted overall.</summary>
public static class PlayerPositionConversion
{
    public static int DefaultCode(Position position) => position switch
    {
        Position.QB => 0, Position.RB => 1, Position.FB => 2, Position.WR => 3, Position.TE => 4,
        Position.OL => 7, Position.DE => 10, Position.DT => 12, Position.OLB => 13, Position.MLB => 14,
        Position.CB => 16, Position.FS => 17, Position.SS => 18, Position.K => 19, Position.P => 20,
        _ => 0
    };

    public static DynastyPlayer Convert(DynastyPlayer player, Position target, int? code = null)
    {
        if (player.Position == target) return player;
        var natural = player.Position;
        var loss = PositionFit.AwarenessLoss(natural, target);
        var overall = PositionFit.Apply(player.OverallRating, natural, target);
        var skills = PlayerSkillService.EnsureInitialized(player).Skills! with
        {
            Discipline = Math.Clamp(player.Skills?.Discipline ?? 75, 1, 99)
        };
        return player with
        {
            Position = target,
            NcaaPositionCode = code ?? DefaultCode(target),
            AwarenessRating = Math.Clamp(player.AwarenessRating - loss, 1, 99),
            OverallRating = overall,
            SeasonStartOverall = Math.Min(player.SeasonStartOverall > 0 ? player.SeasonStartOverall : overall, overall),
            PotentialRating = Math.Max(overall, Math.Clamp(player.PotentialRating - loss / 3, overall, 99)),
            PlayingPositionCode = null,
            Skills = skills
        };
    }
}