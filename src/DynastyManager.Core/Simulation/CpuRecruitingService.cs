using DynastyManager.Core.Models;
using DynastyManager.Core.Seasons;

namespace DynastyManager.Core.Simulation;

/// <summary>
/// User recruiting assistance operates the visible recruiting
/// system as the player: target board, scholarships, scouting and pitches. The
/// workload setting controls how much of the available recruiting budget staff
/// may use, while manual user-managed prospects are never overwritten.
/// </summary>
public static class CpuRecruitingService
{
    public static DynastyState ApplyWeeklyUserAssistance(
        DynastyState state,
        Team userTeam)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(userTeam);
        state = CompetingRecruitingService.RestoreUserBoard(state);

        var workload = Math.Clamp(
            state.RecruitingAssistanceWorkloadPercent,
            0,
            100);
        var sources = GetAssistanceSources(state);

        if (!state.RecruitingAssistanceEnabled ||
            workload <= 0 ||
            sources.Length == 0 ||
            (state.RecruitingAssistanceSeasonYear == state.SeasonYear &&
             state.RecruitingAssistanceWeek == state.Week &&
             state.RecruitingAssistancePhase == state.Phase))
        {
            return state;
        }

        var startingPoints = state.RecruitingPointsRemaining;
        var pointBudget = Math.Clamp(
            (int)Math.Floor(
                startingPoints * (workload / 100.0)),
            0,
            startingPoints);

        var rosterCount = CountTeamRoster(
            state.ActiveRoster,
            state.UserTeamName);
        var openSlots = Math.Max(
            0,
            DynastyRosterRules.MaximumRosterSize - rosterCount);

        var workloadFactor =
            0.35 + workload / 100.0 * 0.65;

        var targetSource = state.Phase == SeasonPhase.Recruiting
            ? RecruitingSource.HighSchool
            : RecruitingSource.TransferPortal;

        var currentOffers = state.RecruitingInteractions.Count(item =>
            item.SeasonYear == state.SeasonYear &&
            item.Source == targetSource &&
            item.CommittedTeamName is null &&
            item.ScholarshipOffered);

        var minimumHighSchoolBoard = workload switch
        {
            >= 90 => 18,
            >= 60 => 14,
            _ => 10
        };

        var baseDesiredOffers = targetSource ==
            RecruitingSource.HighSchool
                ? Math.Min(
                    40,
                    Math.Max(
                        minimumHighSchoolBoard,
                        (int)Math.Ceiling(
                            Math.Max(4, openSlots) * 2.5)))
                : Math.Max(
                    6,
                    (int)Math.Ceiling(
                        Math.Max(4, openSlots) * 0.85));

        var desiredOpenOffers =
            (int)Math.Ceiling(
                baseDesiredOffers * workloadFactor);

        var additionsNeeded = Math.Max(
            0,
            desiredOpenOffers - currentOffers);

        var acquisitionSources = state.Phase == SeasonPhase.Recruiting
            ? new[] { RecruitingSource.HighSchool }
            : sources;

        var unavailableIds = state.ActiveRoster.Select(player => player.PlayerId)
            .Concat(state.PendingTransferCommitments.Select(item => item.ProspectId))
            .Concat(state.RecruitingCommitments.Where(item => item.SeasonYear == state.SeasonYear)
                .Select(item => item.ProspectId)).ToHashSet();

