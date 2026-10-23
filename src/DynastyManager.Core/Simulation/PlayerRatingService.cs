using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class PlayerRatingService
{
    public static DynastyState EnsureProfiles(DynastyState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return state with
        {
            OverallCalibrationVersion = 1,
            ActiveRoster = PlayerJerseyService.Assign(OffensiveLineService.AssignMissingRoles(state.ActiveRoster)
                .Select(player =>
                {
                    var updated = InitializeForScale(state, player);
                    if (state.OverallCalibrationVersion == 0 && updated.OverallRating != player.OverallRating)
                        updated = updated with { RatingBaselineSeason = state.SeasonYear, SeasonStartOverall = updated.OverallRating };
                    return WithSeasonBaseline(state, updated);
                }).ToArray()),
            TransferPortalEntries = state.TransferPortalEntries.Select(e => e with { Player = InitializeForScale(state, e.Player) }).ToArray(),
            PendingTransferCommitments = state.PendingTransferCommitments.Select(e => e with { Player = InitializeForScale(state, e.Player) }).ToArray(),
            HighSchoolRecruitingPool = state.OverallCalibrationVersion == 0
                ? state.HighSchoolRecruitingPool.Select(r =>
                {
                    var profile = InitializeForScale(state, r.AttributeProfile ?? new DynastyPlayer
                    {
                        PlayerId = r.RecruitId, FullName = r.FullName, TeamName = "", Position = r.Position,
                        ClassYear = 1, OverallRating = r.TrueOverallRating, PotentialRating = r.PotentialRating,
                        TalentLevel = r.StarRating * 2, OriginalRecruitingStars = r.StarRating,
                        HeightInches = r.HeightInches, WeightPounds = r.WeightPounds
                    });
                    return r with { AttributeProfile = profile, TrueOverallRating = profile.OverallRating, PotentialRating = profile.PotentialRating };
                }).ToArray() : state.HighSchoolRecruitingPool
        };
    }

    private static DynastyPlayer InitializeForScale(DynastyState state, DynastyPlayer player)
    {
        var updated = EnsureInitialized(state, player);
        // A one-time scale migration preserves remaining development headroom.
        // It never trains attributes or rebalances a population of players.
        if (state.OverallCalibrationVersion == 0 && player.Skills is not null && player.SpeedRating > 0 && player.PotentialRating > 0)
            updated = updated with { PotentialRating = Math.Clamp(player.PotentialRating + updated.OverallRating - player.OverallRating, updated.OverallRating, 99) };
        return updated;
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
        if (player.NcaaPositionCode is null)
        {
            var role = Deterministic(state, player, "position-code");
            player = player with { NcaaPositionCode = player.Position switch
            {
                Position.RB => role % 8 == 0 ? 2 : 1, Position.OL => 5 + role % 5,
                Position.DE => 10 + role % 2, Position.OLB => role % 2 == 0 ? 13 : 15,
                Position.K => 19 + role % 2, _ => Ncaa06RatingService.PositionCode(player)
            } };
        }

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
        var basis = initialized;
        var bestDistance = Math.Abs(CalculateOverall(initialized) - target);
        var lowShift = -98;
        var highShift = 98;
        while (lowShift <= highShift)
        {
            var shift = (lowShift + highShift) / 2;
            var skills = basis.Skills!;
            var candidate = basis with
            {
                SpeedRating = ClampRating(basis.SpeedRating + shift),
                StrengthRating = ClampRating(basis.StrengthRating + shift),
                AgilityRating = ClampRating(basis.AgilityRating + shift),
                AwarenessRating = ClampRating(basis.AwarenessRating + shift),
                Skills = PlayerSkillService.Shift(skills, shift) with
                { Leadership = skills.Leadership, Discipline = skills.Discipline }
            };
            var current = CalculateOverall(candidate);
            if (Math.Abs(current - target) < bestDistance)
            {
                initialized = candidate;
                bestDistance = Math.Abs(current - target);
            }
            if (current == target) break;
            if (current < target) lowShift = shift + 1;
            else highShift = shift - 1;
        }
        // Steep curve segments can skip an integer under a uniform shift.
        // Refine individual attributes instead of overwriting the calculated OVR.
        for (var attempt = 0; attempt < 4 && bestDistance > 1; attempt++)
        {
            var direction = Math.Sign(target - CalculateOverall(initialized));
            var improved = false;
            foreach (var field in Ncaa06RosterRatings.RatingFields)
            {
                var values = Ncaa06RosterRatings.Export(initialized).ToDictionary(p => p.Key, p => p.Value);
                values[field] = Math.Clamp(values[field] + direction, 40, 99);
                var candidate = Ncaa06RosterRatings.Apply(initialized, values);
                var distance = Math.Abs(candidate.OverallRating - target);
                if (distance >= bestDistance) continue;
                initialized = candidate;
                bestDistance = distance;
                improved = true;
                if (bestDistance <= 1) break;
            }
            if (!improved) break;
        }
        var overall = CalculateOverall(initialized);
        return initialized with { OverallRating = overall, PotentialRating = Math.Max(overall, potential) };
    }

    private static DynastyPlayer WithSeasonBaseline(DynastyState state, DynastyPlayer player) =>
        player.RatingBaselineSeason == state.SeasonYear && player.SeasonStartOverall > 0 ? player :
            player with { RatingBaselineSeason = state.SeasonYear, SeasonStartOverall = player.OverallRating };

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
