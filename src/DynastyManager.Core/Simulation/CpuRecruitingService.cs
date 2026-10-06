using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

/// <summary>
/// CPU recruiting safety net for every program, including the user team.
/// Manual user recruiting resolves first; CPU assistance then fills remaining
/// roster needs from the same portal/recruit pool. Transfer assistance is
/// intentionally capped so high-school recruiting still matters.
/// </summary>
public static class CpuRecruitingService
{
    public const int MaximumCpuTransferAdditionsPerTeam = 8;
    public const int WeeklyUserAssistanceOffers = 3;
    public const int MaximumUserAssistedOpenOffers = 10;

    public static DynastyState ApplyWeeklyUserAssistance(
        DynastyState state,
        Team userTeam)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(userTeam);

        if (!state.RecruitingAssistanceEnabled ||
            state.Phase != SeasonPhase.Recruiting ||
            (state.RecruitingAssistanceSeasonYear == state.SeasonYear &&
             state.RecruitingAssistanceWeek == state.Week))
        {
            return state;
        }

        var interactions = state.RecruitingInteractions.ToList();
        var openOffers = interactions.Count(item =>
            item.SeasonYear == state.SeasonYear &&
            item.Source == RecruitingSource.HighSchool &&
            item.CommittedTeamName is null &&
            item.ScholarshipOffered);

        var rosterCount = CountTeamRoster(
            state.ActiveRoster,
            state.UserTeamName);
        var openSlots = Math.Max(
            0,
            DynastyRosterRules.MaximumRosterSize - rosterCount);
        var desiredOpenOffers = Math.Min(
            MaximumUserAssistedOpenOffers,
            Math.Max(3, openSlots));
        var additions = Math.Min(
            WeeklyUserAssistanceOffers,
            Math.Max(0, desiredOpenOffers - openOffers));

        for (var index = 0; index < additions; index++)
        {
            var candidate = state.HighSchoolRecruitingPool
                .Where(recruit =>
                    recruit.SeasonYear == 0 ||
                    recruit.SeasonYear == state.SeasonYear)
                .Where(recruit =>
                {
                    var interaction =
                        InteractiveRecruitingService.GetInteraction(
                            state with
                            {
                                RecruitingInteractions = interactions
                            },
                            RecruitingSource.HighSchool,
                            recruit.RecruitId);

                    if (interaction.CommittedTeamName is not null ||
                        interaction.ScholarshipOffered ||
                        interaction.IsOnTargetBoard ||
                        interaction.WasCpuAssisted)
                    {
                        return false;
                    }

                    return RecruitPreferenceService.GetAttainabilityScore(
                        state with
                        {
                            RecruitingInteractions = interactions
                        },
                        userTeam,
                        RecruitingSource.HighSchool,
                        recruit.RecruitId) >= 35;
                })
                .OrderByDescending(recruit =>
                    GetUserAssistanceScore(
                        state with
                        {
                            RecruitingInteractions = interactions
                        },
                        userTeam,
                        recruit))
                .ThenBy(recruit => recruit.FullName,
                    StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

            if (candidate is null)
                break;

            var existing =
                InteractiveRecruitingService.GetInteraction(
                    state with
                    {
                        RecruitingInteractions = interactions
                    },
                    RecruitingSource.HighSchool,
                    candidate.RecruitId);

            var updated = existing with
            {
                IsOnTargetBoard = true,
                ScholarshipOffered = true,
                WasCpuAssisted = true,
                UserInterest = existing.UserInterest + 15
            };

            var existingIndex = interactions.FindIndex(item =>
                item.SeasonYear == updated.SeasonYear &&
                item.Source == updated.Source &&
                item.ProspectId == updated.ProspectId);

            if (existingIndex >= 0)
                interactions[existingIndex] = updated;
            else
                interactions.Add(updated);
        }

        return state with
        {
            RecruitingInteractions = interactions,
            RecruitingAssistanceSeasonYear = state.SeasonYear,
            RecruitingAssistanceWeek = state.Week
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

            if ((!assistUserTeam && isUserTeam) ||
                (isUserTeam &&
                 source == RecruitingSource.HighSchool))
            {
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

    private static int GetUserAssistanceScore(
        DynastyState state,
        Team userTeam,
        HighSchoolRecruit recruit)
    {
        var interaction =
            InteractiveRecruitingService.GetInteraction(
                state,
                RecruitingSource.HighSchool,
                recruit.RecruitId);
        var attainability =
            RecruitPreferenceService.GetAttainabilityScore(
                state,
                userTeam,
                RecruitingSource.HighSchool,
                recruit.RecruitId);
        var need =
            GetUserAssistancePositionNeed(
                state,
                recruit.Position);
        var scoutKnowledge = interaction.ScoutingPercent;

        return attainability * 10 +
               need * 220 +
               recruit.StarRating * 90 +
               scoutKnowledge * 2 +
               SimulationSeed.Create(
                   state.DynastyId,
                   state.SeasonYear,
                   state.Week,
                   userTeam.Name,
                   $"assist-{recruit.RecruitId:N}") % 61;
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
            item.Source == RecruitingSource.HighSchool &&
            item.ScholarshipOffered &&
            item.CommittedTeamName is null &&
            state.HighSchoolRecruitingPool.Any(recruit =>
                recruit.RecruitId == item.ProspectId &&
                recruit.Position == position));
        var target =
            DynastyRosterRules.TargetPositionCounts.TryGetValue(
                position,
                out var count)
                ? count
                : 1;

        return Math.Max(0, target - current - offered);
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