        var availableCandidates = acquisitionSources
            .SelectMany(source =>
                GetCandidates(state, source)
                    .Select(candidate =>
                        new AssistanceCandidate(
                            source,
                            candidate)))
            .Where(item => !unavailableIds.Contains(item.Candidate.ProspectId))
            .Where(item =>
                item.Source != RecruitingSource.TransferPortal ||
                !item.Candidate.OriginTeamName.Equals(
                    state.UserTeamName,
                    StringComparison.OrdinalIgnoreCase))
            .Where(item =>
            {
                var interaction =
                    InteractiveRecruitingService.GetInteraction(
                        state,
                        item.Source,
                        item.Candidate.ProspectId);

                if (interaction.CommittedTeamName is not null ||
                    interaction.ScholarshipOffered ||
                    IsManualInteraction(interaction))
                {
                    return false;
                }

                return IsReasonableUserAssistanceTarget(
                    state,
                    userTeam,
                    item.Source,
                    item.Candidate,
                    workload);
            })
            .OrderByDescending(item =>
                GetUserAssistanceScore(
                    state,
                    userTeam,
                    item.Source,
                    item.Candidate))
            .ThenByDescending(item =>
                item.Candidate.OverallRating)
            .ThenBy(item =>
                item.Candidate.FullName,
                StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var offersAdded = 0;
        foreach (var candidate in availableCandidates)
        {
            if (offersAdded >= additionsNeeded) break;
            if (candidate.Source == RecruitingSource.TransferPortal &&
                !IsUsefulTransfer(state, userTeam, candidate.Candidate, includeOffers: true))
                continue;
            offersAdded++;
            state = InteractiveRecruitingService
                .ToggleScholarship(
                    state,
                    userTeam,
                    candidate.Source,
                    candidate.Candidate.ProspectId,
                    cpuAssisted: true);
        }

        var assistedTargets = GetAssistedTargets(
            state,
            userTeam,
            sources);

        if (assistedTargets.Length > 0 &&
            pointBudget >=
                InteractiveRecruitingService.ScoutCost)
        {
            var chiefScout =
                StaffManagementService.GetStaff(
                    state,
                    state.UserTeamName,
                    StaffRole.ChiefScout);
            var scoutQuality =
                GetScoutQuality(state, chiefScout);
            var highSchoolRecruitingPhase =
                state.Phase == SeasonPhase.Recruiting;

            var scoutingShare = highSchoolRecruitingPhase
                ? Math.Clamp(
                    0.10 +
                    (scoutQuality - 50) / 300.0,
                    0.08,
                    0.22)
                : Math.Clamp(
                    0.20 +
                    (scoutQuality - 50) / 200.0,
                    0.18,
                    0.42);

            var scoutingBudget =
                (int)Math.Floor(
                    pointBudget * scoutingShare);

            var targetScoutPercent =
                highSchoolRecruitingPhase
                    ? workload switch
                    {
                        >= 90 => 75,
                        >= 60 => 50,
                        _ => 25
                    }
                    : workload switch
                    {
                        >= 90 => 100,
                        >= 60 => 75,
                        _ => 50
                    };

            if (!highSchoolRecruitingPhase &&
                scoutQuality >= 85)
            {
                targetScoutPercent = Math.Min(
                    100,
                    targetScoutPercent + 25);
            }

            var scoutingSpent = 0;
            var madeProgress = true;

            while (madeProgress &&
                   scoutingSpent +
                       InteractiveRecruitingService.ScoutCost <=
                   scoutingBudget)
            {
                madeProgress = false;

                foreach (var target in assistedTargets)
                {
                    var interaction =
                        InteractiveRecruitingService.GetInteraction(
                            state,
                            target.Source,
                            target.Candidate.ProspectId);

                    if (interaction.ScoutingPercent >=
                            targetScoutPercent ||
                        state.RecruitingPointsRemaining <
                            InteractiveRecruitingService.ScoutCost ||
                        scoutingSpent +
                            InteractiveRecruitingService.ScoutCost >
                            scoutingBudget)
                    {
                        continue;
                    }

                    var before =
                        state.RecruitingPointsRemaining;
                    state = InteractiveRecruitingService.Scout(
                        state,
                        target.Source,
                        target.Candidate.ProspectId,
                        cpuAssisted: true);

                    if (state.RecruitingPointsRemaining <
                        before)
                    {
                        scoutingSpent +=
                            before -
                            state.RecruitingPointsRemaining;
                        madeProgress = true;
                    }
                }
            }
        }

        var spentSoFar =
            startingPoints - state.RecruitingPointsRemaining;
        var remainingBudget =
            Math.Max(0, pointBudget - spentSoFar);

        assistedTargets = GetAssistedTargets(
            state,
            userTeam,
            sources);

        var pitchProgress = true;
        while (pitchProgress &&
               remainingBudget >=
                   InteractiveRecruitingService.PitchCost &&
               state.RecruitingPointsRemaining >=
                   InteractiveRecruitingService.PitchCost)
        {
            pitchProgress = false;

            foreach (var target in assistedTargets)
            {
                if (remainingBudget <
                        InteractiveRecruitingService.PitchCost ||
                    state.RecruitingPointsRemaining <
                        InteractiveRecruitingService.PitchCost)
                {
                    break;
                }

                var interaction =
                    InteractiveRecruitingService.GetInteraction(
                        state,
                        target.Source,
                        target.Candidate.ProspectId);
                if (interaction.CommittedTeamName is not null ||
                    !interaction.ScholarshipOffered ||
                    !interaction.WasCpuAssisted)
                {
                    continue;
                }

                var pitchType = GetBestPitchType(
                    state,
                    userTeam,
                    target.Source,
                    target.Candidate);

                var before =
                    state.RecruitingPointsRemaining;
                state = InteractiveRecruitingService.Pitch(
                    state,
                    userTeam,
                    target.Source,
                    target.Candidate.ProspectId,
                    pitchType,
                    cpuAssisted: true);

                var spent =
                    before - state.RecruitingPointsRemaining;
                if (spent > 0)
                {
                    remainingBudget -= spent;
                    pitchProgress = true;
                }
            }
        }

        if (remainingBudget >=
                InteractiveRecruitingService.ScoutCost &&
            state.RecruitingPointsRemaining >=
                InteractiveRecruitingService.ScoutCost)
        {
            assistedTargets = GetAssistedTargets(
                state,
                userTeam,
                sources);

            foreach (var target in assistedTargets)
            {
                if (remainingBudget <
                        InteractiveRecruitingService.ScoutCost ||
                    state.RecruitingPointsRemaining <
                        InteractiveRecruitingService.ScoutCost)
                {
                    break;
                }

                var interaction =
                    InteractiveRecruitingService.GetInteraction(
                        state,
                        target.Source,
                        target.Candidate.ProspectId);
                if (interaction.ScoutingPercent >= 100)
                    continue;

                var before =
                    state.RecruitingPointsRemaining;
                state = InteractiveRecruitingService.Scout(
                    state,
                    target.Source,
                    target.Candidate.ProspectId,
                    cpuAssisted: true);
                var spent =
                    before - state.RecruitingPointsRemaining;
                remainingBudget -= spent;
            }
        }

        return state with
        {
            RecruitingAssistanceSeasonYear =
                state.SeasonYear,
            RecruitingAssistanceWeek = state.Week,
            RecruitingAssistancePhase = state.Phase
        };
    }

