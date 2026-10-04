using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class PlayerDevelopmentService
{
    public static DynastyState ApplyOffseasonDevelopment(
        DynastyState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.Phase != SeasonPhase.Offseason)
            return state;

        if (state.PlayerDevelopmentHistory.Any(record =>
                record.SeasonYear == state.SeasonYear))
        {
            return state;
        }

        state = PlayerRatingService.EnsureProfiles(state);

        var updatedPlayers = new List<DynastyPlayer>(
            state.ActiveRoster.Count);

        var history = state.PlayerDevelopmentHistory.ToList();

        foreach (var player in state.ActiveRoster)
        {
            var beforeOverall = player.OverallRating;
            var beforePotential = player.PotentialRating;

            var developed = DevelopPlayer(
                state,
                player);

            updatedPlayers.Add(developed);

            history.Add(new PlayerDevelopmentRecord
            {
                SeasonYear = state.SeasonYear,
                PlayerId = player.PlayerId,
                PlayerName = player.FullName,
                TeamName = player.TeamName,
                Position = player.Position,
                BeforeOverall = beforeOverall,
                AfterOverall = developed.OverallRating,
                BeforePotential = beforePotential,
                AfterPotential = developed.PotentialRating
            });
        }

        return RosterManagementService.NormalizeAllDepthCharts(
            state with
            {
                ActiveRoster = updatedPlayers,
                PlayerDevelopmentHistory = history
            });
    }

    private static DynastyPlayer DevelopPlayer(
        DynastyState state,
        DynastyPlayer player)
    {
        var potentialGap =
            player.PotentialRating -
            player.OverallRating;

        var gapFactor = potentialGap switch
        {
            >= 18 => 3,
            >= 10 => 2,
            >= 5 => 1,
            <= 0 => -1,
            _ => 0
        };

        var classFactor = player.ClassYear switch
        {
            1 => 2,
            2 => 1,
            3 => 0,
            4 => -1,
            _ => 0
        };

        var roleFactor = RosterManagementService.IsStarter(player)
            ? 1
            : player.DepthChartOrder is > 0 and <= 3
                ? 0
                : -1;

        var redshirtFactor =
            player.IsRedshirted
                ? 1
                : 0;

        var injuryFactor =
            player.CurrentInjury is null
                ? 0
                : player.CurrentInjury.Severity switch
                {
                    InjurySeverity.Minor => 0,
                    InjurySeverity.Moderate => -1,
                    InjurySeverity.Major => -1,
                    InjurySeverity.Severe => -2,
                    _ => 0
                };

        var random =
            Deterministic(
                state,
                player,
                "development") %
            5 - 2;

        var targetChange = Math.Clamp(
            gapFactor +
            classFactor +
            roleFactor +
            redshirtFactor +
            injuryFactor +
            random,
            -3,
            6);

        var developed = ApplyAttributeDevelopment(
            state,
            player,
            targetChange);

        developed = developed with
        {
            CurrentInjury = null
        };

        if (developed.OverallRating >
            developed.PotentialRating + 1)
        {
            developed = ReduceToPotential(
                developed,
                developed.PotentialRating + 1);
        }

        return PlayerRatingService.RecalculateOverall(
            developed);
    }

    private static DynastyPlayer ApplyAttributeDevelopment(
        DynastyState state,
        DynastyPlayer player,
        int targetChange)
    {
        if (targetChange == 0)
            return player;

        var speedChange = AttributeChange(
            state,
            player,
            "speed",
            targetChange,
            PhysicalMultiplier(player.ClassYear));
        var strengthChange = AttributeChange(
            state,
            player,
            "strength",
            targetChange,
            PhysicalMultiplier(player.ClassYear));
        var agilityChange = AttributeChange(
            state,
            player,
            "agility",
            targetChange,
            PhysicalMultiplier(player.ClassYear));
        var awarenessChange = AttributeChange(
            state,
            player,
            "awareness",
            targetChange,
            1.15);
        var techniqueChange = AttributeChange(
            state,
            player,
            "technique",
            targetChange,
            1.10);
        var durabilityChange = targetChange > 0
            ? Math.Max(
                0,
                AttributeChange(
                    state,
                    player,
                    "durability",
                    Math.Max(1, targetChange / 2),
                    .75))
            : Math.Min(
                0,
                AttributeChange(
                    state,
                    player,
                    "durability",
                    targetChange,
                    .50));

        var developed = player with
        {
            SpeedRating = Clamp(
                player.SpeedRating + speedChange),
            StrengthRating = Clamp(
                player.StrengthRating + strengthChange),
            AgilityRating = Clamp(
                player.AgilityRating + agilityChange),
            AwarenessRating = Clamp(
                player.AwarenessRating + awarenessChange),
            TechniqueRating = Clamp(
                player.TechniqueRating + techniqueChange),
            DurabilityRating = Clamp(
                player.DurabilityRating + durabilityChange)
        };

        return PlayerRatingService.RecalculateOverall(
            developed);
    }

    private static int AttributeChange(
        DynastyState state,
        DynastyPlayer player,
        string attribute,
        int targetChange,
        double multiplier)
    {
        var noise =
            Deterministic(
                state,
                player,
                $"development-{attribute}") %
            3 - 1;

        var change =
            (int)Math.Round(
                targetChange * multiplier +
                noise * .5,
                MidpointRounding.AwayFromZero);

        if (targetChange > 0)
            return Math.Max(0, change);

        return Math.Min(0, change);
    }

    private static double PhysicalMultiplier(int classYear) =>
        classYear switch
        {
            1 => 1.10,
            2 => 1.00,
            3 => .85,
            4 => .65,
            _ => .80
        };

    private static DynastyPlayer ReduceToPotential(
        DynastyPlayer player,
        int targetOverall)
    {
        var difference =
            player.OverallRating -
            targetOverall;

        if (difference <= 0)
            return player;

        var reduced = player with
        {
            SpeedRating = Clamp(
                player.SpeedRating - difference),
            StrengthRating = Clamp(
                player.StrengthRating - difference),
            AgilityRating = Clamp(
                player.AgilityRating - difference),
            AwarenessRating = Clamp(
                player.AwarenessRating - difference),
            TechniqueRating = Clamp(
                player.TechniqueRating - difference),
            DurabilityRating = Clamp(
                player.DurabilityRating - difference)
        };

        return PlayerRatingService.RecalculateOverall(
            reduced);
    }

    private static int Deterministic(
        DynastyState state,
        DynastyPlayer player,
        string purpose) =>
        SimulationSeed.Create(
            state.DynastyId,
            state.SeasonYear,
            player.ClassYear,
            player.PlayerId.ToString("N"),
            purpose);

    private static int Clamp(int value) =>
        Math.Clamp(value, 40, 99);
}
