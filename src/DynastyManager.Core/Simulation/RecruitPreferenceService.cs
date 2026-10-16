using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class RecruitPreferenceService
{
    public static IReadOnlyList<RecruitPreference> GetPreferences(
        DynastyState state,
        RecruitingSource source,
        Guid prospectId)
    {
        ArgumentNullException.ThrowIfNull(state);

        return Enum.GetValues<RecruitPitchType>()
            .Select(type => new RecruitPreference
            {
                Type = type,
                Importance = GetImportance(
                    state,
                    source,
                    prospectId,
                    type)
            })
            .OrderByDescending(item => item.Importance)
            .ThenBy(item => item.Type)
            .ToArray();
    }

    public static IReadOnlyList<RecruitPreference>
        GetRevealedPreferences(
            DynastyState state,
            RecruitingSource source,
            Guid prospectId)
    {
        var interaction =
            InteractiveRecruitingService.GetInteraction(
                state,
                source,
                prospectId);

        var revealCount = interaction.ScoutingPercent switch
        {
            >= 100 => 4,
            >= 75 => 3,
            >= 50 => 2,
            >= 25 => 1,
            _ => 0
        };

        return GetPreferences(
                state,
                source,
                prospectId)
            .Take(revealCount)
            .ToArray();
    }

    public static int GetProgramGrade(
        DynastyState state,
        Team team,
        RecruitingSource source,
        Guid prospectId,
        RecruitPitchType pitchType)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(team);

        var position = GetPosition(
            state,
            source,
            prospectId);
        var prestige = ProgramPrestigeService.GetCurrentPrestige(state, team);
        var recruitingPrestige = Math.Clamp(
            prestige + GetRecentSuccessBonus(state, team),
            35,
            99);

        return pitchType switch
        {
            RecruitPitchType.PlayingTime =>
                GetPlayingTimeGrade(
                    state,
                    team.Name,
                    position),

            RecruitPitchType.ProgramPrestige =>
                recruitingPrestige,

            RecruitPitchType.ProPotential =>
                Math.Clamp(
                    42 + prestige * 3 / 5,
                    40,
                    99),

            RecruitPitchType.Development =>
                Math.Clamp(
                    48 + prestige / 2,
                    40,
                    99),

            RecruitPitchType.SchemeFit =>
                52 + SimulationSeed.Create(
                    state.DynastyId,
                    state.SeasonYear,
                    prestige,
                    team.Name,
                    $"scheme-{prospectId:N}") % 44,

            RecruitPitchType.Proximity =>
                GetProximityGrade(
                    state,
                    team,
                    source,
                    prospectId),

            RecruitPitchType.Facilities =>
                Math.Clamp(
                    45 + prestige / 2,
                    40,
                    99),

            RecruitPitchType.Academics =>
                52 + SimulationSeed.Create(
                    state.DynastyId,
                    0,
                    team.LegacyRegionId,
                    team.Name,
                    "academics") % 44,

            _ => 60
        };
    }

    public static int GetInitialSchoolInterest(
        DynastyState state,
        Team team,
        RecruitingSource source,
        Guid prospectId)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(team);

        if (source == RecruitingSource.TransferPortal &&
            IsTransferOriginTeam(
                state,
                prospectId,
                team.Name))
        {
            return 0;
        }

        var preferences = GetPreferences(
            state,
            source,
            prospectId)
            .Take(4)
            .ToArray();

        var weightedFit = preferences.Length == 0
            ? 60.0
            : preferences.Sum(item =>
                    item.Importance *
                    GetProgramGrade(
                        state,
                        team,
                        source,
                        prospectId,
                        item.Type)) /
              Math.Max(
                  1.0,
                  preferences.Sum(item =>
                      item.Importance));

        var qualityFloor =
            source == RecruitingSource.HighSchool
                ? GetHighSchoolPrestigeFloor(
                    state,
                    prospectId)
                : GetTransferPrestigeFloor(
                    state,
                    prospectId);

        var effectivePrestige = Math.Clamp(
            ProgramPrestigeService.GetCurrentPrestige(
                state,
                team) +
            GetRecentSuccessBonus(
                state,
                team),
            35,
            99);

        var affinity =
            SimulationSeed.Create(
                state.DynastyId,
                state.SeasonYear,
                effectivePrestige,
                team.Name,
                $"initial-interest-{source}-{prospectId:N}") %
            17 - 8;

        var interest =
            42.0 +
            (weightedFit - 55.0) * 0.58 +
            (effectivePrestige - qualityFloor) * 0.52 +
            affinity;

        return Math.Clamp(
            (int)Math.Round(interest),
            5,
            95);
    }

    public static int GetCurrentUserInterest(
        DynastyState state,
        Team userTeam,
        RecruitingSource source,
        Guid prospectId,
        RecruitingInteraction interaction)
    {
        ArgumentNullException.ThrowIfNull(interaction);

        if (interaction.UserInterest > 0)
        {
            return Math.Clamp(
                interaction.UserInterest,
                0,
                100);
        }

        return GetInitialSchoolInterest(
            state,
            userTeam,
            source,
            prospectId);
    }

    public static int GetBestRivalInterest(
        DynastyState state,
        Team userTeam,
        IReadOnlyDictionary<string, Team> teamsByName,
        RecruitingSource source,
        Guid prospectId)
    {
        ArgumentNullException.ThrowIfNull(teamsByName);

        return teamsByName.Values
            .Where(team =>
                !team.Name.Equals(
                    userTeam.Name,
                    StringComparison.OrdinalIgnoreCase))
            .Select(team =>
                GetInitialSchoolInterest(
                    state,
                    team,
                    source,
                    prospectId))
            .DefaultIfEmpty(5)
            .Max();
    }

    public static IReadOnlyList<(string TeamName, int Interest)>
        GetInitialTopSchools(
            DynastyState state,
            IReadOnlyDictionary<string, Team> teamsByName,
            RecruitingSource source,
            Guid prospectId,
            int count = 5)
    {
        ArgumentNullException.ThrowIfNull(teamsByName);

        return teamsByName.Values
            .Select(team => (
                TeamName: team.Name,
                Interest: GetInitialSchoolInterest(
                    state,
                    team,
                    source,
                    prospectId)))
            .OrderByDescending(item =>
                item.Interest)
            .ThenBy(item =>
                item.TeamName,
                StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, count))
            .ToArray();
    }

    public static int GetAttainabilityScore(
        DynastyState state,
        Team team,
        RecruitingSource source,
        Guid prospectId)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(team);

        var prestige = Math.Clamp(
            ProgramPrestigeService.GetCurrentPrestige(
                state,
                team) +
            GetRecentSuccessBonus(
                state,
                team),
            35,
            99);
        var interaction = InteractiveRecruitingService.GetInteraction(
            state,
            source,
            prospectId);
        var currentInterest =
            GetCurrentUserInterest(
                state,
                team,
                source,
                prospectId,
                interaction);

        var qualityFloor = source == RecruitingSource.HighSchool
            ? GetHighSchoolPrestigeFloor(state, prospectId)
            : GetTransferPrestigeFloor(state, prospectId);

        var preferences = GetPreferences(
            state,
            source,
            prospectId)
            .Take(4)
            .ToArray();

        var weightedFit = preferences.Length == 0
            ? 60
            : (int)Math.Round(
                preferences.Sum(item =>
                    item.Importance *
                    GetProgramGrade(
                        state,
                        team,
                        source,
                        prospectId,
                        item.Type)) /
                Math.Max(1.0, preferences.Sum(item => item.Importance)));

        var score =
            45 +
            (prestige - qualityFloor) +
            (int)Math.Round((weightedFit - 55) * .45) +
            (int)Math.Round(
                currentInterest * .35);

        return Math.Clamp(score, 0, 100);
    }

    public static int GetHighSchoolAttainabilityScore(
        DynastyState state,
        Team team,
        HighSchoolRecruit recruit,
        RecruitingInteraction interaction)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(team);
        ArgumentNullException.ThrowIfNull(recruit);
        ArgumentNullException.ThrowIfNull(interaction);

        var prestige = Math.Clamp(
            ProgramPrestigeService.GetCurrentPrestige(
                state,
                team) +
            GetRecentSuccessBonus(
                state,
                team),
            35,
            99);

        var qualityFloor = recruit.StarRating switch
        {
            >= 5 => 78,
            4 => 64,
            3 => 48,
            2 => 32,
            _ => 20
        };

        var preferences = GetPreferences(
            state,
            RecruitingSource.HighSchool,
            recruit.RecruitId)
            .Take(4)
            .ToArray();

        var weightedFit = preferences.Length == 0
            ? 60
            : (int)Math.Round(
                preferences.Sum(item =>
                    item.Importance *
                    GetKnownHighSchoolProgramGrade(
                        state,
                        team,
                        recruit,
                        item.Type,
                        prestige)) /
                Math.Max(
                    1.0,
                    preferences.Sum(item =>
                        item.Importance)));

        var currentInterest =
            GetCurrentUserInterest(
                state,
                team,
                RecruitingSource.HighSchool,
                recruit.RecruitId,
                interaction);

        var score =
            45 +
            (prestige - qualityFloor) +
            (int)Math.Round(
                (weightedFit - 55) * .45) +
            (int)Math.Round(
                currentInterest * .35);

        return Math.Clamp(score, 0, 100);
    }

    private static int GetKnownHighSchoolProgramGrade(
        DynastyState state,
        Team team,
        HighSchoolRecruit recruit,
        RecruitPitchType pitchType,
        int prestige) =>
        pitchType switch
        {
            RecruitPitchType.PlayingTime =>
                GetPlayingTimeGrade(
                    state,
                    team.Name,
                    recruit.Position),

            RecruitPitchType.ProgramPrestige =>
                Math.Clamp(prestige, 35, 99),

            RecruitPitchType.ProPotential =>
                Math.Clamp(
                    42 + prestige * 3 / 5,
                    40,
                    99),

            RecruitPitchType.Development =>
                Math.Clamp(
                    48 + prestige / 2,
                    40,
                    99),

            RecruitPitchType.SchemeFit =>
                52 + SimulationSeed.Create(
                    state.DynastyId,
                    state.SeasonYear,
                    prestige,
                    team.Name,
                    $"scheme-{recruit.RecruitId:N}") % 44,

            RecruitPitchType.Proximity =>
                GetKnownHighSchoolProximityGrade(
                    state,
                    team,
                    recruit),

            RecruitPitchType.Facilities =>
                Math.Clamp(
                    45 + prestige / 2,
                    40,
                    99),

            RecruitPitchType.Academics =>
                52 + SimulationSeed.Create(
                    state.DynastyId,
                    0,
                    team.LegacyRegionId,
                    team.Name,
                    "academics") % 44,

            _ => 60
        };

    private static int GetKnownHighSchoolProximityGrade(
        DynastyState state,
        Team team,
        HighSchoolRecruit recruit)
    {
        var homeRegion =
            RecruitGeography.GetRegion(recruit);

        if (homeRegion ==
            Math.Clamp(
                team.LegacyRegionId,
                0,
                4))
        {
            return 88 + SimulationSeed.Create(
                state.DynastyId,
                state.SeasonYear,
                team.LegacyRegionId,
                team.Name,
                $"home-proximity-{recruit.RecruitId:N}") % 12;
        }

        if (Math.Abs(
                homeRegion -
                Math.Clamp(
                    team.LegacyRegionId,
                    0,
                    4)) == 1)
        {
            return 64 + SimulationSeed.Create(
                state.DynastyId,
                state.SeasonYear,
                team.LegacyRegionId,
                team.Name,
                $"near-proximity-{recruit.RecruitId:N}") % 25;
        }

        return 42 + SimulationSeed.Create(
            state.DynastyId,
            homeRegion,
            team.LegacyRegionId,
            team.Name,
            $"proximity-{recruit.RecruitId:N}") % 40;
    }

    private static bool IsTransferOriginTeam(
        DynastyState state,
        Guid prospectId,
        string teamName)
    {
        var entry = state.TransferPortalEntries
            .FirstOrDefault(item =>
                item.Player.PlayerId == prospectId);

        return entry is not null &&
               entry.OriginTeamName.Equals(
                   teamName,
                   StringComparison.OrdinalIgnoreCase);
    }

    public static int GetRecentSuccessBonus(
        DynastyState state,
        Team team)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(team);

        var season = state.TeamSeasonHistory
            .Where(item =>
                item.TeamName.Equals(
                    team.Name,
                    StringComparison.OrdinalIgnoreCase) &&
                item.SeasonYear <= state.SeasonYear)
            .OrderByDescending(item =>
                item.SeasonYear)
            .FirstOrDefault();

        if (season is null)
            return 0;

        var bonus = season.Wins switch
        {
            >= 12 => 5,
            >= 10 => 4,
            >= 8 => 2,
            >= 6 => 1,
            >= 4 => -1,
            _ => -3
        };

        bonus += season.FinalRanking switch
        {
            1 => 6,
            > 1 and <= 5 => 5,
            > 5 and <= 10 => 4,
            > 10 and <= 25 => 2,
            _ => 0
        };

        if (season.ConferenceChampion)
            bonus += 2;
        if (season.PlayoffParticipant)
            bonus += 3;
        if (season.NationalChampion)
            bonus += 5;

        return Math.Clamp(
            bonus,
            -4,
            10);
    }

    public static string GetAttainabilityLabel(int score) =>
        score switch
        {
            >= 72 => "Strong",
            >= 52 => "Realistic",
            >= 35 => "Reach",
            >= 22 => "Long Shot",
            _ => "Very Unlikely"
        };

    private static int GetHighSchoolPrestigeFloor(
        DynastyState state,
        Guid prospectId)
    {
        var recruit = state.HighSchoolRecruitingPool
            .First(item => item.RecruitId == prospectId);

        return recruit.StarRating switch
        {
            >= 5 => 78,
            4 => 64,
            3 => 48,
            2 => 32,
            _ => 20
        };
    }

    private static int GetTransferPrestigeFloor(
        DynastyState state,
        Guid prospectId)
    {
        var overall = state.TransferPortalEntries
            .First(item => item.Player.PlayerId == prospectId)
            .Player.OverallRating;

        return overall switch
        {
            >= 90 => 78,
            >= 84 => 68,
            >= 78 => 58,
            >= 72 => 48,
            _ => 35
        };
    }

    public static string GetImportanceLabel(int importance) =>
        importance switch
        {
            >= 88 => "Very High",
            >= 74 => "High",
            >= 58 => "Medium",
            >= 42 => "Low",
            _ => "Very Low"
        };

    public static string GetGradeLabel(int grade) =>
        grade switch
        {
            >= 92 => "A+",
            >= 88 => "A",
            >= 84 => "A-",
            >= 80 => "B+",
            >= 75 => "B",
            >= 70 => "B-",
            >= 65 => "C+",
            >= 60 => "C",
            >= 55 => "C-",
            >= 50 => "D+",
            >= 45 => "D",
            _ => "F"
        };

    private static int GetImportance(
        DynastyState state,
        RecruitingSource source,
        Guid prospectId,
        RecruitPitchType type)
    {
        var baseValue =
            32 +
            SimulationSeed.Create(
                state.DynastyId,
                state.SeasonYear,
                (int)type,
                prospectId.ToString("N"),
                $"priority-{source}") % 61;

        if (source == RecruitingSource.TransferPortal)
        {
            baseValue += type switch
            {
                RecruitPitchType.PlayingTime => 18,
                RecruitPitchType.SchemeFit => 12,
                RecruitPitchType.Development => 5,
                _ => 0
            };
        }
        else
        {
            baseValue += type switch
            {
                RecruitPitchType.ProPotential => 8,
                RecruitPitchType.Development => 7,
                RecruitPitchType.ProgramPrestige => 5,
                _ => 0
            };
        }

        return Math.Clamp(baseValue, 25, 100);
    }

    private static int GetPlayingTimeGrade(
        DynastyState state,
        string teamName,
        Position position)
    {
        var count = state.ActiveRoster.Count(player =>
            player.TeamName.Equals(
                teamName,
                StringComparison.OrdinalIgnoreCase) &&
            player.Position == position &&
            !player.IsRedshirted);

        var target =
            DynastyRosterRules.TargetPositionCounts.TryGetValue(
                position,
                out var targetCount)
                ? targetCount
                : Math.Max(1, count);

        var starterCount =
            DynastyRosterRules.StarterPositionCounts.TryGetValue(
                position,
                out var starters)
                ? starters
                : 1;

        if (count <= starterCount)
            return 98;

        var deficit = target - count;

        return Math.Clamp(
            62 + deficit * 8,
            38,
            96);
    }

    private static int GetProximityGrade(
        DynastyState state,
        Team team,
        RecruitingSource source,
        Guid prospectId)
    {
        var homeRegion = GetHomeRegion(
            state,
            source,
            prospectId);

        if (homeRegion is not null &&
            homeRegion.Value == team.LegacyRegionId)
        {
            return 88 + SimulationSeed.Create(
                state.DynastyId, state.SeasonYear,
                team.LegacyRegionId, team.Name,
                $"home-proximity-{prospectId:N}") % 12;
        }

        if (homeRegion is not null &&
            Math.Abs(homeRegion.Value - team.LegacyRegionId) == 1)
        {
            return 64 + SimulationSeed.Create(
                state.DynastyId, state.SeasonYear,
                team.LegacyRegionId, team.Name,
                $"near-proximity-{prospectId:N}") % 25;
        }

        return 42 + SimulationSeed.Create(
            state.DynastyId,
            homeRegion ?? 0,
            team.LegacyRegionId,
            team.Name,
            $"proximity-{prospectId:N}") % 40;
    }

    private static int? GetHomeRegion(
        DynastyState state,
        RecruitingSource source,
        Guid prospectId)
    {
        if (source == RecruitingSource.HighSchool)
        {
            return state.HighSchoolRecruitingPool
                .FirstOrDefault(recruit =>
                    recruit.RecruitId == prospectId)
                ?.HomeRegion;
        }

        var entry = state.TransferPortalEntries
            .FirstOrDefault(item =>
                item.Player.PlayerId == prospectId);

        if (entry is null)
            return null;

        return null;
    }

    private static Position GetPosition(
        DynastyState state,
        RecruitingSource source,
        Guid prospectId)
    {
        if (source == RecruitingSource.HighSchool)
        {
            return state.HighSchoolRecruitingPool
                .First(recruit =>
                    recruit.RecruitId == prospectId)
                .Position;
        }

        return state.TransferPortalEntries
            .First(entry =>
                entry.Player.PlayerId == prospectId)
            .Player.Position;
    }
}