    public static DynastyState AdvanceCpuRecruitingWeek(
        DynastyState state, IReadOnlyDictionary<string, Team> teamsByName) =>
        state.Phase == SeasonPhase.Recruiting
            ? CompetingRecruitingService.ResolveWeek(state, teamsByName) : state;

    public static DynastyState ApplyPhaseAssistance(
        DynastyState state, IReadOnlyDictionary<string, Team> teamsByName, bool assistUserTeam = true,
        IProgress<(int Completed, int Total, string TeamName)>? progress = null) =>
        state.Phase is SeasonPhase.TransferPortal or SeasonPhase.Recruiting
            ? CompetingRecruitingService.ResolveWeek(state, teamsByName, progress) : state;

    private static RecruitingSource[] GetAssistanceSources(
        DynastyState state) =>
        state.Phase switch
        {
            SeasonPhase.TransferPortal =>
                new[] { RecruitingSource.TransferPortal },
            SeasonPhase.Recruiting =>
                new[]
                {
                    RecruitingSource.HighSchool,
                    RecruitingSource.TransferPortal
                },
            SeasonPhase.RegularSeason
                when state.TransferPortalEntries.Count > 0 =>
                new[] { RecruitingSource.TransferPortal },
            _ => Array.Empty<RecruitingSource>()
        };

