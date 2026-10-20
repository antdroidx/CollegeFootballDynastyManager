using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class PlayerRatingService
{
    public static DynastyState EnsureProfiles(DynastyState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return state with
        {
            ActiveRoster = PlayerJerseyService.Assign(state.ActiveRoster
                .Select(player => EnsureInitialized(state, player)).ToArray())
        };
    }

    public static DynastyPlayer EnsureInitialized(
        DynastyState state,
        DynastyPlayer player)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(player);
        player = PlayerPhysicalService.EnsureMeasurements(player);

        if (player.PotentialRating > 0 &&
            player.SpeedRating > 0 &&
            player.StrengthRating > 0 &&
            player.AgilityRating > 0 &&
            player.AwarenessRating > 0 &&
            player.InjuryRating > 0)
        {
            return RecalculateOverall(PlayerSkillService.EnsureInitialized(player));
        }

        var target = Math.Clamp(player.OverallRating, 50, 99);

        var speed = ClampRating(target + PositionBias(player.Position, "speed") +
            Offset(state, player, "speed"));
        var strength = ClampRating(target + PositionBias(player.Position, "strength") +
            Offset(state, player, "strength"));
        var agility = ClampRating(target + PositionBias(player.Position, "agility") +
            Offset(state, player, "agility"));
        var awareness = ClampRating(target + PositionBias(player.Position, "awareness") +
            Offset(state, player, "awareness"));
        var durability = ClampRating(
            Math.Max(
                55,
                target + PositionBias(player.Position, "durability") +
                Offset(state, player, "durability")));

        var initialized = player with
        {
            SpeedRating = speed,
            StrengthRating = strength,
            AgilityRating = agility,
            AwarenessRating = awareness,
            InjuryRating = durability
        };

        var potential = player.PotentialRating > 0
            ? player.PotentialRating
            : Math.Clamp(
                target +
                3 +
                player.TalentLevel / 2 +
                Deterministic(state, player, "potential") % 9 -
                Math.Max(0, player.ClassYear - 1),
                target,
                99);

        initialized = PlayerSkillService.EnsureInitialized(initialized);
        // Generated recruiting OVR is a talent target. Fit the generated attributes
        // to that target on the NCAA scale; imported or already initialized ratings
        // take the earlier branch and are never altered by this calibration.
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var current = CalculateOverall(initialized);
            if (Math.Abs(current - target) <= 1) break;
            var shift = Math.Sign(target - current) * Math.Max(1, Math.Abs(target - current) / 2);
            var skills = initialized.Skills!;
            var candidate = initialized with
            {
                SpeedRating = ClampRating(initialized.SpeedRating + shift),
                StrengthRating = ClampRating(initialized.StrengthRating + shift),
                AgilityRating = ClampRating(initialized.AgilityRating + shift),
                AwarenessRating = ClampRating(initialized.AwarenessRating + shift),
                Skills = PlayerSkillService.Shift(skills, shift) with
                { Leadership = skills.Leadership, Discipline = skills.Discipline }
            };
            if (Math.Abs(CalculateOverall(candidate) - target) >= Math.Abs(current - target)) break;
            initialized = candidate;
        }
        var overall = CalculateOverall(initialized);
        return initialized with { OverallRating = overall, PotentialRating = Math.Max(overall, potential) };
    }

    public static int CalculateOverall(DynastyPlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);
        return Ncaa06RatingService.CalculateOverall(player);
    }

    public static DynastyPlayer RecalculateOverall(DynastyPlayer player) =>
        player with { OverallRating = CalculateOverall(player) };

    private static int Offset(
        DynastyState state,
        DynastyPlayer player,
        string attribute) =>
        Deterministic(state, player, attribute) % 7 - 3;

    private static int Deterministic(
        DynastyState state,
        DynastyPlayer player,
        string purpose) =>
        SimulationSeed.Create(
            state.DynastyId,
            state.SeasonYear,
            player.ClassYear,
            player.PlayerId.ToString("N"),
            $"player-rating-{purpose}");

    private static int ClampRating(int value) =>
        Math.Clamp(value, 40, 99);

    private static int PositionBias(
        Position position,
        string attribute) =>
        (position, attribute) switch
        {
            (Position.QB, "technique") => 4,
            (Position.QB, "awareness") => 3,
            (Position.QB, "strength") => 1,
            (Position.QB, "speed") => -2,

            (Position.RB, "speed") => 3,
            (Position.RB, "agility") => 4,
            (Position.RB, "strength") => 1,

            (Position.WR, "speed") => 4,
            (Position.WR, "agility") => 3,
            (Position.WR, "technique") => 3,
            (Position.WR, "strength") => -2,

            (Position.TE, "strength") => 3,
            (Position.TE, "technique") => 3,
            (Position.TE, "speed") => -1,

            (Position.OL, "strength") => 5,
            (Position.OL, "technique") => 4,
            (Position.OL, "speed") => -5,
            (Position.OL, "agility") => -2,

            (Position.K, "technique") => 6,
            (Position.K, "awareness") => 2,
            (Position.K, "strength") => 1,
            (Position.K, "speed") => -4,

            (Position.DE, "strength") => 3,
            (Position.DE, "technique") => 3,
            (Position.DE, "speed") => 1,

            (Position.DT, "strength") => 5,
            (Position.DT, "technique") => 3,
            (Position.DT, "speed") => -4,
            (Position.DT, "agility") => -2,

            (Position.OLB, "speed") => 2,
            (Position.OLB, "agility") => 2,
            (Position.OLB, "technique") => 2,

            (Position.MLB, "awareness") => 4,
            (Position.MLB, "strength") => 2,
            (Position.MLB, "technique") => 2,

            (Position.CB, "speed") => 5,
            (Position.CB, "agility") => 4,
            (Position.CB, "technique") => 2,
            (Position.CB, "strength") => -4,

            (Position.FS, "awareness") => 4,
            (Position.FS, "speed") => 2,
            (Position.FS, "agility") => 2,

            (Position.SS, "strength") => 2,
            (Position.SS, "awareness") => 3,
            (Position.SS, "agility") => 1,

            _ => 0
        };

}
