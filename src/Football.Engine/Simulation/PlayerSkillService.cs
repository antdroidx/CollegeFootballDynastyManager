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
            complete.Leadership is >= 1 and <= 99 &&
            complete.Discipline is >= 1 and <= 99 &&
            complete.Jumping is >= 1 and <= 99 &&
            complete.KickPower is >= 1 and <= 99 &&
            complete.KickAccuracy is >= 1 and <= 99) return player;
        var old = player.Skills ?? new PlayerSkillRatings();
        int Skill(int existing, string name, bool? specialist)
        {
            if (existing > 0) return Math.Clamp(existing, 1, 99);
            var offset = SimulationSeed.Create(player.PlayerId, 0, 0, player.FullName, name) % 25 - 12;
            var baseline = Math.Clamp(player.OverallRating, 40, 99);
            return Math.Clamp(baseline + offset + (specialist is null ? 0 : specialist.Value ? 3 : -28), 1, 99);
        }
        return player with { Skills = old with
        {
            PassingPower = Skill(old.PassingPower, "PassingPower", player.Position is Position.QB),
            PassingAccuracy = Skill(old.PassingAccuracy, "PassingAccuracy", player.Position is Position.QB),
            Catching = Skill(old.Catching, "Catching", player.Position is Position.WR or Position.TE),
            BreakTackle = Skill(old.BreakTackle, "BreakTackle", player.Position is Position.RB or Position.FB or Position.TE),
            Tackling = Skill(old.Tackling, "Tackling", player.Position is Position.DE or Position.DT or Position.OLB or Position.MLB or Position.CB or Position.FS or Position.SS),
            RunBlocking = Skill(old.RunBlocking, "RunBlocking", player.Position is Position.OL or Position.TE),
            PassBlocking = Skill(old.PassBlocking, "PassBlocking", player.Position is Position.OL),
            BallCarrying = Skill(old.BallCarrying, "BallCarrying", player.Position is Position.RB or Position.FB or Position.QB),
            Acceleration = Skill(old.Acceleration, "Acceleration", null),
            Leadership = Skill(old.Leadership, "Leadership", null),
            Discipline = Skill(old.Discipline, "Discipline", null),
            Jumping = Skill(old.Jumping, "Jumping", player.Position is Position.WR or Position.CB or Position.FS or Position.SS),
            KickPower = Skill(old.KickPower, "KickPower", player.Position.IsSpecialist()),
            KickAccuracy = Skill(old.KickAccuracy, "KickAccuracy", player.Position.IsSpecialist()),
        } };
    }

    // Physical/technical injury regression carries into the affected football skills.
    public static PlayerSkillRatings? ApplyInjuryRegression(DynastyPlayer before, DynastyPlayer after)
    {
        if (before.Skills is not { } skills) return null;
        int Shift(int value, int delta) => Math.Clamp(value + delta, 1, 99);
        return skills with
        {
            PassingPower = Shift(skills.PassingPower, after.StrengthRating - before.StrengthRating),
            PassingAccuracy = Shift(skills.PassingAccuracy, after.AwarenessRating - before.AwarenessRating),
            Catching = Shift(skills.Catching, after.AwarenessRating - before.AwarenessRating),
            BreakTackle = Shift(skills.BreakTackle, after.StrengthRating - before.StrengthRating),
            Tackling = Shift(skills.Tackling, after.AwarenessRating - before.AwarenessRating),
            RunBlocking = Shift(skills.RunBlocking, after.StrengthRating - before.StrengthRating),
            PassBlocking = Shift(skills.PassBlocking, after.AwarenessRating - before.AwarenessRating),
            BallCarrying = Shift(skills.BallCarrying, after.AgilityRating - before.AgilityRating),
            Acceleration = Shift(skills.Acceleration, after.SpeedRating - before.SpeedRating),
            Jumping = Shift(skills.Jumping, after.AgilityRating - before.AgilityRating),
            KickPower = Shift(skills.KickPower, after.StrengthRating - before.StrengthRating),
            KickAccuracy = Shift(skills.KickAccuracy, after.AwarenessRating - before.AwarenessRating),
        };
    }

    /// <summary>Independent yearly/midseason skill practice, seeded by identity and stage.</summary>
    public static PlayerSkillRatings Train(DynastyState state, DynastyPlayer player, int change, bool midseason)
    {
        var skills = EnsureInitialized(player).Skills!;
        int Gain(string name, int value, bool mental = false)
        {
            if (change == 0 || mental && midseason) return value;
            var roll = SimulationSeed.Create(state.DynastyId, state.SeasonYear, midseason ? 1 : 0,
                player.PlayerId.ToString("N"), "practice-" + name) % 5;
            var delta = change > 0 ? Math.Max(0, change + roll - 2) : Math.Min(0, change + roll - 2);
            var focused = name switch
            {
                "PassingPower" or "PassingAccuracy" => player.Position == Position.QB,
                "Catching" or "Jumping" => player.Position is Position.WR or Position.TE or Position.CB or Position.FS or Position.SS,
                "RunBlocking" or "PassBlocking" => player.Position is Position.OL or Position.TE,
                "BreakTackle" or "BallCarrying" => player.Position is Position.RB or Position.FB or Position.QB or Position.TE,
                "Tackling" => player.Position is Position.DE or Position.DT or Position.OLB or Position.MLB or Position.CB or Position.FS or Position.SS,
                "KickPower" or "KickAccuracy" => player.Position.IsSpecialist(),
                _ => true
            };
            if (!focused) delta = (int)Math.Round(delta * .35);
            if (mental) delta = Math.Clamp(delta, -1, 1);
            return Math.Clamp(value + delta, 1, 99);
        }
        return skills with
        {
            PassingPower = Gain("PassingPower", skills.PassingPower),
            PassingAccuracy = Gain("PassingAccuracy", skills.PassingAccuracy),
            Catching = Gain("Catching", skills.Catching),
            BreakTackle = Gain("BreakTackle", skills.BreakTackle),
            Tackling = Gain("Tackling", skills.Tackling),
            RunBlocking = Gain("RunBlocking", skills.RunBlocking),
            PassBlocking = Gain("PassBlocking", skills.PassBlocking),
            BallCarrying = Gain("BallCarrying", skills.BallCarrying),
            Acceleration = Gain("Acceleration", skills.Acceleration),
            Leadership = Gain("Leadership", skills.Leadership, true),
            Discipline = Gain("Discipline", skills.Discipline, true),
            Jumping = Gain("Jumping", skills.Jumping),
            KickPower = Gain("KickPower", skills.KickPower),
            KickAccuracy = Gain("KickAccuracy", skills.KickAccuracy),
        };
    }

    public static PlayerSkillRatings Shift(PlayerSkillRatings skills, int change) => skills with
    {
        PassingPower = Math.Clamp(skills.PassingPower + change, 1, 99),
        PassingAccuracy = Math.Clamp(skills.PassingAccuracy + change, 1, 99),
        Catching = Math.Clamp(skills.Catching + change, 1, 99),
        BreakTackle = Math.Clamp(skills.BreakTackle + change, 1, 99),
        Tackling = Math.Clamp(skills.Tackling + change, 1, 99),
        RunBlocking = Math.Clamp(skills.RunBlocking + change, 1, 99),
        PassBlocking = Math.Clamp(skills.PassBlocking + change, 1, 99),
        BallCarrying = Math.Clamp(skills.BallCarrying + change, 1, 99),
        Acceleration = Math.Clamp(skills.Acceleration + change, 1, 99),
        Leadership = Math.Clamp(skills.Leadership + change, 1, 99),
        Discipline = Math.Clamp(skills.Discipline + change, 1, 99),
        Jumping = Math.Clamp(skills.Jumping + change, 1, 99),
        KickPower = Math.Clamp(skills.KickPower + change, 1, 99),
        KickAccuracy = Math.Clamp(skills.KickAccuracy + change, 1, 99),
    };

    public static double GameRating(DynastyPlayer player, bool passing = true)
    {
        var s = EnsureInitialized(player).Skills!;
        double role = player.Position switch
        {
            Position.QB => passing ? s.PassingPower * .35 + s.PassingAccuracy * .65 : s.BallCarrying * .6 + s.Acceleration * .4,
            Position.RB => passing ? s.Catching * .7 + s.PassBlocking * .3 : s.BallCarrying * .5 + s.BreakTackle * .5,
            Position.WR => s.Catching * .8 + s.Jumping * .2,
            Position.TE => passing ? s.Catching * .6 + s.PassBlocking * .4 : s.RunBlocking,
            Position.OL => passing ? s.PassBlocking : s.RunBlocking,
            Position.CB or Position.FS or Position.SS => passing ? player.AwarenessRating * .65 + player.AgilityRating * .35 : s.Tackling,
            Position.OLB or Position.MLB => passing ? s.Tackling * .4 + player.AwarenessRating * .6 : s.Tackling,
            Position.DE or Position.DT => s.Tackling,
            Position.K => s.KickAccuracy * .7 + s.KickPower * .3,
            Position.P => s.KickPower * .75 + s.KickAccuracy * .25,
            _ => player.OverallRating
        };
        var athletic = player.Position switch
        {
            Position.OL or Position.DT => player.StrengthRating * .8 + player.AgilityRating * .2,
            Position.DE or Position.OLB or Position.MLB => player.StrengthRating * .5 + player.SpeedRating * .3 + player.AgilityRating * .2,
            Position.K or Position.P => player.StrengthRating * .5 + player.AwarenessRating * .5,
            _ => player.SpeedRating * .4 + player.AgilityRating * .3 + s.Acceleration * .3
        };
        var support = player.InjuryRating * .6 + s.Leadership * .2 + s.Discipline * .2;
        return Math.Clamp(role * .65 + athletic * .20 + player.AwarenessRating * .10 + support * .05, 1, 99);
    }
}