    private static bool IsManualInteraction(
        RecruitingInteraction interaction) =>
        !interaction.WasCpuAssisted &&
        (interaction.IsOnTargetBoard ||
         interaction.ScholarshipOffered ||
         interaction.IsPriorityScout ||
         interaction.LastPitchType is not null ||
         interaction.UserInterest > 0);

    private static AssistanceCandidate[] GetAssistedTargets(
        DynastyState state,
        Team userTeam,
        IReadOnlyCollection<RecruitingSource> sources) =>
        state.RecruitingInteractions
            .Where(interaction =>
                interaction.SeasonYear == state.SeasonYear &&
                sources.Contains(interaction.Source) &&
                interaction.WasCpuAssisted &&
                interaction.ScholarshipOffered &&
                interaction.CommittedTeamName is null)
            .Select(interaction =>
            {
                var candidate = GetCandidates(
                        state,
                        interaction.Source)
                    .FirstOrDefault(item =>
                        item.ProspectId ==
                        interaction.ProspectId);
                return candidate is null
                    ? null
                    : new AssistanceCandidate(
                        interaction.Source,
                        candidate);
            })
            .Where(item => item is not null)
            .Select(item => item!)
            .Where(item => item.Source != RecruitingSource.TransferPortal ||
                IsUsefulTransfer(state, userTeam, item.Candidate))
            .OrderByDescending(item =>
                GetUserAssistanceScore(
                    state,
                    userTeam,
                    item.Source,
                    item.Candidate))
            .ToArray();

    private static bool IsUsefulTransfer(DynastyState state, Team team, Candidate candidate,
        IEnumerable<DynastyPlayer>? roster = null, bool includeOffers = false)
    {
        var plannedRoster = roster ?? state.ActiveRoster;
        if (includeOffers)
        {
            var offered = state.RecruitingInteractions.Where(item =>
                item.SeasonYear == state.SeasonYear &&
                item.Source == RecruitingSource.TransferPortal &&
                item.ScholarshipOffered && item.CommittedTeamName is null &&
                item.ProspectId != candidate.ProspectId).Select(item => item.ProspectId).ToHashSet();
            plannedRoster = plannedRoster.Concat(state.TransferPortalEntries
                .Where(item => offered.Contains(item.Player.PlayerId))
                .Select(item => item.Player with { TeamName = team.Name }));
        }
        return TransferRosterPlanningService.GetValue(state, team,
            candidate.ToPlayer(team.Name), plannedRoster) > 0;
    }

