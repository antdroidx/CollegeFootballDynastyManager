using System.Runtime.CompilerServices;
using DynastyManager.Core.Models;
using DynastyManager.Core.Seasons;

namespace DynastyManager.Core.Simulation;

public static class InteractiveRecruitingService
{
    private static readonly ConditionalWeakTable<IReadOnlyList<RecruitingCommitmentRecord>,
        Dictionary<(int Season, RecruitingSource Source, Guid Id), string>> CommitmentIndexes = new();

    private static string? GetCommittedTeam(DynastyState state, RecruitingSource source, Guid prospectId) =>
        CommitmentIndexes.GetValue(state.RecruitingCommitments, records => records
            .GroupBy(item => (item.SeasonYear, item.Source, item.ProspectId))
            .ToDictionary(group => group.Key, group => group.First().TeamName))
            .GetValueOrDefault((state.SeasonYear, source, prospectId));

    public const int ScoutCost = 25;
    public const int PitchCost = 50;
    public const int ScoutStep = 25;
    public const int MidseasonPortalBasePoints = 250;
    public const int OffseasonRecruitingWeeklyBasePoints = 350;

    public static DynastyState EnsurePhaseInitialized(
        DynastyState state,
        Team userTeam)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(userTeam);

        var isMidseasonPortal =
            state.Phase == SeasonPhase.RegularSeason &&
            state.TransferPortalEntries.Count > 0;
        var isOffseasonRecruiting =
            state.Phase == SeasonPhase.Recruiting;

        if (state.Phase is not
                (SeasonPhase.TransferPortal or SeasonPhase.Recruiting) &&
            !isMidseasonPortal)
        {
            return state;
        }

        if (isMidseasonPortal || isOffseasonRecruiting)
        {
            if (state.RecruitingPointsPhase == state.Phase &&
                state.RecruitingPointsWeek == state.Week)
            {
                return state;
            }
        }
        else if (state.RecruitingPointsPhase == state.Phase)
        {
            return state;
        }

        var prestige = ProgramPrestigeService.GetCurrentPrestige(state, userTeam);

        var rosterCount = state.ActiveRoster.Count(player =>
            player.TeamName.Equals(
                state.UserTeamName,
                StringComparison.OrdinalIgnoreCase));

        var shortageBonus = Math.Max(
            0,
            DynastyRosterRules.MaximumRosterSize - rosterCount) * 28;

        var startingPoints = isMidseasonPortal
            ? MidseasonPortalBasePoints +
              prestige * 2
            : isOffseasonRecruiting
                ? prestige * 4 +
                  shortageBonus / 6
                : prestige * 15 +
                  shortageBonus;

