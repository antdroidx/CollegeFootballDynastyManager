using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class TransferPortalMarketService
{
    public static DynastyState AdvanceRegularSeasonWeek(
        DynastyState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.Phase != SeasonPhase.RegularSeason ||
            state.TransferPortalEntries.Count == 0)
        {
            return state;
        }

        var committedIds = state.RecruitingCommitments
            .Where(record =>
                record.Source == RecruitingSource.TransferPortal)
            .Select(record => record.ProspectId)
            .Concat(state.PendingTransferCommitments.Select(
                record => record.ProspectId))
            .ToHashSet();

        var interactions = state.RecruitingInteractions
            .Where(interaction =>
                interaction.SeasonYear == state.SeasonYear &&
                interaction.Source == RecruitingSource.TransferPortal)
            .ToDictionary(
                interaction => interaction.ProspectId,
                interaction => interaction);

        var remaining = new List<TransferPortalEntry>(
            state.TransferPortalEntries.Count);
        var exits = state.TransferPortalExitHistory.ToList();

        foreach (var entry in state.TransferPortalEntries)
        {
            var playerId = entry.Player.PlayerId;

            if (committedIds.Contains(playerId))
                continue;

            var weeks = entry.WeeksInPortal + 1;
            interactions.TryGetValue(
                playerId,
                out var interaction);

            var exitChance = GetExitChancePercent(
                state,
                entry,
                weeks,
                interaction);

            var roll = SimulationSeed.Create(
                state.DynastyId,
                state.SeasonYear,
                state.Week,
                playerId.ToString("N"),
                "portal-attrition") % 100;

            if (roll < exitChance)
            {
                exits.Add(new TransferPortalExitRecord
                {
                    EntrySeasonYear = entry.SeasonYear,
                    ExitSeasonYear = state.SeasonYear,
                    ExitWeek = state.Week,
                    PlayerId = playerId,
                    PlayerName = entry.Player.FullName,
                    OriginTeamName = entry.OriginTeamName,
                    Position = entry.Player.Position,
                    OverallRating = entry.Player.OverallRating,
                    Destination = GetExitDestination(
                        state,
                        entry)
                });
                continue;
            }

            remaining.Add(entry with
            {
                WeeksInPortal = weeks
            });
        }

        return state with
        {
            TransferPortalEntries = remaining,
            TransferPortalExitHistory = exits
        };
    }

    public static DynastyState MaterializePendingCommitments(
        DynastyState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.PendingTransferCommitments.Count == 0)
            return state;

        var upcomingSeasonYear = state.SeasonYear + 1;
        var eligible = state.PendingTransferCommitments
            .Where(commitment =>
                commitment.JoinSeasonYear <= upcomingSeasonYear)
            .OrderBy(commitment => commitment.CommittedSeasonYear)
            .ThenBy(commitment => commitment.CommittedWeek)
            .ToArray();

        if (eligible.Length == 0)
            return state;

        var roster = state.ActiveRoster.ToList();
        var rosterIds = roster
            .Select(player => player.PlayerId)
            .ToHashSet();

        foreach (var commitment in eligible)
        {
            if (!rosterIds.Add(commitment.ProspectId))
                continue;

            var teamRoster = roster.Where(player =>
                    player.TeamName.Equals(commitment.TeamName,
                        StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (teamRoster.Length >= DynastyRosterRules.MaximumRosterSize)
            {
                var released = teamRoster
                    .OrderBy(player => player.OverallRating)
                    .ThenBy(player => player.PotentialRating)
                    .ThenByDescending(player => player.ClassYear)
                    .First();
                roster.Remove(released);
                rosterIds.Remove(released.PlayerId);
            }

            roster.Add(commitment.Player with
            {
                TeamName = commitment.TeamName,
                CurrentInjury = null,
                IsRedshirted = false,
                DepthChartOrder = 0
            });
        }

        var eligibleIds = eligible
            .Select(commitment => commitment.ProspectId)
            .ToHashSet();

        var pending = state.PendingTransferCommitments
            .Where(commitment =>
                !eligibleIds.Contains(commitment.ProspectId))
            .ToArray();

        var updated = PlayerRatingService.EnsureProfiles(
            state with
            {
                ActiveRoster = roster,
                PendingTransferCommitments = pending
            });

        return RosterManagementService
            .NormalizeAllDepthCharts(updated);
    }

    private static int GetExitChancePercent(
        DynastyState state,
        TransferPortalEntry entry,
        int weeksInPortal,
        RecruitingInteraction? interaction)
    {
        var chance = entry.Player.OverallRating switch
        {
            >= 88 => 1,
            >= 84 => 2,
            >= 80 => 4,
            >= 76 => 7,
            >= 72 => 11,
            >= 68 => 16,
            _ => 22
        };

        chance += entry.Player.ClassYear switch
        {
            >= 4 => 4,
            3 => 2,
            _ => 0
        };

        chance += Math.Min(
            12,
            weeksInPortal / 4 * 2);

        if (interaction?.ScholarshipOffered == true)
            chance -= 4;

        if (interaction?.UserInterest >= 100)
            chance -= 3;

        var patientHoldover = SimulationSeed.Create(
            state.DynastyId,
            entry.SeasonYear,
            entry.Player.OverallRating,
            entry.Player.PlayerId.ToString("N"),
            "portal-patient-holdover") % 100 < 15;

        if (patientHoldover)
            chance = Math.Max(1, chance / 3);

        return Math.Clamp(chance, 1, 45);
    }

    private static TransferPortalExitDestination GetExitDestination(
        DynastyState state,
        TransferPortalEntry entry)
    {
        var roll = SimulationSeed.Create(
            state.DynastyId,
            state.SeasonYear,
            entry.WeeksInPortal,
            entry.Player.PlayerId.ToString("N"),
            "portal-exit-destination") % 100;

        return roll switch
        {
            < 60 => TransferPortalExitDestination.Fcs,
            < 88 => TransferPortalExitDestination.DivisionII,
            < 96 => TransferPortalExitDestination.Naia,
            _ => TransferPortalExitDestination.OtherNonFbs
        };
    }
}