    private static bool IsReasonableUserAssistanceTarget(
        DynastyState state,
        Team userTeam,
        RecruitingSource source,
        Candidate candidate,
        int workload)
    {
        var attainability =
            RecruitPreferenceService.GetAttainabilityScore(
                state,
                userTeam,
                source,
                candidate.ProspectId);

        var baseThreshold = Math.Clamp(
            38 - workload / 8,
            24,
            38);

        if (source == RecruitingSource.TransferPortal)
            return attainability >= Math.Max(20, baseThreshold - 4) &&
                IsUsefulTransfer(state, userTeam, candidate);

        var recruit = state.HighSchoolRecruitingPool
            .FirstOrDefault(item =>
                item.RecruitId == candidate.ProspectId);
        if (recruit is null)
            return attainability >= baseThreshold;

        var regional =
            RecruitGeography.GetRegion(recruit) ==
            Math.Clamp(userTeam.LegacyRegionId, 0, 4);
        var specificallyAssigned =
            state.ScoutAssignments.Any(assignment =>
                assignment.Scope == ScoutAssignmentScope.Region &&
                int.TryParse(assignment.Target, out var region) &&
                region == RecruitGeography.GetRegion(recruit)) ||
            state.ScoutAssignments.Any(assignment =>
                assignment.Scope == ScoutAssignmentScope.State &&
                int.TryParse(assignment.Target, out var stateIndex) &&
                stateIndex == RecruitGeography.GetStateIndex(recruit));

        var interaction =
            InteractiveRecruitingService.GetInteraction(
                state,
                source,
                candidate.ProspectId);

        var threshold = baseThreshold;

        if (regional && recruit.StarRating >= 3)
            threshold -= recruit.StarRating >= 4 ? 8 : 5;

        if (specificallyAssigned && recruit.StarRating >= 3)
            threshold -= 4;

        if (workload >= 90 &&
            interaction.ScoutingPercent >= 50 &&
            recruit.StarRating is >= 3 and <= 4)
        {
            threshold -= interaction.ScoutingPercent >= 75 ? 6 : 3;
        }

        threshold = Math.Max(14, threshold);

        if (recruit.StarRating >= 5)
        {
            var currentInterest =
                RecruitPreferenceService.GetCurrentUserInterest(
                    state,
                    userTeam,
                    source,
                    candidate.ProspectId,
                    interaction);

            return attainability >= Math.Max(30, threshold) ||
                   currentInterest >= 60;
        }

        if (workload >= 90 &&
            interaction.ScoutingPercent >= 50 &&
            (regional || specificallyAssigned) &&
            recruit.StarRating >= 3)
        {
            return attainability >= Math.Max(14, threshold - 2);
        }

        return attainability >= threshold;
    }

    private static int GetGeographyRecruitingBonus(
        DynastyState state,
        Team userTeam,
        RecruitingSource source,
        Candidate candidate)
    {
        if (source != RecruitingSource.HighSchool)
            return 0;

        var recruit = state.HighSchoolRecruitingPool
            .FirstOrDefault(item =>
                item.RecruitId == candidate.ProspectId);
        if (recruit is null)
            return 0;

        var bonus = 0;
        var region =
            RecruitGeography.GetRegion(recruit);
        var stateIndex =
            RecruitGeography.GetStateIndex(recruit);

        if (region ==
            Math.Clamp(userTeam.LegacyRegionId, 0, 4))
        {
            bonus += 180 + recruit.StarRating * 55;
        }

        if (state.ScoutAssignments.Any(assignment =>
                assignment.Scope == ScoutAssignmentScope.Region &&
                int.TryParse(assignment.Target, out var assignedRegion) &&
                assignedRegion == region))
        {
            bonus += 100;
        }

        if (state.ScoutAssignments.Any(assignment =>
                assignment.Scope == ScoutAssignmentScope.State &&
                int.TryParse(assignment.Target, out var assignedState) &&
                assignedState == stateIndex))
        {
            bonus += 150;
        }

        return bonus;
    }