        return state with
        {
            RecruitingPointsRemaining = isMidseasonPortal
                ? Math.Max(
                    MidseasonPortalBasePoints,
                    startingPoints)
                : isOffseasonRecruiting
                    ? Math.Max(
                        OffseasonRecruitingWeeklyBasePoints,
                        startingPoints)
                    : Math.Max(900, startingPoints),
            RecruitingPointsPhase = state.Phase,
            RecruitingPointsWeek =
                isMidseasonPortal || isOffseasonRecruiting
                    ? state.Week
                    : -1
        };
    }

    public static DynastyState Scout(
        DynastyState state,
        RecruitingSource source,
        Guid prospectId,
        bool cpuAssisted = false)
    {
        if (!cpuAssisted && !UserCoachControlService.CanRecruit(state, source, prospectId)) return state;
        if (state.RecruitingPointsRemaining < ScoutCost)
            return state;

        var interaction = GetOrCreateInteraction(
            state,
            source,
            prospectId);

        if (interaction.CommittedTeamName is not null ||
            interaction.ScoutingPercent >= 100)
        {
            return state;
        }

        var scoutStep = source == RecruitingSource.TransferPortal
            ? 50
            : ScoutStep;

        interaction = interaction with
        {
            IsOnTargetBoard = true,
            WasCpuAssisted = cpuAssisted,
            ScoutingPercent = Math.Min(
                100,
                interaction.ScoutingPercent + scoutStep)
        };

        return ReplaceInteraction(
            state with
            {
                RecruitingPointsRemaining =
                    state.RecruitingPointsRemaining - ScoutCost
            },
            interaction);
    }

    public static DynastyState ToggleScholarship(
        DynastyState state,
        Team userTeam,
        RecruitingSource source,
        Guid prospectId,
        bool cpuAssisted = false)
    {
        if (!cpuAssisted && !UserCoachControlService.CanRecruit(state, source, prospectId)) return state;
        ArgumentNullException.ThrowIfNull(userTeam);

        var interaction = GetOrCreateInteraction(
            state,
            source,
            prospectId);

        if (interaction.CommittedTeamName is not null)
            return state;

        var currentInterest =
            RecruitPreferenceService.GetCurrentUserInterest(
                state,
                userTeam,
                source,
                prospectId,
                interaction);

        var addingOffer =
            !interaction.ScholarshipOffered;

        interaction = interaction with
        {
            IsOnTargetBoard = true,
            WasCpuAssisted = cpuAssisted,
            ScholarshipOffered = addingOffer,
            UserInterest = Math.Clamp(
                currentInterest +
                (addingOffer ? 15 : -15),
                0,
                100)
        };

        return ReplaceInteraction(state, interaction);
    }

    public static DynastyState Pitch(
        DynastyState state,
        Team userTeam,
        RecruitingSource source,
        Guid prospectId,
        bool cpuAssisted = false) =>
        Pitch(
            state,
            userTeam,
            source,
            prospectId,
            RecruitPitchType.ProgramPrestige,
            cpuAssisted);

    public static DynastyState Pitch(
        DynastyState state,
        Team userTeam,
        RecruitingSource source,
        Guid prospectId,
        RecruitPitchType pitchType,
        bool cpuAssisted = false)
    {
        if (!cpuAssisted && !UserCoachControlService.CanRecruit(state, source, prospectId)) return state;
        ArgumentNullException.ThrowIfNull(userTeam);

        if (state.RecruitingPointsRemaining < PitchCost)
            return state;

        var interaction = GetOrCreateInteraction(
            state,
            source,
            prospectId);

        if (interaction.CommittedTeamName is not null)
            return state;

        var position = GetProspectPosition(
            state,
            source,
            prospectId);

        var needBonus =
            pitchType == RecruitPitchType.PlayingTime
                ? GetPositionNeedBonus(
                    state,
                    position)
                : 0;

        var preference = RecruitPreferenceService
            .GetPreferences(
                state,
                source,
                prospectId)
            .First(item =>
                item.Type == pitchType);

        var programGrade =
            RecruitPreferenceService.GetProgramGrade(
                state,
                userTeam,
                source,
                prospectId,
                pitchType);

        var currentInterest =
            RecruitPreferenceService.GetCurrentUserInterest(
                state,
                userTeam,
                source,
                prospectId,
                interaction);

        var deterministicBonus =
            SimulationSeed.Create(
                state.DynastyId,
                state.SeasonYear,
                currentInterest,
                prospectId.ToString("N"),
                $"recruiting-pitch-{pitchType}") % 8;

        var prestige = ProgramPrestigeService.GetCurrentPrestige(state, userTeam);

        var staffBonus = GetRecruitingStaffBonus(
            state, userTeam.Name, position);

        var gain =
            5 +
            preference.Importance / 10 +
            programGrade / 12 +
            prestige / 20 +
            staffBonus +
            needBonus +
            deterministicBonus;

        interaction = interaction with
        {
            IsOnTargetBoard = true,
            WasCpuAssisted = cpuAssisted,
            LastPitchType = pitchType,
            UserInterest = Math.Clamp(
                currentInterest + gain,
                0,
                100)
        };

        return ReplaceInteraction(
            state with
            {
                RecruitingPointsRemaining =
                    state.RecruitingPointsRemaining - PitchCost
            },
            interaction, PitchCost);
    }

    public static DynastyState ToggleTargetBoard(
        DynastyState state,
        RecruitingSource source,
        Guid prospectId)
    {
        if (!UserCoachControlService.CanRecruit(state, source, prospectId)) return state;
        var interaction = GetOrCreateInteraction(
            state,
            source,
            prospectId);

        if (interaction.CommittedTeamName is not null)
            return state;

        return ReplaceInteraction(
            state,
            interaction with
            {
                IsOnTargetBoard =
                    !interaction.IsOnTargetBoard,
                WasCpuAssisted = false
            });
    }

    public static DynastyState ResolveCurrentPhase(
        DynastyState state, Team userTeam, IReadOnlyDictionary<string, Team> teamsByName,
        IProgress<(int Completed, int Total, string TeamName)>? progress = null) =>
        CompetingRecruitingService.ResolveWeek(state, teamsByName, progress);

    public static RecruitingInteraction GetInteraction(
        DynastyState state,
        RecruitingSource source,
        Guid prospectId)
    {
        var interaction =
            state.RecruitingInteractions.FirstOrDefault(item =>
                item.SeasonYear == state.SeasonYear &&
                item.Source == source &&
                item.ProspectId == prospectId) ??
            CreateInteraction(
                state,
                source,
                prospectId);

        var userOffer = CompetingRecruitingService.GetOffers(state, source, prospectId).FirstOrDefault(item =>
            item.TeamName.Equals(state.UserTeamName, StringComparison.OrdinalIgnoreCase));
        return interaction with
        {
            CommittedTeamName = GetCommittedTeam(state, source, prospectId) ?? interaction.CommittedTeamName,
            ScholarshipOffered = userOffer is null ? interaction.ScholarshipOffered :
                userOffer.Status == RecruitingOfferStatus.Active,
            WasCpuAssisted = userOffer?.WasCpuAssisted ?? interaction.WasCpuAssisted,
            UserInterest = Math.Clamp(
                userOffer?.Interest ?? interaction.UserInterest,
                0,
                100),
            RivalInterest = Math.Clamp(
                interaction.RivalInterest,
                0,
                100)
        };
    }

    public static (int Minimum, int Maximum) GetScoutedOverallRange(
        DynastyState state,
        RecruitingSource source,
        Guid prospectId)
    {
        var interaction = GetInteraction(
            state,
            source,
            prospectId);

        var overall = GetTrueOverall(
            state,
            source,
            prospectId);

        var uncertainty = source == RecruitingSource.TransferPortal
            ? interaction.ScoutingPercent switch
            {
                >= 75 => 0,
                >= 50 => 1,
                >= 25 => 2,
                _ => 4
            }
            : interaction.ScoutingPercent switch
            {
                >= 100 => 1,
                >= 75 => 3,
                >= 50 => 5,
                >= 25 => 7,
                _ => 10
            };

        var evaluator = state.ScoutingStaff.Count == 0
            ? 65
            : (int)state.ScoutingStaff.Average(item => item.TalentEvaluation);
        var minimumUncertainty =
            source == RecruitingSource.TransferPortal ? 0 : 1;
        uncertainty = Math.Max(
            minimumUncertainty,
            uncertainty - Math.Max(0, evaluator - 70) / 15);
        var errorSpan = Math.Max(1, uncertainty);
        var estimate = overall +
            SimulationSeed.Create(state.DynastyId, state.SeasonYear,
                interaction.ScoutingPercent, prospectId.ToString("N"),
                "overall-estimate") % (errorSpan * 2 + 1) - errorSpan;

        return uncertainty == 0
            ? (overall, overall)
            : (
                Math.Max(40, estimate - uncertainty),
                Math.Min(99, estimate + uncertainty));
    }

    public static (int Minimum, int Maximum) GetScoutedPotentialRange(
        DynastyState state,
        RecruitingSource source,
        Guid prospectId)
    {
        var interaction = GetInteraction(state, source, prospectId);
        var potential = source == RecruitingSource.HighSchool
            ? state.HighSchoolRecruitingPool.First(item =>
                item.RecruitId == prospectId).PotentialRating
            : state.TransferPortalEntries.First(item =>
                item.Player.PlayerId == prospectId).Player.PotentialRating;
        var evaluator = state.ScoutingStaff.Count == 0
            ? 65
            : (int)state.ScoutingStaff.Average(item => item.PotentialEvaluation);
        var uncertainty = source == RecruitingSource.TransferPortal
            ? interaction.ScoutingPercent switch
            {
                >= 100 => 1,
                >= 75 => 2,
                >= 50 => 3,
                >= 25 => 4,
                _ => 6
            }
            : interaction.ScoutingPercent switch
            {
                >= 100 => 2,
                >= 75 => 4,
                >= 50 => 7,
                >= 25 => 10,
                _ => 14
            };
        uncertainty = Math.Max(
            source == RecruitingSource.TransferPortal ? 1 : 2,
            uncertainty - Math.Max(0, evaluator - 70) / 12);
        var estimate = potential +
            SimulationSeed.Create(state.DynastyId, state.SeasonYear,
                interaction.ScoutingPercent, prospectId.ToString("N"),
                "potential-estimate") % (uncertainty * 2 + 1) - uncertainty;
        return (Math.Max(40, estimate - uncertainty),
            Math.Min(99, estimate + uncertainty));
    }

    private static RecruitingInteraction GetOrCreateInteraction(
        DynastyState state, RecruitingSource source, Guid prospectId) =>
        GetInteraction(state, source, prospectId);

    private static RecruitingInteraction CreateInteraction(
        DynastyState state,
        RecruitingSource source,
        Guid prospectId)
    {
        return new RecruitingInteraction
        {
            ProspectId = prospectId,
            Source = source,
            SeasonYear = state.SeasonYear,
            RivalInterest = 0
        };
    }

    private static DynastyState ReplaceInteraction(
        DynastyState state,
        RecruitingInteraction interaction, int effort = 0)
    {
        var interactions =
            state.RecruitingInteractions.ToList();

        var index = interactions.FindIndex(item =>
            item.SeasonYear == interaction.SeasonYear &&
            item.Source == interaction.Source &&
            item.ProspectId == interaction.ProspectId);

        if (index >= 0)
            interactions[index] = interaction;
        else
            interactions.Add(interaction);

        return CompetingRecruitingService.SyncUserOffer(state with
        {
            RecruitingInteractions = interactions
        }, interaction, effort);
    }

    private static Position GetProspectPosition(
        DynastyState state,
        RecruitingSource source,
        Guid prospectId)
    {
        if (source == RecruitingSource.TransferPortal)
        {
            return state.TransferPortalEntries
                .First(entry =>
                    entry.Player.PlayerId == prospectId)
                .Player.Position;
        }

        return state.HighSchoolRecruitingPool
            .First(recruit =>
                recruit.RecruitId == prospectId)
            .Position;
    }

    private static int GetTrueOverall(
        DynastyState state,
        RecruitingSource source,
        Guid prospectId)
    {
        if (source == RecruitingSource.TransferPortal)
        {
            return state.TransferPortalEntries
                .First(entry =>
                    entry.Player.PlayerId == prospectId)
                .Player.OverallRating;
        }

        return state.HighSchoolRecruitingPool
            .First(recruit =>
                recruit.RecruitId == prospectId)
            .TrueOverallRating;
    }

    private static int GetRecruitingStaffBonus(
        DynastyState state,
        string teamName,
        Position position)
    {
        var coordinatorRole = position switch
        {
            Position.QB or Position.RB or Position.FB or Position.WR or Position.TE or Position.OL =>
                StaffRole.OffensiveCoordinator,
            Position.K => StaffRole.SpecialTeamsCoordinator,
            _ => StaffRole.DefensiveCoordinator
        };

        var headCoach = StaffManagementService.GetStaff(
            state, teamName, StaffRole.HeadCoach)?.Recruiting ?? 60;
        var coordinator = StaffManagementService.GetStaff(
            state, teamName, coordinatorRole)?.Recruiting ?? 60;
        var average = (headCoach + coordinator) / 2.0;

        return Math.Clamp(
            (int)Math.Round((average - 60) / 8.0),
            -3,
            5);
    }

    private static int GetPositionNeedBonus(
        DynastyState state,
        Position position)
    {
        var current = state.ActiveRoster.Count(player =>
            player.TeamName.Equals(
                state.UserTeamName,
                StringComparison.OrdinalIgnoreCase) &&
            player.Position == position);

        var target =
            DynastyRosterRules.TargetPositionCounts.TryGetValue(
                position,
                out var count)
                ? count
                : 1;

        return Math.Max(
            0,
            target - current) * 4;
    }
}
