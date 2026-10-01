using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class InteractiveRecruitingService
{
    public const int ScoutCost = 25;
    public const int PitchCost = 50;
    public const int ScoutStep = 25;

    public static DynastyState EnsurePhaseInitialized(
        DynastyState state,
        Team userTeam)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(userTeam);

        if (state.Phase is not
            (SeasonPhase.TransferPortal or SeasonPhase.Recruiting))
        {
            return state;
        }

        if (state.RecruitingPointsRemaining > 0)
            return state;

        var rosterCount = state.ActiveRoster.Count(player =>
            player.TeamName.Equals(
                state.UserTeamName,
                StringComparison.OrdinalIgnoreCase));

        var shortageBonus = Math.Max(
            0,
            DynastyRosterRules.MaximumRosterSize - rosterCount) * 28;

        var startingPoints =
            userTeam.Prestige * 15 +
            shortageBonus;

        return state with
        {
            RecruitingPointsRemaining =
                Math.Max(900, startingPoints)
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

        var source = state.Phase switch
        {
            SeasonPhase.TransferPortal =>
                RecruitingSource.TransferPortal,
            SeasonPhase.Recruiting =>
                RecruitingSource.HighSchool,
            _ => (RecruitingSource?)null
        };

        if (source is null)
            return state;

        var current = state.RecruitingInteractions
            .Where(interaction =>
                interaction.SeasonYear == state.SeasonYear &&
                interaction.Source == source &&
                interaction.CommittedTeamName is null)
            .ToArray();

        if (current.Length == 0)
            return state;

        var roster = state.ActiveRoster.ToList();
        var commitments = state.RecruitingCommitments.ToList();
        var interactions = state.RecruitingInteractions.ToList();

        var userRosterCount = roster.Count(player =>
            player.TeamName.Equals(
                state.UserTeamName,
                StringComparison.OrdinalIgnoreCase));

        foreach (var interaction in current
                     .OrderByDescending(item => item.UserInterest))
        {
            var resolvedTeam = ResolveCommitment(
                state,
                userTeam,
                teamsByName,
                interaction);

            if (resolvedTeam.Equals(
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

            if (!resolvedTeam.Equals(
                    state.UserTeamName,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var player = CreateCommittedPlayer(
                state,
                interaction);

            if (player is null)
                continue;

            roster.Add(player);
            userRosterCount++;

            commitments.Add(new RecruitingCommitmentRecord
            {
                SeasonYear = state.SeasonYear,
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
        RecruitingInteraction interaction)
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
                TeamName = state.UserTeamName
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
            TeamName = state.UserTeamName,
            Position = recruit.Position,
            ClassYear = 1,
            TalentLevel = Math.Clamp(
                recruit.StarRating * 2,
                1,
                10),
            OverallRating =
                recruit.TrueOverallRating
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