    private static int GetUserAssistanceScore(
        DynastyState state,
        Team userTeam,
        RecruitingSource source,
        Candidate candidate)
    {
        var interaction =
            InteractiveRecruitingService.GetInteraction(
                state,
                source,
                candidate.ProspectId);
        var attainability =
            RecruitPreferenceService.GetAttainabilityScore(
                state,
                userTeam,
                source,
                candidate.ProspectId);
        var need =
            GetUserAssistancePositionNeed(
                state,
                candidate.Position);
        var scoutKnowledge =
            interaction.ScoutingPercent;
        var naturalInterest =
            RecruitPreferenceService.GetInitialSchoolInterest(
                state,
                userTeam,
                source,
                candidate.ProspectId);
        var highInterestDiscoveryBonus =
            interaction.ScoutingPercent < 50 &&
            naturalInterest >= 65
                ? (naturalInterest - 50) * 35 +
                  Math.Max(0, candidate.TalentLevel - 4) * 100
                : 0;
        var coordinator =
            StaffManagementService.GetStaff(
                state,
                state.UserTeamName,
                GetRecruitingCoordinatorRole(
                    candidate.Position));
        var headCoach =
            StaffManagementService.GetStaff(
                state,
                state.UserTeamName,
                StaffRole.HeadCoach);
        var chiefScout =
            StaffManagementService.GetStaff(
                state,
                state.UserTeamName,
                StaffRole.ChiefScout);

        var recruitingStaff =
            ((headCoach?.Recruiting ?? 60) +
             (coordinator?.Recruiting ?? 60)) / 2;
        var scoutQuality =
            GetScoutQuality(
                state,
                chiefScout);
        var recommended =
            state.ScoutingRecommendationReport?
                .Recommendations.Any(item =>
                    item.ProspectId ==
                    candidate.ProspectId) == true
                ? 450
                : 0;

        var scoutedDiscoveryBonus =
            source == RecruitingSource.HighSchool &&
            interaction.ScoutingPercent >= 50
                ? interaction.ScoutingPercent * 5 +
                  Math.Max(0, candidate.TalentLevel - 4) * 90
                : 0;

        var publicQuality = source ==
            RecruitingSource.HighSchool
                ? candidate.TalentLevel * 45
                : Math.Max(
                    0,
                    candidate.OverallRating - 50) * 6;
        var sourceBalance =
            source == RecruitingSource.HighSchool
                ? 40
                : 0;

        return attainability * 12 +
               need * 260 +
               publicQuality +
               sourceBalance +
               GetGeographyRecruitingBonus(
                   state,
                   userTeam,
                   source,
                   candidate) +
               scoutKnowledge * 3 +
               (recruitingStaff - 60) * 18 +
               (scoutQuality - 60) * 10 +
               recommended +
               scoutedDiscoveryBonus +
               highInterestDiscoveryBonus +
               SimulationSeed.Create(
                   state.DynastyId,
                   state.SeasonYear,
                   state.Week,
                   userTeam.Name,
                   $"assist-{source}-{candidate.ProspectId:N}") %
               61;
    }

    private static int GetUserAssistancePositionNeed(
        DynastyState state,
        Position position)
    {
        var current = state.ActiveRoster.Count(player =>
            player.TeamName.Equals(
                state.UserTeamName,
                StringComparison.OrdinalIgnoreCase) &&
            player.Position == position);

        var offered = state.RecruitingInteractions.Count(item =>
            item.SeasonYear == state.SeasonYear &&
            item.ScholarshipOffered &&
            item.CommittedTeamName is null &&
            GetProspectPosition(
                state,
                item.Source,
                item.ProspectId) == position);

        var target =
            DynastyRosterRules.TargetPositionCounts.TryGetValue(
                position,
                out var count)
                ? count
                : 1;

        return Math.Max(
            0,
            target - current - offered);
    }

    private static Position? GetProspectPosition(
        DynastyState state,
        RecruitingSource source,
        Guid prospectId)
    {
        if (source == RecruitingSource.HighSchool)
        {
            return state.HighSchoolRecruitingPool
                .FirstOrDefault(item =>
                    item.RecruitId == prospectId)
                ?.Position;
        }

        return state.TransferPortalEntries
            .FirstOrDefault(item =>
                item.Player.PlayerId == prospectId)
            ?.Player.Position;
    }

    private static StaffRole GetRecruitingCoordinatorRole(
        Position position) =>
        position switch
        {
            Position.QB or Position.RB or
            Position.WR or Position.TE or
            Position.OL =>
                StaffRole.OffensiveCoordinator,
            Position.K =>
                StaffRole.SpecialTeamsCoordinator,
            _ =>
                StaffRole.DefensiveCoordinator
        };

