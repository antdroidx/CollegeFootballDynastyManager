using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class InteractiveRecruitingService
{
    public const int ScoutCost = 25;
    public const int PitchCost = 50;
    public const int ScoutStep = 25;
    public const int MidseasonPortalBasePoints = 250;

    public static DynastyState EnsurePhaseInitialized(
        DynastyState state,
        Team userTeam)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(userTeam);

        var isMidseasonPortal =
            state.Phase == SeasonPhase.RegularSeason &&
            state.TransferPortalEntries.Count > 0;

        if (state.Phase is not
                (SeasonPhase.TransferPortal or SeasonPhase.Recruiting) &&
            !isMidseasonPortal)
        {
            return state;
        }

        if (isMidseasonPortal)
        {
            if (state.RecruitingPointsPhase ==
                    SeasonPhase.RegularSeason &&
                state.RecruitingPointsWeek == state.Week)
            {
                return state;
            }
        }
        else if (state.RecruitingPointsPhase == state.Phase)
        {
            return state;
        }

        var rosterCount = state.ActiveRoster.Count(player =>
            player.TeamName.Equals(
                state.UserTeamName,
                StringComparison.OrdinalIgnoreCase));

        var shortageBonus = Math.Max(
            0,
            DynastyRosterRules.MaximumRosterSize - rosterCount) * 28;

        var startingPoints = isMidseasonPortal
            ? MidseasonPortalBasePoints +
              userTeam.Prestige * 2
            : userTeam.Prestige * 15 +
              shortageBonus;

        return state with
        {
            RecruitingPointsRemaining = isMidseasonPortal
                ? Math.Max(
                    MidseasonPortalBasePoints,
                    startingPoints)
                : Math.Max(900, startingPoints),
            RecruitingPointsPhase = state.Phase,
            RecruitingPointsWeek = isMidseasonPortal
                ? state.Week
                : -1
        };
    }

    public static DynastyState Scout(
        DynastyState state,
        RecruitingSource source,
        Guid prospectId)
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

        interaction = interaction with
        {
            ScoutingPercent = Math.Min(
                100,
                interaction.ScoutingPercent + ScoutStep)
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
        RecruitingSource source,
        Guid prospectId)
    {
        var interaction = GetOrCreateInteraction(
            state,
            source,
            prospectId);

        if (interaction.CommittedTeamName is not null)
            return state;

        interaction = interaction with
        {
            ScholarshipOffered =
                !interaction.ScholarshipOffered,
            UserInterest =
                !interaction.ScholarshipOffered
                    ? interaction.UserInterest + 15
                    : Math.Max(0, interaction.UserInterest - 15)
        };

        return ReplaceInteraction(state, interaction);
    }

    public static DynastyState Pitch(
        DynastyState state,
        Team userTeam,
        RecruitingSource source,
        Guid prospectId)
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

        var needBonus = GetPositionNeedBonus(
            state,
            position);

        var deterministicBonus =
            SimulationSeed.Create(
                state.DynastyId,
                state.SeasonYear,
                interaction.UserInterest,
                prospectId.ToString("N"),
                "recruiting-pitch") % 11;

        var gain =
            16 +
            userTeam.Prestige / 10 +
            needBonus +
            deterministicBonus;

        interaction = interaction with
        {
            UserInterest =
                interaction.UserInterest + gain
        };

        return ReplaceInteraction(
            state with
            {
                RecruitingPointsRemaining =
                    state.RecruitingPointsRemaining - PitchCost
            },
            interaction);
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
                (!isMidseasonPortal ||
                 interaction.Source !=
                    RecruitingSource.TransferPortal ||
                 (interaction.ScholarshipOffered &&
                  interaction.UserInterest >= 60)))
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

            if (!deferTransfer &&
                resolvedTeam.Equals(
                    state.UserTeamName,
                    StringComparison.OrdinalIgnoreCase) &&
                userRosterCount >=
                    DynastyRosterRules.MaximumRosterSize)
            {
                resolvedTeam = ResolveRivalTeam(
                    state,
                    teamsByName,
                    interaction);
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
                    ProspectId = interaction.ProspectId,
                    Source = RecruitingSource.TransferPortal,
                    PlayerName = player.FullName,
                    TeamName = resolvedTeam,
                    Position = player.Position,
                    OverallRating = player.OverallRating,
                    WasCpuAssisted =
                        !resolvedTeam.Equals(
                            state.UserTeamName,
                            StringComparison.OrdinalIgnoreCase)
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
                    ProspectId = interaction.ProspectId,
                    Source = RecruitingSource.TransferPortal,
                    PlayerName = player.FullName,
                    TeamName = resolvedTeam,
                    Position = player.Position,
                    OverallRating = player.OverallRating,
                    WasCpuAssisted =
                        !resolvedTeam.Equals(
                            state.UserTeamName,
                            StringComparison.OrdinalIgnoreCase)
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
                continue;
            }

            roster.Add(player);
            userRosterCount++;

            commitments.Add(new RecruitingCommitmentRecord
            {
                SeasonYear = state.SeasonYear,
                JoinSeasonYear =
                    state.SeasonYear + 1,
                ProspectId = interaction.ProspectId,
                Source = interaction.Source,
                PlayerName = player.FullName,
                TeamName = state.UserTeamName,
                Position = player.Position,
                OverallRating = player.OverallRating
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
        Guid prospectId) =>
        state.RecruitingInteractions.FirstOrDefault(interaction =>
            interaction.SeasonYear == state.SeasonYear &&
            interaction.Source == source &&
            interaction.ProspectId == prospectId) ??
        CreateInteraction(state, source, prospectId);

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

        var uncertainty = interaction.ScoutingPercent switch
        {
            >= 100 => 0,
            >= 75 => 2,
            >= 50 => 4,
            >= 25 => 6,
            _ => 9
        };

        return (
            Math.Max(40, overall - uncertainty),
            Math.Min(99, overall + uncertainty));
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
        var overall = GetTrueOverall(
            state,
            source,
            prospectId);

        var rivalInterest =
            45 +
            overall / 2 +
            SimulationSeed.Create(
                state.DynastyId,
                state.SeasonYear,
                overall,
                prospectId.ToString("N"),
                "rival-interest") % 31;

        return new RecruitingInteraction
        {
            ProspectId = prospectId,
            Source = source,
            SeasonYear = state.SeasonYear,
            RivalInterest = rivalInterest
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

        var userScore =
            interaction.UserInterest +
            userTeam.Prestige / 4 +
            SimulationSeed.Create(
                state.DynastyId,
                state.SeasonYear,
                interaction.UserInterest,
                interaction.ProspectId.ToString("N"),
                "user-commitment") % 21;

        return userScore >= interaction.RivalInterest
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
                team.Prestige +
                SimulationSeed.Create(
                    state.DynastyId,
                    state.SeasonYear,
                    team.Prestige,
                    interaction.ProspectId.ToString("N"),
                    $"rival-{team.Name}") % 31)
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
