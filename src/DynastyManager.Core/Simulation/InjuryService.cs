using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class InjuryService
{
    private static readonly InjuryBodyArea[] BodyAreas =
        Enum.GetValues<InjuryBodyArea>();

    public static DynastyState AdvanceAndGenerateForCurrentWeek(
        DynastyState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        state = PlayerRatingService.EnsureProfiles(state);

        var injuredAtStart = state.ActiveRoster
            .Where(player => player.CurrentInjury is not null)
            .Select(player => player.PlayerId)
            .ToHashSet();

        var advancedRoster = state.ActiveRoster
            .Select(player =>
            {
                if (player.CurrentInjury is null)
                    return player;

                var remaining =
                    player.CurrentInjury.WeeksRemaining - 1;

                return remaining <= 0
                    ? player with { CurrentInjury = null }
                    : player with
                    {
                        CurrentInjury =
                            player.CurrentInjury with
                            {
                                WeeksRemaining = remaining
                            }
                    };
            })
            .ToArray();

        state = state with
        {
            ActiveRoster = advancedRoster
        };

        var teamsThatPlayed = state.Schedule
            .Where(game =>
                game.SeasonYear == state.SeasonYear &&
                game.Week == state.Week &&
                game.HasPlayed)
            .SelectMany(game => new[]
            {
                game.HomeTeamName,
                game.AwayTeamName
            })
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (teamsThatPlayed.Count == 0)
            return state;

        var candidates = state.ActiveRoster
            .Where(player =>
                teamsThatPlayed.Contains(player.TeamName) &&
                !player.IsRedshirted &&
                player.CurrentInjury is null &&
                !injuredAtStart.Contains(player.PlayerId))
            .GroupBy(player => new
            {
                player.TeamName,
                player.Position
            })
            .SelectMany(group =>
            {
                var starterCount =
                    DynastyRosterRules.StarterPositionCounts
                        .TryGetValue(
                            group.Key.Position,
                            out var count)
                        ? count
                        : 1;

                return group
                    .OrderBy(player =>
                        player.DepthChartOrder > 0
                            ? player.DepthChartOrder
                            : int.MaxValue)
                    .ThenByDescending(player =>
                        player.OverallRating)
                    .Take(starterCount);
            })
            .ToArray();

        foreach (var player in candidates)
        {
            if (!ShouldInjure(state, player))
                continue;

            var severity = GenerateSeverity(
                state,
                player);

            var bodyArea = BodyAreas[
                Deterministic(
                    state,
                    player,
                    "body") %
                BodyAreas.Length];

            var duration = GenerateDuration(
                state,
                player,
                severity);

            state = ApplyInjury(
                state,
                player.PlayerId,
                bodyArea,
                severity,
                duration);
        }

        return state;
    }

    public static DynastyState ApplyInjury(
        DynastyState state,
        Guid playerId,
        InjuryBodyArea bodyArea,
        InjurySeverity severity,
        int durationWeeks)
    {
        ArgumentNullException.ThrowIfNull(state);

        state = PlayerRatingService.EnsureProfiles(state);

        var player = state.ActiveRoster
            .FirstOrDefault(candidate =>
                candidate.PlayerId == playerId);

        if (player is null ||
            player.CurrentInjury is not null)
        {
            return state;
        }

        durationWeeks = Math.Max(1, durationWeeks);

        var regression = GetRegression(
            state,
            player,
            bodyArea,
            severity);

        var injuryId = CreateInjuryId(
            state,
            player,
            bodyArea,
            severity);

        var medicalRedshirt =
            severity is InjurySeverity.Major or InjurySeverity.Severe &&
            state.Week <= 5 &&
            durationWeeks >= 8 &&
            !player.HasRedshirted;

        var updated = player with
        {
            SpeedRating = Math.Clamp(
                player.SpeedRating - regression.Speed,
                40,
                99),
            StrengthRating = Math.Clamp(
                player.StrengthRating - regression.Strength,
                40,
                99),
            AgilityRating = Math.Clamp(
                player.AgilityRating - regression.Agility,
                40,
                99),
            AwarenessRating = Math.Clamp(
                player.AwarenessRating - regression.Awareness,
                40,
                99),
            TechniqueRating = Math.Clamp(
                player.TechniqueRating - regression.Technique,
                40,
                99),
            DurabilityRating = Math.Clamp(
                player.DurabilityRating - regression.Durability,
                25,
                99),
            PotentialRating = Math.Clamp(
                player.PotentialRating - regression.Potential,
                40,
                99),
            IsRedshirted =
                medicalRedshirt ||
                player.IsRedshirted,
            CurrentInjury = new PlayerInjury
            {
                InjuryId = injuryId,
                SeasonYear = state.SeasonYear,
                StartWeek = state.Week,
                BodyArea = bodyArea,
                Severity = severity,
                InitialWeeks = durationWeeks,
                WeeksRemaining = durationWeeks,
                IsMedicalRedshirt = medicalRedshirt
            }
        };

        updated = PlayerRatingService.RecalculateOverall(
            updated);

        if (updated.PotentialRating <
            updated.OverallRating)
        {
            updated = updated with
            {
                PotentialRating =
                    updated.OverallRating
            };
        }

        var history = state.InjuryHistory
            .Append(
                new PlayerInjuryRecord
                {
                    InjuryId = injuryId,
                    SeasonYear = state.SeasonYear,
                    Week = state.Week,
                    PlayerId = player.PlayerId,
                    PlayerName = player.FullName,
                    TeamName = player.TeamName,
                    Position = player.Position,
                    BodyArea = bodyArea,
                    Severity = severity,
                    InitialWeeks = durationWeeks,
                    IsMedicalRedshirt = medicalRedshirt,
                    SpeedLoss = regression.Speed,
                    StrengthLoss = regression.Strength,
                    AgilityLoss = regression.Agility,
                    AwarenessLoss = regression.Awareness,
                    TechniqueLoss = regression.Technique,
                    DurabilityLoss = regression.Durability,
                    PotentialLoss = regression.Potential
                })
            .ToArray();

        return state with
        {
            ActiveRoster = state.ActiveRoster
                .Select(candidate =>
                    candidate.PlayerId == playerId
                        ? updated
                        : candidate)
                .ToArray(),
            InjuryHistory = history
        };
    }

    private static bool ShouldInjure(
        DynastyState state,
        DynastyPlayer player)
    {
        var chanceBasisPoints = Math.Clamp(
            110 +
            (80 - player.DurabilityRating) * 3,
            45,
            210);

        return Deterministic(
                   state,
                   player,
                   "injury-roll") %
               10000 <
               chanceBasisPoints;
    }

    private static InjurySeverity GenerateSeverity(
        DynastyState state,
        DynastyPlayer player)
    {
        var roll = Deterministic(
            state,
            player,
            "severity") % 1000;

        return roll switch
        {
            < 650 => InjurySeverity.Minor,
            < 900 => InjurySeverity.Moderate,
            < 985 => InjurySeverity.Major,
            _ => InjurySeverity.Severe
        };
    }

    private static int GenerateDuration(
        DynastyState state,
        DynastyPlayer player,
        InjurySeverity severity)
    {
        var roll = Deterministic(
            state,
            player,
            "duration");

        return severity switch
        {
            InjurySeverity.Minor =>
                1 + roll % 2,
            InjurySeverity.Moderate =>
                2 + roll % 4,
            InjurySeverity.Major =>
                5 + roll % 6,
            InjurySeverity.Severe =>
                9 + roll % 8,
            _ => 1
        };
    }

    private static Regression GetRegression(
        DynastyState state,
        DynastyPlayer player,
        InjuryBodyArea bodyArea,
        InjurySeverity severity)
    {
        if (severity is
            InjurySeverity.Minor or
            InjurySeverity.Moderate)
        {
            var durabilityLoss =
                severity == InjurySeverity.Moderate &&
                Deterministic(
                    state,
                    player,
                    "moderate-durability") %
                5 == 0
                    ? 1
                    : 0;

            return new Regression(
                0, 0, 0, 0, 0,
                durabilityLoss,
                0);
        }

        var baseLoss = severity ==
                       InjurySeverity.Major
            ? 1 +
              Deterministic(
                  state,
                  player,
                  "major-loss") % 3
            : 3 +
              Deterministic(
                  state,
                  player,
                  "severe-loss") % 6;

        var secondaryLoss = Math.Max(
            1,
            baseLoss / 2);

        var potentialLoss =
            severity == InjurySeverity.Major
                ? Deterministic(
                    state,
                    player,
                    "major-potential") %
                  5 < 2
                    ? 1 +
                      Deterministic(
                          state,
                          player,
                          "major-potential-size") % 3
                    : 0
                : 2 +
                  Deterministic(
                      state,
                      player,
                      "severe-potential") % 7;

        var durabilityLoss =
            severity == InjurySeverity.Major
                ? 1 + baseLoss
                : 3 + baseLoss;

        return bodyArea switch
        {
            InjuryBodyArea.Knee =>
                new(
                    baseLoss,
                    0,
                    baseLoss,
                    0,
                    secondaryLoss,
                    durabilityLoss,
                    potentialLoss),

            InjuryBodyArea.Thigh =>
                new(
                    baseLoss,
                    secondaryLoss,
                    secondaryLoss,
                    0,
                    0,
                    durabilityLoss,
                    potentialLoss),

            InjuryBodyArea.Shoulder =>
                new(
                    0,
                    baseLoss,
                    0,
                    0,
                    baseLoss,
                    durabilityLoss,
                    potentialLoss),

            InjuryBodyArea.Wrist =>
                new(
                    0,
                    0,
                    0,
                    0,
                    baseLoss,
                    secondaryLoss,
                    potentialLoss),

            InjuryBodyArea.Ankle =>
                new(
                    baseLoss,
                    0,
                    baseLoss,
                    0,
                    secondaryLoss,
                    durabilityLoss,
                    potentialLoss),

            InjuryBodyArea.Foot =>
                new(
                    baseLoss,
                    0,
                    baseLoss,
                    0,
                    0,
                    secondaryLoss,
                    potentialLoss),

            InjuryBodyArea.Arm =>
                new(
                    0,
                    baseLoss,
                    0,
                    0,
                    baseLoss,
                    durabilityLoss,
                    potentialLoss),

            InjuryBodyArea.Back =>
                new(
                    0,
                    baseLoss,
                    baseLoss,
                    secondaryLoss,
                    0,
                    durabilityLoss,
                    potentialLoss),

            InjuryBodyArea.Head =>
                new(
                    0,
                    0,
                    0,
                    baseLoss,
                    secondaryLoss,
                    durabilityLoss,
                    Math.Max(
                        potentialLoss,
                        severity == InjurySeverity.Severe
                            ? 3
                            : 0)),

            _ =>
                new(
                    0, 0, 0, 0, 0,
                    durabilityLoss,
                    potentialLoss)
        };
    }

    private static int Deterministic(
        DynastyState state,
        DynastyPlayer player,
        string purpose) =>
        SimulationSeed.Create(
            state.DynastyId,
            state.SeasonYear,
            state.Week,
            player.PlayerId.ToString("N"),
            $"injury-{purpose}");

    private static Guid CreateInjuryId(
        DynastyState state,
        DynastyPlayer player,
        InjuryBodyArea bodyArea,
        InjurySeverity severity)
    {
        var seed1 = SimulationSeed.Create(
            state.DynastyId,
            state.SeasonYear,
            state.Week,
            player.PlayerId.ToString("N"),
            $"{bodyArea}-{severity}-a");

        var seed2 = SimulationSeed.Create(
            state.DynastyId,
            state.SeasonYear,
            state.Week,
            player.PlayerId.ToString("N"),
            $"{bodyArea}-{severity}-b");

        var bytes = new byte[16];
        BitConverter.GetBytes(seed1).CopyTo(
            bytes,
            0);
        BitConverter.GetBytes(seed2).CopyTo(
            bytes,
            4);
        player.PlayerId.ToByteArray()
            .AsSpan(0, 8)
            .CopyTo(bytes.AsSpan(8));

        return new Guid(bytes);
    }

    private sealed record Regression(
        int Speed,
        int Strength,
        int Agility,
        int Awareness,
        int Technique,
        int Durability,
        int Potential);
}