    private static int GetScoutQuality(
        DynastyState state,
        StaffMember? chiefScout)
    {
        var chiefQuality = chiefScout is null
            ? 60
            : (chiefScout.TalentEvaluation +
               chiefScout.PotentialEvaluation +
               chiefScout.RegionalKnowledge +
               chiefScout.StaffManagement) / 4;

        var subScouts = state.ScoutingStaff;
        if (subScouts.Count == 0)
            return chiefQuality;

        var subordinateQuality =
            (int)Math.Round(
                subScouts.Average(item =>
                    (item.TalentEvaluation +
                     item.PotentialEvaluation +
                     item.RegionalKnowledge +
                     item.WorkRate) / 4.0));

        return (chiefQuality * 2 +
                subordinateQuality) / 3;
    }

    private static RecruitPitchType GetBestPitchType(
        DynastyState state,
        Team userTeam,
        RecruitingSource source,
        Candidate candidate)
    {
        var preferences =
            RecruitPreferenceService.GetPreferences(
                state,
                source,
                candidate.ProspectId);

        return preferences
            .OrderByDescending(preference =>
            {
                var grade =
                    RecruitPreferenceService.GetProgramGrade(
                        state,
                        userTeam,
                        source,
                        candidate.ProspectId,
                        preference.Type);
                var needBonus =
                    preference.Type ==
                        RecruitPitchType.PlayingTime
                        ? GetUserAssistancePositionNeed(
                            state,
                            candidate.Position) * 8
                        : 0;

                return preference.Importance * 2 +
                       grade +
                       needBonus;
            })
            .ThenBy(item => item.Type)
            .First()
            .Type;
    }

    private static int CountTeamRoster(
        IEnumerable<DynastyPlayer> roster,
        string teamName) =>
        roster.Count(player => player.TeamName.Equals(
            teamName,
            StringComparison.OrdinalIgnoreCase));

    private static IReadOnlyList<Candidate> GetCandidates(
        DynastyState state,
        RecruitingSource source)
    {
        if (source == RecruitingSource.TransferPortal)
        {
            return state.TransferPortalEntries
                .Select(entry => new Candidate(
                    entry.Player.PlayerId,
                    entry.Player.FullName,
                    entry.Player.Position,
                    entry.Player.OverallRating,
                    entry.Player.PotentialRating,
                    entry.Player.TalentLevel,
                    entry.Player.ClassYear,
                    entry.OriginTeamName, entry.Player.HeightInches, entry.Player.WeightPounds, entry.Player.Appearance))
                .GroupBy(candidate => candidate.ProspectId)
                .Select(group => group
                    .OrderByDescending(candidate => candidate.OverallRating)
                    .First())
                .ToArray();
        }

        return state.HighSchoolRecruitingPool
            .Where(recruit =>
                recruit.SeasonYear == 0 ||
                recruit.SeasonYear == state.SeasonYear)
            .Select(recruit => new Candidate(
                recruit.RecruitId,
                recruit.FullName,
                recruit.Position,
                recruit.TrueOverallRating,
                recruit.PotentialRating,
                Math.Clamp(recruit.StarRating * 2, 1, 10),
                1,
                string.Empty, recruit.HeightInches, recruit.WeightPounds))
            .GroupBy(candidate => candidate.ProspectId)
            .Select(group => group
                .OrderByDescending(candidate => candidate.OverallRating)
                .First())
            .ToArray();
    }

    private sealed record AssistanceCandidate(
        RecruitingSource Source,
        Candidate Candidate);

    private sealed record Candidate(
        Guid ProspectId,
        string FullName,
        Position Position,
        int OverallRating,
        int PotentialRating,
        int TalentLevel,
        int ClassYear,
        string OriginTeamName, int HeightInches = 0, int WeightPounds = 0, NcaaPlayerAppearance? Appearance = null)
    {
        public DynastyPlayer ToPlayer(string teamName) =>
            new()
            {
                PlayerId = ProspectId,
                FullName = FullName,
                TeamName = teamName,
                Position = Position,
                ClassYear = ClassYear,
                TalentLevel = TalentLevel,
                OverallRating = OverallRating,
                PotentialRating = PotentialRating, HeightInches = HeightInches, WeightPounds = WeightPounds, Appearance = Appearance
            };
    }
}
