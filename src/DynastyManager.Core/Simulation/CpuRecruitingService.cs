using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

/// <summary>
/// CPU recruiting support for the league. CPU programs fill roster needs
/// directly. User recruiting assistance can operate the same visible recruiting
/// system as the player: target board, scholarships, scouting and pitches. The
/// workload setting controls how much of the available recruiting budget staff
/// may use, while manual user-managed prospects are never overwritten.
/// </summary>
public static class CpuRecruitingService
{
    public const int MaximumCpuTransferAdditionsPerTeam = 8;

    public static DynastyState ApplyWeeklyUserAssistance(
        DynastyState state,
        Team userTeam)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(userTeam);

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

        var currentOffers = state.RecruitingInteractions.Count(item =>
            item.SeasonYear == state.SeasonYear &&
            sources.Contains(item.Source) &&
            item.CommittedTeamName is null &&
            item.ScholarshipOffered);

        var offerMultiplier =
            0.75 + workload / 100.0 * 0.75;
        var desiredOpenOffers = openSlots == 0
            ? 0
            : Math.Max(
                Math.Min(3, openSlots),
                (int)Math.Ceiling(
                    openSlots * offerMultiplier));

        var additionsNeeded = Math.Max(
            0,
            desiredOpenOffers - currentOffers);

        var availableCandidates = sources
            .SelectMany(source =>
                GetCandidates(state, source)
                    .Select(candidate =>
                        new AssistanceCandidate(
                            source,
                            candidate)))
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

                var threshold =
                    Math.Clamp(38 - workload / 8, 24, 38);
                return RecruitPreferenceService
                    .GetAttainabilityScore(
                        state,
                        userTeam,
                        item.Source,
                        item.Candidate.ProspectId) >=
                    threshold;
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

