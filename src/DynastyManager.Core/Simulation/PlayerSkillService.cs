using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class PlayerSkillService
{
    public static DynastyPlayer EnsureInitialized(DynastyPlayer player)
    {
        if (player.Skills is { } complete &&
            complete.PassingPower is >= 1 and <= 99 &&
            complete.PassingAccuracy is >= 1 and <= 99 &&
            complete.Catching is >= 1 and <= 99 &&
            complete.BreakTackle is >= 1 and <= 99 &&
            complete.Tackling is >= 1 and <= 99 &&
            complete.RunBlocking is >= 1 and <= 99 &&
            complete.PassBlocking is >= 1 and <= 99 &&
            complete.BallCarrying is >= 1 and <= 99 &&
            complete.Acceleration is >= 1 and <= 99 &&
            complete.Stamina is >= 1 and <= 99 &&
            complete.Leadership is >= 1 and <= 99 &&
            complete.Discipline is >= 1 and <= 99 &&
            complete.Jumping is >= 1 and <= 99 &&
            complete.ManCoverage is >= 1 and <= 99 &&
            complete.ZoneCoverage is >= 1 and <= 99 &&
            complete.KickPower is >= 1 and <= 99 &&
            complete.KickAccuracy is >= 1 and <= 99) return player;
        var old = player.Skills ?? new PlayerSkillRatings();
        int Skill(int existing, string name, int basis, bool? specialist)
        {
            if (existing > 0) return Math.Clamp(existing, 1, 99);
            var offset = SimulationSeed.Create(player.PlayerId, 0, 0, player.FullName, name) % 13 - 6;
            var baseline = basis > 0 ? basis : Math.Clamp(player.OverallRating, 40, 99);
            return Math.Clamp(baseline + offset + (specialist is null ? 0 : specialist.Value ? 3 : -28), 1, 99);
        }
        return player with { Skills = old with
        {
            PassingPower = Skill(old.PassingPower, "PassingPower", player.StrengthRating, player.Position is Position.QB),
            PassingAccuracy = Skill(old.PassingAccuracy, "PassingAccuracy", player.TechniqueRating, player.Position is Position.QB),
            Catching = Skill(old.Catching, "Catching", player.TechniqueRating, player.Position is Position.WR or Position.TE),
            BreakTackle = Skill(old.BreakTackle, "BreakTackle", player.StrengthRating, player.Position is Position.RB or Position.TE),
            Tackling = Skill(old.Tackling, "Tackling", player.TechniqueRating, player.Position is Position.DE or Position.DT or Position.OLB or Position.MLB or Position.CB or Position.FS or Position.SS),
            RunBlocking = Skill(old.RunBlocking, "RunBlocking", player.StrengthRating, player.Position is Position.OL or Position.TE),
            PassBlocking = Skill(old.PassBlocking, "PassBlocking", player.TechniqueRating, player.Position is Position.OL),
            BallCarrying = Skill(old.BallCarrying, "BallCarrying", player.AgilityRating, player.Position is Position.RB or Position.QB),
            Acceleration = Skill(old.Acceleration, "Acceleration", player.SpeedRating, null),
            Stamina = Skill(old.Stamina, "Stamina", player.DurabilityRating, null),
            Leadership = Skill(old.Leadership, "Leadership", player.AwarenessRating, null),
            Discipline = Skill(old.Discipline, "Discipline", player.AwarenessRating, null),
            Jumping = Skill(old.Jumping, "Jumping", player.AgilityRating, player.Position is Position.WR or Position.CB or Position.FS or Position.SS),
            ManCoverage = Skill(old.ManCoverage, "ManCoverage", player.AgilityRating, player.Position is Position.CB or Position.FS or Position.SS),
            ZoneCoverage = Skill(old.ZoneCoverage, "ZoneCoverage", player.AwarenessRating, player.Position is Position.CB or Position.FS or Position.SS or Position.OLB or Position.MLB),
            KickPower = Skill(old.KickPower, "KickPower", player.StrengthRating, player.Position is Position.K),
            KickAccuracy = Skill(old.KickAccuracy, "KickAccuracy", player.TechniqueRating, player.Position is Position.K),
        } };
    }

    // Skill growth follows physical/technical development; personality is stable.
    public static PlayerSkillRatings? Develop(DynastyPlayer before, DynastyPlayer after)
    {
        if (before.Skills is not { } skills) return null;
        int Shift(int value, int delta) => Math.Clamp(value + delta, 1, 99);
        return skills with
        {
            PassingPower = Shift(skills.PassingPower, after.StrengthRating - before.StrengthRating),
            PassingAccuracy = Shift(skills.PassingAccuracy, after.TechniqueRating - before.TechniqueRating),
            Catching = Shift(skills.Catching, after.TechniqueRating - before.TechniqueRating),
            BreakTackle = Shift(skills.BreakTackle, after.StrengthRating - before.StrengthRating),
            Tackling = Shift(skills.Tackling, after.TechniqueRating - before.TechniqueRating),
            RunBlocking = Shift(skills.RunBlocking, after.StrengthRating - before.StrengthRating),
            PassBlocking = Shift(skills.PassBlocking, after.TechniqueRating - before.TechniqueRating),
            BallCarrying = Shift(skills.BallCarrying, after.AgilityRating - before.AgilityRating),
            Acceleration = Shift(skills.Acceleration, after.SpeedRating - before.SpeedRating),
            Stamina = Shift(skills.Stamina, after.DurabilityRating - before.DurabilityRating),
            Jumping = Shift(skills.Jumping, after.AgilityRating - before.AgilityRating),
            ManCoverage = Shift(skills.ManCoverage, after.AgilityRating - before.AgilityRating),
            ZoneCoverage = Shift(skills.ZoneCoverage, after.AwarenessRating - before.AwarenessRating),
            KickPower = Shift(skills.KickPower, after.StrengthRating - before.StrengthRating),
            KickAccuracy = Shift(skills.KickAccuracy, after.TechniqueRating - before.TechniqueRating),
        };
    }

    public static double GameRating(DynastyPlayer player, bool passing = true)
    {
        if (player.Skills is not { } s) return player.OverallRating;
        double role = player.Position switch
        {
            Position.QB => passing ? s.PassingPower * .35 + s.PassingAccuracy * .65 : s.BallCarrying * .6 + s.Acceleration * .4,
            Position.RB => passing ? s.Catching * .7 + s.PassBlocking * .3 : s.BallCarrying * .5 + s.BreakTackle * .5,
            Position.WR => s.Catching * .8 + s.Jumping * .2,
            Position.TE => passing ? s.Catching * .6 + s.PassBlocking * .4 : s.RunBlocking,
            Position.OL => passing ? s.PassBlocking : s.RunBlocking,
            Position.CB or Position.FS or Position.SS => passing ? (s.ManCoverage + s.ZoneCoverage) / 2.0 : s.Tackling,
            Position.OLB or Position.MLB => passing ? s.Tackling * .4 + s.ZoneCoverage * .6 : s.Tackling,
            Position.DE or Position.DT => s.Tackling,
            Position.K => (s.KickPower + s.KickAccuracy) / 2.0,
            _ => player.OverallRating
        };
        var support = (s.Acceleration + s.Stamina + s.Leadership + s.Discipline) / 4.0;
        return Math.Clamp(player.OverallRating * .70 + role * .25 + support * .05, 1, 99);
    }
}
