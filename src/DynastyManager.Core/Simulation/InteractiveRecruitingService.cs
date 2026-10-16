using DynastyManager.Core.Models;
using DynastyManager.Core.Seasons;

namespace DynastyManager.Core.Simulation;

public static class InteractiveRecruitingService
{
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
            interaction);
    }

    public static DynastyState ToggleTargetBoard(
        DynastyState state,
        RecruitingSource source,
        Guid prospectId)
    {
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
        DynastyState state,
        Team userTeam,
        IReadOnlyDictionary<string, Team> teamsByName)
    {
        ArgumentNullException.ThrowIfNull(userTeam);
        ArgumentNullException.ThrowIfNull(teamsByName);

        var sources = state.Phase switch
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

        if (sources.Length == 0)
            return state;

        var isMidseasonPortal =
            state.Phase == SeasonPhase.RegularSeason;

        var current = state.RecruitingInteractions
            .Where(interaction =>
                interaction.SeasonYear == state.SeasonYear &&
                sources.Contains(interaction.Source) &&
                interaction.CommittedTeamName is null &&
                (state.Phase != SeasonPhase.Recruiting ||
                 (interaction.ScholarshipOffered &&
                  ShouldResolveRecruitingTarget(
                      state,
                      userTeam,
                      teamsByName,
                      interaction))) &&
                (!isMidseasonPortal ||
                 interaction.Source !=
                    RecruitingSource.TransferPortal ||
                 (interaction.ScholarshipOffered &&
                  RecruitPreferenceService
                      .GetCurrentUserInterest(
                          state,
                          userTeam,
                          interaction.Source,
                          interaction.ProspectId,
                          interaction) >= 60)))
            .ToArray();

        if (current.Length == 0)
            return state;

        var roster = state.ActiveRoster.ToList();
        var commitments = state.RecruitingCommitments.ToList();
        var pendingTransfers =
            state.PendingTransferCommitments.ToList();
        var interactions = state.RecruitingInteractions.ToList();
        var portalEntries = state.TransferPortalEntries.ToList();

        var userRosterCount = roster.Count(player =>
            player.TeamName.Equals(
                state.UserTeamName,
                StringComparison.OrdinalIgnoreCase));

        foreach (var interaction in current
                     .OrderByDescending(item => item.UserInterest))
        {
            var deferTransfer =
                isMidseasonPortal &&
                interaction.Source ==
                    RecruitingSource.TransferPortal;

            var resolvedTeam = ResolveCommitment(
                state,
                userTeam,
                teamsByName,
                interaction);

            if (!deferTransfer && roster.Count(candidate =>
                    candidate.TeamName.Equals(resolvedTeam,
                        StringComparison.OrdinalIgnoreCase)) >=
                DynastyRosterRules.MaximumRosterSize)
            {
                var availableTeam = teamsByName.Values
                    .Where(team => roster.Count(candidate =>
                        candidate.TeamName.Equals(team.Name,
                            StringComparison.OrdinalIgnoreCase)) <
                        DynastyRosterRules.MaximumRosterSize)
                    .OrderByDescending(team => team.Prestige +
                        SimulationSeed.Create(state.DynastyId,
                            state.SeasonYear, team.Prestige,
                            interaction.ProspectId.ToString("N"),
                            $"capacity-{team.Name}") % 21)
                    .FirstOrDefault();

                if (availableTeam is null)
                    continue;

                resolvedTeam = availableTeam.Name;
            }

            var player = CreateCommittedPlayer(
                state,
                interaction,
                resolvedTeam);

            if (player is null)
                continue;

            var updatedInteraction = interaction with
            {
                CommittedTeamName = resolvedTeam
            };

            var index = interactions.FindIndex(item =>
                item.ProspectId == interaction.ProspectId &&
                item.Source == interaction.Source &&
                item.SeasonYear == interaction.SeasonYear);

            if (index >= 0)
                interactions[index] = updatedInteraction;

            if (deferTransfer)
            {
                var originTeamName = portalEntries
                    .First(entry =>
                        entry.Player.PlayerId ==
                        interaction.ProspectId)
                    .OriginTeamName;

                pendingTransfers.Add(
                    new PendingTransferCommitment
                    {
                        ProspectId = interaction.ProspectId,
                        CommittedSeasonYear =
                            state.SeasonYear,
                        CommittedWeek = state.Week,
                        JoinSeasonYear =
                            state.SeasonYear + 1,
                        TeamName = resolvedTeam,
                        OriginTeamName = originTeamName,
                        Player = player,
                        WasUserCommitment =
                            resolvedTeam.Equals(
                                state.UserTeamName,
                                StringComparison.OrdinalIgnoreCase)
                    });

                commitments.Add(new RecruitingCommitmentRecord
                {
                    SeasonYear = state.SeasonYear,
                    JoinSeasonYear =
                        state.SeasonYear + 1,
                    CommittedWeek = state.Week,
                    ProspectId = interaction.ProspectId,
                    Source = RecruitingSource.TransferPortal,
                    PlayerName = player.FullName,
                    TeamName = resolvedTeam,
                    Position = player.Position,
                    OverallRating = player.OverallRating,
                    WasCpuAssisted =
                        resolvedTeam.Equals(
                            state.UserTeamName,
                            StringComparison.OrdinalIgnoreCase)
                            ? interaction.WasCpuAssisted
                            : true
                });

                portalEntries.RemoveAll(entry =>
                    entry.Player.PlayerId ==
                    interaction.ProspectId);
                continue;
            }

            if (interaction.Source ==
                RecruitingSource.TransferPortal)
            {
                if (roster.Count(candidate =>
                        candidate.TeamName.Equals(
                            resolvedTeam,
                            StringComparison.OrdinalIgnoreCase)) <
                    DynastyRosterRules.MaximumRosterSize)
                {
                    roster.Add(player);

                    if (resolvedTeam.Equals(
                            state.UserTeamName,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        userRosterCount++;
                    }
                }

                commitments.Add(new RecruitingCommitmentRecord
                {
                    SeasonYear = state.SeasonYear,
                    JoinSeasonYear =
                        state.SeasonYear + 1,
                    CommittedWeek = state.Week,
                    ProspectId = interaction.ProspectId,
                    Source = RecruitingSource.TransferPortal,
                    PlayerName = player.FullName,
                    TeamName = resolvedTeam,
                    Position = player.Position,
                    OverallRating = player.OverallRating,
                    WasCpuAssisted =
                        resolvedTeam.Equals(
                            state.UserTeamName,
                            StringComparison.OrdinalIgnoreCase)
                            ? interaction.WasCpuAssisted
                            : true
                });

                portalEntries.RemoveAll(entry =>
                    entry.Player.PlayerId ==
                    interaction.ProspectId);
                continue;
            }

            if (!resolvedTeam.Equals(
                    state.UserTeamName,
                    StringComparison.OrdinalIgnoreCase))
            {
                if (roster.Count(candidate =>
                        candidate.TeamName.Equals(
                            resolvedTeam,
                            StringComparison.OrdinalIgnoreCase)) <
                    DynastyRosterRules.MaximumRosterSize)
                {
                    roster.Add(player);
                }

                commitments.Add(new RecruitingCommitmentRecord
                {
                    SeasonYear = state.SeasonYear,
                    JoinSeasonYear =
                        state.SeasonYear + 1,
                    CommittedWeek = state.Week,
                    ProspectId = interaction.ProspectId,
                    Source = interaction.Source,
                    PlayerName = player.FullName,
                    TeamName = resolvedTeam,
                    Position = player.Position,
                    OverallRating = player.OverallRating,
                    WasCpuAssisted = true
                });
                continue;
            }

            roster.Add(player);
            userRosterCount++;

            commitments.Add(new RecruitingCommitmentRecord
            {
                SeasonYear = state.SeasonYear,
                JoinSeasonYear =
                    state.SeasonYear + 1,
                CommittedWeek = state.Week,
                ProspectId = interaction.ProspectId,
                Source = interaction.Source,
                PlayerName = player.FullName,
                TeamName = state.UserTeamName,
                Position = player.Position,
                OverallRating = player.OverallRating,
                WasCpuAssisted = interaction.WasCpuAssisted
            });
        }

        return state with
        {
            ActiveRoster = roster,
            TransferPortalEntries = portalEntries,
            PendingTransferCommitments =
                pendingTransfers,
            RecruitingInteractions = interactions,
            RecruitingCommitments = commitments
        };
    }

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

        return interaction with
        {
            UserInterest = Math.Clamp(
                interaction.UserInterest,
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
        DynastyState state,
        RecruitingSource source,
        Guid prospectId) =>
        state.RecruitingInteractions.FirstOrDefault(interaction =>
            interaction.SeasonYear == state.SeasonYear &&
            interaction.Source == source &&
            interaction.ProspectId == prospectId) ??
        CreateInteraction(state, source, prospectId);

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
        RecruitingInteraction interaction)
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

        return state with
        {
            RecruitingInteractions = interactions
        };
    }

    private static bool ShouldResolveRecruitingTarget(
        DynastyState state,
        Team userTeam,
        IReadOnlyDictionary<string, Team> teamsByName,
        RecruitingInteraction interaction)
    {
        if (state.Phase != SeasonPhase.Recruiting)
            return true;

        if (SeasonProgression.IsFinalRecruitingWeek(state))
            return true;

        var recruitingWeek =
            SeasonProgression.GetRecruitingWeekNumber(state);

        var userInterest =
            RecruitPreferenceService.GetCurrentUserInterest(
                state,
                userTeam,
                interaction.Source,
                interaction.ProspectId,
                interaction);
        var rivalInterest = Math.Max(
            Math.Clamp(
                interaction.RivalInterest,
                0,
                100),
            RecruitPreferenceService.GetBestRivalInterest(
                state,
                userTeam,
                teamsByName,
                interaction.Source,
                interaction.ProspectId));

        var leaderInterest = Math.Max(
            userInterest,
            rivalInterest);

        if (interaction.Source == RecruitingSource.TransferPortal)
        {
            var transferThreshold = Math.Max(
                55,
                76 - (recruitingWeek - 1) * 5);
            if (leaderInterest < transferThreshold)
                return false;

            var transferChance = Math.Clamp(
                45 +
                recruitingWeek * 7 +
                Math.Max(0, leaderInterest - transferThreshold) * 2,
                45,
                95);

            var transferRoll = SimulationSeed.Create(
                state.DynastyId,
                state.SeasonYear,
                state.Week,
                interaction.ProspectId.ToString("N"),
                "weekly-transfer-commit") % 100;

            return transferRoll < transferChance;
        }

        // Readiness is personal and stable across reloads. Interest and a clear
        // favorite can bring a decision forward, without making 100% interest
        // an automatic instant commitment for the entire class.
        var readiness = SimulationSeed.Create(
            state.DynastyId, state.SeasonYear, 0,
            interaction.ProspectId.ToString("N"), "hs-decision-readiness") % 100;
        var timeline = recruitingWeek switch
        {
            <= 0 => 0,
            1 => 10,
            2 => 25,
            3 => 43,
            4 => 63,
            _ => 82
        };
        var confidence = (leaderInterest - 80) / 3 +
            Math.Min(8, Math.Abs(userInterest - rivalInterest) / 8);
        var threshold = Math.Max(50, 78 - (recruitingWeek - 1) * 5);
        return leaderInterest >= threshold && readiness < timeline + confidence;
    }

    private static string ResolveCommitment(
        DynastyState state,
        Team userTeam,
        IReadOnlyDictionary<string, Team> teamsByName,
        RecruitingInteraction interaction)
    {
        if (!interaction.ScholarshipOffered)
        {
            return ResolveRivalTeam(
                state,
                teamsByName,
                interaction);
        }

        var position = GetProspectPosition(
            state,
            interaction.Source,
            interaction.ProspectId);
        var userStaffBonus = GetRecruitingStaffBonus(
            state,
            userTeam.Name,
            position);
        var currentUserInterest =
            RecruitPreferenceService.GetCurrentUserInterest(
                state,
                userTeam,
                interaction.Source,
                interaction.ProspectId,
                interaction);
        var bestRivalInterest =
            RecruitPreferenceService.GetBestRivalInterest(
                state,
                userTeam,
                teamsByName,
                interaction.Source,
                interaction.ProspectId);

        var userScore =
            currentUserInterest +
            userStaffBonus * 2 +
            SimulationSeed.Create(
                state.DynastyId,
                state.SeasonYear,
                currentUserInterest,
                interaction.ProspectId.ToString("N"),
                "user-commitment") % 11;

        var rivalScore =
            bestRivalInterest +
            SimulationSeed.Create(
                state.DynastyId,
                state.SeasonYear,
                bestRivalInterest,
                interaction.ProspectId.ToString("N"),
                "rival-commitment") % 11;

        return userScore >= rivalScore
            ? state.UserTeamName
            : ResolveRivalTeam(
                state,
                teamsByName,
                interaction);
    }

    private static string ResolveRivalTeam(
        DynastyState state,
        IReadOnlyDictionary<string, Team> teamsByName,
        RecruitingInteraction interaction)
    {
        return teamsByName.Values
            .Where(team => !team.Name.Equals(
                state.UserTeamName,
                StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(team =>
                RecruitPreferenceService.GetInitialSchoolInterest(
                    state,
                    team,
                    interaction.Source,
                    interaction.ProspectId) * 3 +
                ProgramPrestigeService.GetCurrentPrestige(state, team) / 3 +
                GetRecruitingStaffBonus(
                    state,
                    team.Name,
                    GetProspectPosition(
                        state,
                        interaction.Source,
                        interaction.ProspectId)) * 2 +
                SimulationSeed.Create(
                    state.DynastyId,
                    state.SeasonYear,
                    ProgramPrestigeService.GetCurrentPrestige(state, team),
                    interaction.ProspectId.ToString("N"),
                    $"rival-{team.Name}") % 11)
            .ThenBy(team => team.Name, StringComparer.OrdinalIgnoreCase)
            .First()
            .Name;
    }

    private static DynastyPlayer? CreateCommittedPlayer(
        DynastyState state,
        RecruitingInteraction interaction,
        string teamName)
    {
        if (interaction.Source ==
            RecruitingSource.TransferPortal)
        {
            var portal = state.TransferPortalEntries
                .FirstOrDefault(entry =>
                    entry.Player.PlayerId ==
                    interaction.ProspectId);

            if (portal is null)
                return null;

            return portal.Player with
            {
                TeamName = teamName
            };
        }

        var recruit = state.HighSchoolRecruitingPool
            .FirstOrDefault(player =>
                player.RecruitId ==
                interaction.ProspectId);

        if (recruit is null)
            return null;

        return new DynastyPlayer
        {
            PlayerId = recruit.RecruitId,
            FullName = recruit.FullName,
            TeamName = teamName,
            Position = recruit.Position,
            ClassYear = 1,
            TalentLevel = Math.Clamp(
                recruit.StarRating * 2,
                1,
                10),
            OverallRating =
                recruit.TrueOverallRating,
            PotentialRating =
                recruit.PotentialRating
        };
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
            Position.QB or Position.RB or Position.WR or Position.TE or Position.OL =>
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