        foreach (var candidate in availableCandidates
                     .Take(additionsNeeded))
        {
            state = InteractiveRecruitingService
                .ToggleScholarship(
                    state,
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
            var scoutingShare = Math.Clamp(
                0.20 +
                (scoutQuality - 50) / 200.0,
                0.18,
                0.42);
            var scoutingBudget =
                (int)Math.Floor(
                    pointBudget * scoutingShare);
            var targetScoutPercent = workload switch
            {
                >= 90 => 100,
                >= 60 => 75,
                _ => 50
            };
            if (scoutQuality >= 85)
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

    public static DynastyState ApplyPhaseAssistance(
        DynastyState state,
        IReadOnlyDictionary<string, Team> teamsByName,
        bool assistUserTeam = true)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(teamsByName);

        var source = state.Phase switch
        {
            SeasonPhase.TransferPortal => RecruitingSource.TransferPortal,
            SeasonPhase.Recruiting => RecruitingSource.HighSchool,
            _ => (RecruitingSource?)null
        };

        if (source is null)
            return state;

        var candidates = GetCandidates(state, source.Value);
        if (candidates.Count == 0)
            return state;

        var roster = state.ActiveRoster.ToList();
        var commitments = state.RecruitingCommitments.ToList();

        var usedProspectIds = commitments
            .Where(record =>
                record.SeasonYear == state.SeasonYear &&
                record.Source == source)
            .Select(record => record.ProspectId)
            .Concat(roster.Select(player => player.PlayerId))
            .ToHashSet();

        MaterializeResolvedInteractions(
            state,
            teamsByName,
            source.Value,
            candidates,
            roster,
            commitments,
            usedProspectIds);

        var available = candidates
            .Where(candidate =>
                !usedProspectIds.Contains(candidate.ProspectId))
            .ToDictionary(
                candidate => candidate.ProspectId);

        var teams = teamsByName.Values
            .OrderByDescending(team => ProgramPrestigeService.GetCurrentPrestige(state, team))
            .ThenBy(team => team.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var team in teams)
        {
            var isUserTeam = team.Name.Equals(
                state.UserTeamName,
                StringComparison.OrdinalIgnoreCase);

            if (isUserTeam)
            {
                // The user program always recruits through the visible
                // interaction system. Assistance may automate those actions,
                // but never silently signs players here.
                continue;
            }

            var rosterCount = CountTeamRoster(roster, team.Name);
            var capacity = Math.Max(
                0,
                DynastyRosterRules.MaximumRosterSize - rosterCount);

            if (capacity == 0)
                continue;

            var phaseLimit = source == RecruitingSource.TransferPortal
                ? Math.Min(
                    capacity,
                    MaximumCpuTransferAdditionsPerTeam)
                : capacity;

            for (var addition = 0;
                 addition < phaseLimit && available.Count > 0;
                 addition++)
            {
                var position = SelectMostNeededPosition(
                    roster,
                    team.Name);

                var candidate = SelectBestCandidate(
                    state,
                    team,
                    source.Value,
                    position,
                    available.Values);

                if (candidate is null)
                    break;

                roster.Add(
                    candidate.ToPlayer(team.Name));

                commitments.Add(new RecruitingCommitmentRecord
                {
                    SeasonYear = state.SeasonYear,
                    JoinSeasonYear = state.SeasonYear + 1,
                    ProspectId = candidate.ProspectId,
                    Source = source.Value,
                    PlayerName = candidate.FullName,
                    TeamName = team.Name,
                    Position = candidate.Position,
                    OverallRating = candidate.OverallRating,
                    WasCpuAssisted = true
                });

                available.Remove(candidate.ProspectId);
                usedProspectIds.Add(candidate.ProspectId);
            }
        }

        return state with
        {
            ActiveRoster = roster,
            TransferPortalEntries =
                source == RecruitingSource.TransferPortal
                    ? state.TransferPortalEntries
                        .Where(entry =>
                            !usedProspectIds.Contains(
                                entry.Player.PlayerId))
                        .ToArray()
                    : state.TransferPortalEntries,
            RecruitingCommitments = commitments
        };
    }

    private static void MaterializeResolvedInteractions(
        DynastyState state,
        IReadOnlyDictionary<string, Team> teamsByName,
        RecruitingSource source,
        IReadOnlyList<Candidate> candidates,
        List<DynastyPlayer> roster,
        List<RecruitingCommitmentRecord> commitments,
        HashSet<Guid> usedProspectIds)
    {
        var byId = candidates
            .GroupBy(candidate => candidate.ProspectId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(candidate => candidate.OverallRating)
                    .First());

        foreach (var interaction in state.RecruitingInteractions
                     .Where(interaction =>
                         interaction.SeasonYear == state.SeasonYear &&
                         interaction.Source == source &&
                         interaction.CommittedTeamName is not null))
        {
            if (usedProspectIds.Contains(interaction.ProspectId) ||
                !byId.TryGetValue(
                    interaction.ProspectId,
                    out var candidate) ||
                interaction.CommittedTeamName is not string teamName ||
                !teamsByName.ContainsKey(teamName) ||
                CountTeamRoster(roster, teamName) >=
                    DynastyRosterRules.MaximumRosterSize)
            {
                continue;
            }

            roster.Add(candidate.ToPlayer(teamName));

            commitments.Add(new RecruitingCommitmentRecord
            {
                SeasonYear = state.SeasonYear,
                JoinSeasonYear = state.SeasonYear + 1,
                ProspectId = candidate.ProspectId,
                Source = source,
                PlayerName = candidate.FullName,
                TeamName = teamName,
                Position = candidate.Position,
                OverallRating = candidate.OverallRating,
                WasCpuAssisted = !teamName.Equals(
                    state.UserTeamName,
                    StringComparison.OrdinalIgnoreCase)
            });

            usedProspectIds.Add(candidate.ProspectId);
        }
    }

    private static Candidate? SelectBestCandidate(
        DynastyState state,
        Team team,
        RecruitingSource source,
        Position preferredPosition,
        IEnumerable<Candidate> candidates)
    {
        var pool = candidates
            .Where(candidate =>
                source != RecruitingSource.TransferPortal ||
                !candidate.OriginTeamName.Equals(
                    team.Name,
                    StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (team.Name.Equals(
                state.UserTeamName,
                StringComparison.OrdinalIgnoreCase) &&
            source == RecruitingSource.HighSchool)
        {
            pool = pool
                .Where(candidate =>
                    RecruitPreferenceService.GetAttainabilityScore(
                        state,
                        team,
                        RecruitingSource.HighSchool,
                        candidate.ProspectId) >= 30)
                .ToArray();
        }

        if (pool.Length == 0)
            return null;

        var positionPool = pool
            .Where(candidate =>
                candidate.Position == preferredPosition)
            .ToArray();

        if (positionPool.Length > 0)
            pool = positionPool;

        return pool
            .OrderByDescending(candidate =>
                GetCandidateFitScore(
                    state,
                    team,
                    candidate))
            .ThenByDescending(candidate => candidate.OverallRating)
            .ThenBy(candidate => candidate.FullName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

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
         interaction.ScoutingPercent > 0 ||
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
            .OrderByDescending(item =>
                GetUserAssistanceScore(
                    state,
                    userTeam,
                    item.Source,
                    item.Candidate))
            .ToArray();

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
                ? 250
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
               scoutKnowledge * 3 +
               (recruitingStaff - 60) * 18 +
               (scoutQuality - 60) * 10 +
               recommended +
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

    private static int GetCandidateFitScore(
        DynastyState state,
        Team team,
        Candidate candidate)
    {
        var prestige = ProgramPrestigeService.GetCurrentPrestige(state, team);
        var programTarget =
            58 + prestige / 3;

        var qualityFit =
            candidate.OverallRating <= programTarget + 8
                ? candidate.OverallRating * 100
                : candidate.OverallRating * 100 -
                  (candidate.OverallRating - programTarget - 8) * 175;

        var coordinatorRole = candidate.Position switch
        {
            Position.QB or Position.RB or Position.WR or Position.TE or Position.OL =>
                StaffRole.OffensiveCoordinator,
            Position.K => StaffRole.SpecialTeamsCoordinator,
            _ => StaffRole.DefensiveCoordinator
        };
        var headRecruiting = StaffManagementService.GetStaff(
            state, team.Name, StaffRole.HeadCoach)?.Recruiting ?? 60;
        var coordinatorRecruiting = StaffManagementService.GetStaff(
            state, team.Name, coordinatorRole)?.Recruiting ?? 60;
        var staffFit = (headRecruiting + coordinatorRecruiting - 120) * 7;

        var deterministicFit =
            SimulationSeed.Create(
                state.DynastyId,
                state.SeasonYear,
                prestige,
                team.Name,
                candidate.ProspectId.ToString("N")) % 100;

        return qualityFit + staffFit + deterministicFit;
    }

    private static Position SelectMostNeededPosition(
        IReadOnlyCollection<DynastyPlayer> roster,
        string teamName)
    {
        var counts = roster
            .Where(player => player.TeamName.Equals(
                teamName,
                StringComparison.OrdinalIgnoreCase))
            .GroupBy(player => player.Position)
            .ToDictionary(
                group => group.Key,
                group => group.Count());

        return DynastyRosterRules.TargetPositionCounts
            .Select(pair => new
            {
                Position = pair.Key,
                Deficit = pair.Value -
                    (counts.TryGetValue(
                        pair.Key,
                        out var count)
                        ? count
                        : 0)
            })
            .OrderByDescending(item => item.Deficit)
            .ThenBy(item => item.Position)
            .First()
            .Position;
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
                    entry.OriginTeamName))
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
                string.Empty))
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
        string OriginTeamName)
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
                PotentialRating = PotentialRating
            };
    }
}
