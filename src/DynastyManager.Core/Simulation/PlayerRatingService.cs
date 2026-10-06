using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class PlayerRatingService
{
    public static DynastyState EnsureProfiles(DynastyState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return state with
        {
            ActiveRoster = state.ActiveRoster
                .Select(player => EnsureInitialized(state, player))
                .ToArray()
        };
    }

    public static DynastyPlayer EnsureInitialized(
        DynastyState state,
        DynastyPlayer player)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(player);

        if (player.PotentialRating > 0 &&
            player.SpeedRating > 0 &&
            player.StrengthRating > 0 &&
            player.AgilityRating > 0 &&
            player.AwarenessRating > 0 &&
            player.TechniqueRating > 0 &&
            player.DurabilityRating > 0)
        {
            return player;
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
        var technique = ClampRating(target + PositionBias(player.Position, "technique") +
            Offset(state, player, "technique"));
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
            TechniqueRating = technique,
            DurabilityRating = durability
        };

        var calculated = CalculateOverall(initialized);
        var correction = target - calculated;

        initialized = initialized with
        {
            SpeedRating = ClampRating(initialized.SpeedRating + correction),
            StrengthRating = ClampRating(initialized.StrengthRating + correction),
            AgilityRating = ClampRating(initialized.AgilityRating + correction),
            AwarenessRating = ClampRating(initialized.AwarenessRating + correction),
            TechniqueRating = ClampRating(initialized.TechniqueRating + correction),
            DurabilityRating = ClampRating(initialized.DurabilityRating + correction)
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

        return initialized with
        {
            OverallRating = CalculateOverall(initialized),
            PotentialRating = Math.Max(
                CalculateOverall(initialized),
                potential)
        };
    }

    public static int CalculateOverall(DynastyPlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);

        var weights = Weights(player.Position);

        var rating =
            player.SpeedRating * weights.Speed +
            player.StrengthRating * weights.Strength +
            player.AgilityRating * weights.Agility +
            player.AwarenessRating * weights.Awareness +
            player.TechniqueRating * weights.Technique +
            player.DurabilityRating * weights.Durability;

        return Math.Clamp(
            (int)Math.Round(rating, MidpointRounding.AwayFromZero),
            40,
            99);
    }

    public static DynastyPlayer RecalculateOverall(DynastyPlayer player) =>
        player with
        {
            OverallRating = CalculateOverall(player)
        };

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

    private static RatingWeights Weights(Position position) =>
        position switch
        {
            Position.QB => new(.10, .10, .10, .25, .35, .10),
            Position.RB => new(.25, .15, .25, .10, .15, .10),
            Position.WR => new(.25, .10, .20, .10, .25, .10),
            Position.TE => new(.15, .20, .10, .15, .25, .15),
            Position.OL => new(.00, .35, .05, .15, .30, .15),
            Position.K => new(.05, .10, .05, .20, .45, .15),
            Position.DE => new(.15, .25, .15, .10, .25, .10),
            Position.DT => new(.05, .35, .05, .10, .25, .20),
            Position.OLB => new(.20, .15, .20, .15, .20, .10),
            Position.MLB => new(.10, .20, .15, .25, .20, .10),
            Position.CB => new(.30, .02, .25, .15, .20, .08),
            Position.FS => new(.20, .05, .20, .25, .20, .10),
            Position.SS => new(.18, .10, .18, .24, .20, .10),
            _ => new(.15, .15, .15, .20, .25, .10)
        };

    private sealed record RatingWeights(
        double Speed,
        double Strength,
        double Agility,
        double Awareness,
        double Technique,
        double Durability);
}
