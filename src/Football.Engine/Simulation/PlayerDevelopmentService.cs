using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class PlayerDevelopmentService
{
    public static IReadOnlyList<PlayerDevelopmentRecord> GetLatestRecords(
        IEnumerable<PlayerDevelopmentRecord> history,
        string teamName)
    {
        ArgumentNullException.ThrowIfNull(history);
        ArgumentException.ThrowIfNullOrWhiteSpace(teamName);

        var teamRecords = history
            .Where(record => record.TeamName.Equals(
                teamName,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (teamRecords.Length == 0)
            return Array.Empty<PlayerDevelopmentRecord>();

        var latestSeason = teamRecords.Max(record => record.SeasonYear);
        return teamRecords
            .Where(record => record.SeasonYear == latestSeason)
            .GroupBy(record => record.PlayerId)
            .Select(group => group
                .OrderBy(record => record.Stage == PlayerDevelopmentStage.Offseason ? 0 : 1)
                .ThenByDescending(record => record.Week)
                .First())
            .ToArray();
    }

    public static DynastyState ApplyOffseasonDevelopment(
        DynastyState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.Phase != SeasonPhase.Offseason)
            return state;

        if (state.PlayerDevelopmentHistory.Any(record =>
                record.SeasonYear == state.SeasonYear &&
                record.Stage == PlayerDevelopmentStage.Offseason))
        {
            return state;
        }

        state = PlayerRatingService.EnsureProfiles(state);

        var updatedPlayers = new List<DynastyPlayer>(
            state.ActiveRoster.Count);

        var history = state.PlayerDevelopmentHistory.ToList();
        var moraleModifiers = state.ActiveRoster.Select(p => p.TeamName).Distinct().ToDictionary(t => t, t => GameMoraleService.TrainingModifier(GameMoraleService.SeasonMorale(state, t)));

        foreach (var player in state.ActiveRoster)
        {
            var beforeOverall = player.OverallRating;
            var beforePotential = player.PotentialRating;

            var developed = DevelopPlayer(
                state,
                player,
                false, moraleModifiers[player.TeamName]);

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
                AfterPotential = developed.PotentialRating,
                Stage = PlayerDevelopmentStage.Offseason
            });
        }

        return RosterManagementService.NormalizeAllDepthCharts(
            state with
            {
                // CPU coaches reassess their depth each offseason. Preserve the
                // human's manual ordering; new recruits must be able to beat a
                // returning low-rated backup for an AI starting job.
                ActiveRoster = updatedPlayers.Select(p => p.TeamName == state.UserTeamName ? p : p with { DepthChartOrder = 0 }).ToArray(),
                PlayerDevelopmentHistory = history
            });
    }

    public static DynastyState ApplyMidseasonDevelopment(
        DynastyState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.Phase != SeasonPhase.RegularSeason || state.Week < 3 || state.Week > 12 || state.Week % 3 != 0 ||
            state.PlayerDevelopmentHistory.Any(record =>
                record.SeasonYear == state.SeasonYear &&
                record.Stage == PlayerDevelopmentStage.Midseason && record.Week == state.Week))
        {
            return state;
        }

        state = PlayerRatingService.EnsureProfiles(state);
        var history = state.PlayerDevelopmentHistory.ToList();
        var moraleModifiers = state.ActiveRoster.Select(p => p.TeamName).Distinct().ToDictionary(t => t, t => GameMoraleService.TrainingModifier(GameMoraleService.SeasonMorale(state, t)));
        var players = state.ActiveRoster.Select(player =>
        {
            var developed = DevelopPlayer(state, player, true, moraleModifiers[player.TeamName]);
            history.Add(new PlayerDevelopmentRecord
            {
                SeasonYear = state.SeasonYear,
                PlayerId = player.PlayerId,
                PlayerName = player.FullName,
                TeamName = player.TeamName,
                Position = player.Position,
                BeforeOverall = player.OverallRating,
                AfterOverall = developed.OverallRating,
                BeforePotential = player.PotentialRating,
                AfterPotential = developed.PotentialRating,
                Stage = PlayerDevelopmentStage.Midseason, Week = state.Week
            });
            return developed;
        }).ToArray();

        return RosterManagementService.NormalizeAllDepthCharts(state with
        {
            ActiveRoster = players,
            PlayerDevelopmentHistory = history
        });
    }

    private static DynastyPlayer DevelopPlayer(
        DynastyState state,
        DynastyPlayer player,
        bool midseason, int moraleModifier)
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

        var staffFactor = GetStaffDevelopmentFactor(state, player);

        var targetChange = Math.Clamp(
            gapFactor +
            classFactor +
            roleFactor +
            redshirtFactor +
            injuryFactor +
            staffFactor +
            (midseason ? GameMoraleService.TrainingModifier(GameMoraleService.PlayerMorale(state, player)) : moraleModifier) +
            random,
            -3,
            6);

        if (midseason)
        {
            targetChange = targetChange switch
            {
                >= 4 => 2,
                >= 1 => 1,
                <= -2 => -1,
                _ => 0
            };
        }

        var developed = ApplyAttributeDevelopment(
            state,
            player,
            targetChange, midseason);

        if (!midseason)
        {
            developed = developed with
            {
                CurrentInjury = null
            };
        }

        while (developed.OverallRating >
            developed.PotentialRating + 1 && developed.OverallRating > 40)
        {
            developed = ReduceToPotential(
                developed,
                developed.PotentialRating + 1);
        }

        var baseline = player.RatingBaselineSeason == state.SeasonYear && player.SeasonStartOverall > 0 ? player.SeasonStartOverall : player.OverallRating;
        var minimum = midseason ? Math.Max(40, baseline - 2) : Math.Max(40, player.OverallRating - 2);
        var maximum = Math.Min(player.PotentialRating + 1, midseason ? baseline + 2 : player.OverallRating + 4);
        return BoundDevelopment(player, developed, minimum, Math.Max(minimum, maximum));
    }

    private static DynastyPlayer BoundDevelopment(DynastyPlayer original, DynastyPlayer developed, int minimum, int maximum)
    {
        var best = original with { CurrentInjury = developed.CurrentInjury };
        for (var step = 1; step <= 20; step++)
        {
            var fraction = step / 20d;
            int Blend(int before, int after) => (int)Math.Round(before + (after - before) * fraction);
            var skills = developed.Skills!;
            // Reduce the developed profile back toward its starting attributes without falsifying OVR.
            var blendedSkills = skills with
            {
                PassingPower = Blend(original.Skills!.PassingPower, skills.PassingPower),
                PassingAccuracy = Blend(original.Skills!.PassingAccuracy, skills.PassingAccuracy),
                Catching = Blend(original.Skills!.Catching, skills.Catching),
                BreakTackle = Blend(original.Skills!.BreakTackle, skills.BreakTackle),
                Tackling = Blend(original.Skills!.Tackling, skills.Tackling),
                RunBlocking = Blend(original.Skills!.RunBlocking, skills.RunBlocking),
                PassBlocking = Blend(original.Skills!.PassBlocking, skills.PassBlocking),
                BallCarrying = Blend(original.Skills!.BallCarrying, skills.BallCarrying),
                Acceleration = Blend(original.Skills!.Acceleration, skills.Acceleration),
                Leadership = Blend(original.Skills!.Leadership, skills.Leadership),
                Discipline = Blend(original.Skills!.Discipline, skills.Discipline),
                Jumping = Blend(original.Skills!.Jumping, skills.Jumping),
                KickPower = Blend(original.Skills!.KickPower, skills.KickPower),
                KickAccuracy = Blend(original.Skills!.KickAccuracy, skills.KickAccuracy)
            };
            var candidate = PlayerRatingService.RecalculateOverall(developed with
            {
                SpeedRating = Blend(original.SpeedRating, developed.SpeedRating), StrengthRating = Blend(original.StrengthRating, developed.StrengthRating),
                AgilityRating = Blend(original.AgilityRating, developed.AgilityRating), AwarenessRating = Blend(original.AwarenessRating, developed.AwarenessRating),
                InjuryRating = Blend(original.InjuryRating, developed.InjuryRating), Skills = blendedSkills
            });
            if (candidate.OverallRating >= minimum && candidate.OverallRating <= maximum) best = candidate;
        }
        return best;
    }

    private static DynastyPlayer ApplyAttributeDevelopment(
        DynastyState state,
        DynastyPlayer player,
        int targetChange, bool midseason)
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
            Skills = PlayerSkillService.Train(state, player, targetChange, midseason),
            InjuryRating = Clamp(
                player.InjuryRating + durabilityChange)
        };

        var baseline = player.RatingBaselineSeason == state.SeasonYear && player.SeasonStartOverall > 0 ? player.SeasonStartOverall : player.OverallRating;
        var minimum = midseason ? Math.Max(40, baseline - 2) : Math.Max(40, player.OverallRating - 2);
        var maximum = Math.Min(player.PotentialRating + 1, midseason ? baseline + 2 : player.OverallRating + 4);
        return BoundDevelopment(player, developed, minimum, Math.Max(minimum, maximum));
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

    private static int GetStaffDevelopmentFactor(
        DynastyState state,
        DynastyPlayer player)
    {
        var coordinatorRole = player.Position switch
        {
            Position.QB or Position.RB or Position.FB or Position.WR or Position.TE or Position.OL =>
                StaffRole.OffensiveCoordinator,
            Position.DE or Position.DT or Position.OLB or Position.MLB or Position.CB or Position.FS or Position.SS =>
                StaffRole.DefensiveCoordinator,
            _ => StaffRole.SpecialTeamsCoordinator
        };

        var headCoach = StaffManagementService.GetStaff(state, player.TeamName, StaffRole.HeadCoach)?.PlayerDevelopment ?? 60;
        var coordinator = StaffManagementService.GetStaff(state, player.TeamName, coordinatorRole)?.PlayerDevelopment ?? 60;
        var strengthCoach = StaffManagementService.GetStaff(state, player.TeamName, StaffRole.StrengthConditioningDirector);
        var strength = ((strengthCoach?.Conditioning ?? 60) + (strengthCoach?.PlayerDevelopment ?? 60)) / 2;

        var average = (headCoach + coordinator + strength) / 3.0;
        return average switch
        {
            >= 84 => 2,
            >= 68 => 1,
            < 48 => -1,
            _ => 0
        };
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
            InjuryRating = Clamp(
                player.InjuryRating - difference)
        };

        reduced = reduced with { Skills = PlayerSkillService.Shift(player.Skills!, -difference) };
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
