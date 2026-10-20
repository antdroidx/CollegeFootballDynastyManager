using System.Runtime.CompilerServices;
using DynastyManager.Core.Models;
using DynastyManager.Core.Seasons;

namespace DynastyManager.Core.Simulation;

/// <summary>One offer market and one decision engine for every school.</summary>
public static class CompetingRecruitingService
{
    private static readonly ConditionalWeakTable<IReadOnlyList<RecruitingOffer>,
        Dictionary<(int, RecruitingSource, Guid), RecruitingOffer[]>> OfferIndexes = new();

    public static IReadOnlyList<RecruitingOffer> GetOffers(DynastyState state,
        RecruitingSource source, Guid prospectId, bool activeOnly = false)
    {
        var rows = OfferIndexes.GetValue(state.RecruitingOffers, offers => offers
            .GroupBy(item => (item.SeasonYear, item.Source, item.ProspectId))
            .ToDictionary(group => group.Key, group => group.ToArray()))
            .GetValueOrDefault((state.SeasonYear, source, prospectId), []);
        return rows.Where(item => !activeOnly || item.Status == RecruitingOfferStatus.Active)
            .OrderByDescending(item => item.Status == RecruitingOfferStatus.Active)
            .ThenByDescending(item => item.Interest)
            .ThenBy(item => item.TeamName, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public static DynastyState RestoreUserBoard(DynastyState state)
    {
        var existing = state.RecruitingInteractions.Where(item => item.SeasonYear == state.SeasonYear)
            .Select(item => (item.Source, item.ProspectId)).ToHashSet();
        var missing = state.RecruitingOffers.Where(item => item.SeasonYear == state.SeasonYear &&
            item.Status == RecruitingOfferStatus.Active && item.TeamName.Equals(state.UserTeamName,
                StringComparison.OrdinalIgnoreCase) && !existing.Contains((item.Source, item.ProspectId)))
            .GroupBy(item => (item.Source, item.ProspectId)).Select(group => group.First()).Select(item =>
                new RecruitingInteraction { SeasonYear = state.SeasonYear, Source = item.Source,
                    ProspectId = item.ProspectId, IsOnTargetBoard = true, ScholarshipOffered = true,
                    UserInterest = item.Interest, WasCpuAssisted = item.WasCpuAssisted }).ToArray();
        return missing.Length == 0 ? state : state with
            { RecruitingInteractions = state.RecruitingInteractions.Concat(missing).ToArray() };
    }

    public static DynastyState SyncUserOffer(DynastyState state, RecruitingInteraction interaction, int effort = 0)
    {
        var existing = GetOffers(state, interaction.Source, interaction.ProspectId).FirstOrDefault(item =>
            item.TeamName.Equals(state.UserTeamName, StringComparison.OrdinalIgnoreCase));
        if (existing is null && !interaction.ScholarshipOffered) return state;
        if (interaction.CommittedTeamName is not null) return state;
        var updated = (existing ?? new RecruitingOffer
        {
            SeasonYear = state.SeasonYear, Source = interaction.Source,
            ProspectId = interaction.ProspectId, TeamName = state.UserTeamName, OfferedWeek = state.Week
        }) with
        {
            Interest = Math.Clamp(interaction.UserInterest, 0, 100),
            PointsInvested = (existing?.PointsInvested ?? 0) + effort,
            LastContactWeek = effort > 0 ? state.Week : existing?.LastContactWeek ?? -1,
            LastContactPhase = effort > 0 ? state.Phase : existing?.LastContactPhase,
            WasCpuAssisted = interaction.WasCpuAssisted,
            Status = interaction.ScholarshipOffered ? RecruitingOfferStatus.Active : RecruitingOfferStatus.Withdrawn
        };
        if (updated == existing) return state;
        return state with { RecruitingOffers = state.RecruitingOffers.Where(item =>
            !(item.SeasonYear == state.SeasonYear && item.Source == updated.Source &&
              item.ProspectId == updated.ProspectId && item.TeamName.Equals(state.UserTeamName,
                  StringComparison.OrdinalIgnoreCase))).Append(updated).ToArray() };
    }

    public static DynastyState AdvanceOffers(DynastyState state, IReadOnlyDictionary<string, Team> teams,
        IProgress<(int Completed, int Total, string TeamName)>? progress = null)
    {
        state = RestoreUserBoard(state);
        // Import real user offers from older saves/interaction callers. Never invent
        // historical CPU offers or change completed commitments during an upgrade.
        teams.TryGetValue(state.UserTeamName, out var user);
        var prospects = Prospects(state);
        var used = Occupied(state);
        foreach (var interaction in state.RecruitingInteractions.Where(item => item.SeasonYear == state.SeasonYear &&
                     item.CommittedTeamName is null && !used.Contains(item.ProspectId) &&
                     prospects.ContainsKey((item.Source, item.ProspectId))))
        {
            var normalized = interaction;
            if (normalized.UserInterest == 0 && user is not null)
                normalized = normalized with { UserInterest = RecruitPreferenceService.GetCurrentUserInterest(
                    state, user, interaction.Source, interaction.ProspectId, interaction) };
            state = SyncUserOffer(state, normalized);
        }
        var sources = Sources(state);
        if (sources.Length == 0 || (state.RecruitingOffersProcessedSeasonYear == state.SeasonYear &&
            state.RecruitingOffersProcessedWeek == state.Week && state.RecruitingOffersProcessedPhase == state.Phase))
            return state;
        var offers = state.RecruitingOffers.Where(item => item.SeasonYear >= state.SeasonYear - 1)
            .GroupBy(Key).ToDictionary(group => group.Key, group => group.Last());
        var teamArray = teams.Values.OrderBy(team => team.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        var processed = 0;
        foreach (var team in teamArray)
        {
            progress?.Report((++processed, teamArray.Length, team.Name));
            if (team.Name.Equals(state.UserTeamName, StringComparison.OrdinalIgnoreCase)) continue;
            var roster = PlanningRoster(state, team.Name).ToArray();
            var capacity = Math.Max(0, DynastyRosterRules.MaximumRosterSize - roster.Length);
            var counts = roster.GroupBy(item => item.Position).ToDictionary(group => group.Key, group => group.Count());
            var plan = TransferRosterPlanningService.CreatePlan(state with { PendingTransferCommitments = [] }, team, roster);
            var prestige = ProgramPrestigeService.GetCurrentPrestige(state, team);
            var head = StaffManagementService.GetStaff(state, team.Name, StaffRole.HeadCoach);
            var recruiting = head?.Recruiting ?? 60;
            var points = state.Phase == SeasonPhase.RegularSeason ? 250 + prestige * 2
                : state.Phase == SeasonPhase.TransferPortal ? Math.Max(900, prestige * 15 + capacity * 28)
                : Math.Max(350, prestige * 4 + capacity * 28 / 6);
            var remainingEffort = (int)(points * 0.8);
            foreach (var source in sources)
            {
                var available = prospects.Values.Where(item => item.Source == source && !used.Contains(item.Id) &&
                    !item.Origin.Equals(team.Name, StringComparison.OrdinalIgnoreCase)).ToArray();
                bool Useful(Prospect prospect) => capacity > 0 && (source == RecruitingSource.TransferPortal
                    ? plan.GetValue(prospect.Player) > 0
                    : counts.GetValueOrDefault(prospect.Player.Position) <
                        DynastyRosterRules.TargetPositionCounts.GetValueOrDefault(prospect.Player.Position, 3));
                foreach (var old in offers.Values.Where(item => item.SeasonYear == state.SeasonYear &&
                    item.Source == source && item.TeamName.Equals(team.Name, StringComparison.OrdinalIgnoreCase) &&
                    item.Status == RecruitingOfferStatus.Active).ToArray())
                {
                    if (!prospects.TryGetValue((source, old.ProspectId), out var prospect) || used.Contains(old.ProspectId) || !Useful(prospect))
                        offers[Key(old)] = old with { Status = RecruitingOfferStatus.Withdrawn };
                }
                if (capacity == 0) continue;
                var active = offers.Values.Where(item => item.SeasonYear == state.SeasonYear && item.Source == source &&
                    item.Status == RecruitingOfferStatus.Active && item.TeamName.Equals(team.Name, StringComparison.OrdinalIgnoreCase)).ToList();
                var targetCounts = active.GroupBy(item => prospects[(source, item.ProspectId)].Player.Position)
                    .ToDictionary(group => group.Key, group => group.Count());
                // This is a pursuit board sized to roster needs, not a signing quota.
                var desired = Math.Max(4, (int)Math.Ceiling(capacity * 1.8));
                var activeIds = active.Select(item => item.ProspectId).ToHashSet();
                // Scouts shortlist by position, public quality and geography before
                // performing the more expensive individual school-fit assessment.
                var shortlist = available.Where(Useful).GroupBy(item => item.Player.Position).SelectMany(group =>
                {
                    var deficit = Math.Max(1, DynastyRosterRules.TargetPositionCounts.GetValueOrDefault(group.Key, 3) -
                        counts.GetValueOrDefault(group.Key));
                    return group.OrderByDescending(prospect =>
                    {
                        var quality = prospect.Player.OverallRating;
                        var target = 58 + prestige / 3;
                        var local = prospect.HomeRegion == team.LegacyRegionId ? 10 : 0;
                        return quality * 2 - Math.Max(0, quality - target - 8) * 4 + local +
                            SimulationSeed.Create(state.DynastyId, state.SeasonYear, 0, team.Name,
                                $"shortlist-{prospect.Id:N}") % 12;
                    }).ThenBy(prospect => prospect.Id).Take(Math.Max(6, deficit * 4));
                }).Concat(available.Where(item => activeIds.Contains(item.Id))).DistinctBy(item => item.Id);
                var ranked = shortlist.Select(prospect =>
                {
                    var natural = RecruitPreferenceService.GetInitialSchoolInterest(state, team, source, prospect.Id);
                    var quality = prospect.Player.OverallRating;
                    var target = 58 + prestige / 3;
                    var reachPenalty = Math.Max(0, quality - target - 8) * 4;
                    var need = Math.Max(0, DynastyRosterRules.TargetPositionCounts.GetValueOrDefault(prospect.Player.Position, 3) -
                        counts.GetValueOrDefault(prospect.Player.Position));
                    return (Prospect: prospect, Natural: natural, Score: quality * 2 + natural + need * 3 - reachPenalty);
                }).OrderByDescending(item => item.Score).ThenBy(item => item.Prospect.Id).ToArray();
                foreach (var item in ranked)
                {
                    if (active.Count >= desired) break;
                    var key = (state.SeasonYear, source, item.Prospect.Id, team.Name.ToUpperInvariant());
                    if (offers.TryGetValue(key, out var old) && old.Status == RecruitingOfferStatus.Active) continue;
                    var position = item.Prospect.Player.Position;
                    var deficit = Math.Max(1, DynastyRosterRules.TargetPositionCounts.GetValueOrDefault(position, 3) - counts.GetValueOrDefault(position));
                    if (targetCounts.GetValueOrDefault(position) >= (int)Math.Ceiling(deficit * 1.8)) continue;
                    var offer = new RecruitingOffer
                    {
                        SeasonYear = state.SeasonYear, Source = source, ProspectId = item.Prospect.Id,
                        TeamName = team.Name, Interest = Math.Clamp(Math.Max(item.Natural + 15, (old?.Interest ?? 0) - 3), 0, 100),
                        OfferedWeek = old?.OfferedWeek ?? state.Week, WasCpuAssisted = true,
                        PointsInvested = old?.PointsInvested ?? 0,
                        LastContactWeek = old?.LastContactWeek ?? -1,
                        LastContactPhase = old?.LastContactPhase
                    };
                    offers[key] = offer;
                    active.Add(offer);
                    targetCounts[position] = targetCounts.GetValueOrDefault(position) + 1;
                }
                var share = source == RecruitingSource.HighSchool &&
                    prospects.Values.Any(item => item.Source == RecruitingSource.TransferPortal && !used.Contains(item.Id))
                    ? 0.8 : 1.0;
                var contacts = Math.Max(0, (int)(remainingEffort * share) / InteractiveRecruitingService.PitchCost);
                var priorities = ranked.ToDictionary(item => item.Prospect.Id, item => item.Score);
                var pursued = active.Where(item => priorities.ContainsKey(item.ProspectId))
                    .OrderBy(item => item.LastContactWeek)
                    .ThenByDescending(item => priorities[item.ProspectId])
                    .ThenBy(item => item.ProspectId).Take(contacts).ToArray();
                remainingEffort -= pursued.Length * InteractiveRecruitingService.PitchCost;
                foreach (var offer in pursued)
                {
                    var gain = 8 + recruiting / 12 + Math.Max(0, offer.Interest - 50) / 12;
                    offers[Key(offer)] = offer with
                    {
                        Interest = Math.Clamp(offer.Interest + gain, 0, 100),
                        PointsInvested = offer.PointsInvested + InteractiveRecruitingService.PitchCost,
                        LastContactWeek = state.Week, LastContactPhase = state.Phase
                    };
                }
            }
        }
        return state with
        {
            RecruitingOffers = offers.Values.OrderBy(item => item.SeasonYear).ThenBy(item => item.Source)
                .ThenBy(item => item.ProspectId).ThenBy(item => item.TeamName, StringComparer.OrdinalIgnoreCase).ToArray(),
            RecruitingOffersProcessedSeasonYear = state.SeasonYear,
            RecruitingOffersProcessedWeek = state.Week, RecruitingOffersProcessedPhase = state.Phase
        };
    }

    public static DynastyState ResolveWeek(DynastyState state, IReadOnlyDictionary<string, Team> teams,
        IProgress<(int Completed, int Total, string TeamName)>? progress = null)
    {
        state = AdvanceOffers(state, teams, progress);
        if (Sources(state).Length == 0 || (state.RecruitingResolutionProcessedSeasonYear == state.SeasonYear &&
            state.RecruitingResolutionProcessedWeek == state.Week && state.RecruitingResolutionProcessedPhase == state.Phase)) return state;
        var prospects = Prospects(state);
        var used = Occupied(state);
        var roster = state.ActiveRoster.ToList();
        var pending = state.PendingTransferCommitments.ToList();
        var commitments = state.RecruitingCommitments.ToList();
        var interactions = state.RecruitingInteractions.ToArray();
        var offers = state.RecruitingOffers.ToArray();
        var plans = new Dictionary<string, TransferRosterPlanningService.RosterPlan>(StringComparer.OrdinalIgnoreCase);
        var planned = teams.Keys.ToDictionary(name => name, name => PlanningRoster(state, name).ToList(), StringComparer.OrdinalIgnoreCase);
        var groups = offers.Select((offer, index) => (Offer: offer, Index: index))
            .Where(item => item.Offer.SeasonYear == state.SeasonYear && item.Offer.Status == RecruitingOfferStatus.Active &&
                Sources(state).Contains(item.Offer.Source))
            .GroupBy(item => (item.Offer.Source, item.Offer.ProspectId))
            .OrderBy(group => SimulationSeed.Create(state.DynastyId, state.SeasonYear, 0,
                group.Key.ProspectId.ToString("N"), "decision-order"))
            .ThenBy(group => group.Key.Source).ThenBy(group => group.Key.ProspectId).ToArray();
        foreach (var group in groups)
        {
            if (used.Contains(group.Key.ProspectId) || !prospects.TryGetValue(group.Key, out var prospect)) continue;
            var eligible = group.Where(item => teams.ContainsKey(item.Offer.TeamName) &&
                !prospect.Origin.Equals(item.Offer.TeamName, StringComparison.OrdinalIgnoreCase) &&
                planned[item.Offer.TeamName].Count < DynastyRosterRules.MaximumRosterSize).Where(item =>
                {
                    if (prospect.Source != RecruitingSource.TransferPortal ||
                        item.Offer.TeamName.Equals(state.UserTeamName, StringComparison.OrdinalIgnoreCase)) return true;
                    if (!plans.TryGetValue(item.Offer.TeamName, out var plan))
                    {
                        plan = TransferRosterPlanningService.CreatePlan(state with { PendingTransferCommitments = [] },
                            teams[item.Offer.TeamName], planned[item.Offer.TeamName]);
                        plans[item.Offer.TeamName] = plan;
                    }
                    return plan.GetValue(prospect.Player) > 0;
                }).OrderByDescending(item => ChoiceScore(state, item.Offer))
                .ThenBy(item => item.Offer.TeamName, StringComparer.OrdinalIgnoreCase).ToArray();
            var eligibleIndexes = eligible.Select(item => item.Index).ToHashSet();
            foreach (var item in group.Where(item => !eligibleIndexes.Contains(item.Index)))
                offers[item.Index] = item.Offer with { Status = RecruitingOfferStatus.Withdrawn };
            if (eligible.Length == 0 || !Ready(state, prospect, eligible.Select(item => item.Offer).ToArray())) continue;
            var winning = eligible[0].Offer;
            var player = PlayerRatingService.EnsureInitialized(state, prospect.Player with { TeamName = winning.TeamName });
            planned[winning.TeamName].Add(player);
            plans.Remove(winning.TeamName);
            if (state.Phase == SeasonPhase.RegularSeason)
                pending.Add(new PendingTransferCommitment
                {
                    ProspectId = prospect.Id, CommittedSeasonYear = state.SeasonYear, CommittedWeek = state.Week,
                    JoinSeasonYear = state.SeasonYear + 1, TeamName = winning.TeamName, OriginTeamName = prospect.Origin,
                    Player = player, WasUserCommitment = winning.TeamName.Equals(state.UserTeamName, StringComparison.OrdinalIgnoreCase)
                });
            else roster.Add(player);
            used.Add(prospect.Id);
            commitments.Add(new RecruitingCommitmentRecord
            {
                SeasonYear = state.SeasonYear, JoinSeasonYear = state.SeasonYear + 1,
                CommittedWeek = state.Week, ProspectId = prospect.Id, Source = prospect.Source,
                PlayerName = player.FullName, TeamName = winning.TeamName, Position = player.Position,
                OverallRating = player.OverallRating, WasCpuAssisted = winning.WasCpuAssisted ||
                    !winning.TeamName.Equals(state.UserTeamName, StringComparison.OrdinalIgnoreCase)
            });
            foreach (var item in eligible)
                offers[item.Index] = item.Offer with { Status = item.Index == eligible[0].Index
                    ? RecruitingOfferStatus.Signed : RecruitingOfferStatus.Lost };
            for (var index = 0; index < interactions.Length; index++)
                if (interactions[index].SeasonYear == state.SeasonYear && interactions[index].Source == prospect.Source &&
                    interactions[index].ProspectId == prospect.Id)
                    interactions[index] = interactions[index] with { CommittedTeamName = winning.TeamName };
        }
        return state with
        {
            ActiveRoster = roster, PendingTransferCommitments = pending, RecruitingCommitments = commitments,
            RecruitingInteractions = interactions, RecruitingOffers = offers,
            TransferPortalEntries = state.TransferPortalEntries.Where(item => !used.Contains(item.Player.PlayerId)).ToArray(),
            RecruitingResolutionProcessedSeasonYear = state.SeasonYear,
            RecruitingResolutionProcessedWeek = state.Week, RecruitingResolutionProcessedPhase = state.Phase,
            CpuRecruitingProcessedSeasonYear = state.SeasonYear, CpuRecruitingProcessedWeek = state.Week
        };
    }

    private static bool Ready(DynastyState state, Prospect prospect, RecruitingOffer[] offers)
    {
        var interests = offers.Select(item => item.Interest).OrderByDescending(value => value).ToArray();
        var leader = interests[0];
        var lead = leader - interests.ElementAtOrDefault(1);
        if (prospect.Source == RecruitingSource.HighSchool)
        {
            var week = SeasonProgression.GetRecruitingWeekNumber(state);
            if (week >= SeasonProgression.RecruitingWeekCount) return leader >= 45;
            var timeline = week switch { <= 0 => 0, 1 => 10, 2 => 25, 3 => 43, 4 => 63, _ => 82 };
            var confidence = (leader - 80) / 3 + Math.Min(8, lead / 8);
            var readiness = SimulationSeed.Create(state.DynastyId, state.SeasonYear, 0,
                prospect.Id.ToString("N"), "hs-decision-readiness") % 100;
            return leader >= Math.Max(50, 78 - (week - 1) * 5) && readiness < timeline + confidence;
        }
        if (leader < 60) return false;
        if (leader >= 90 && lead >= 10) return true;
        var chance = Math.Clamp(45 + (leader - 60) + (state.Phase == SeasonPhase.Recruiting ?
            SeasonProgression.GetRecruitingWeekNumber(state) * 3 : 0), 25, 95);
        return SimulationSeed.Create(state.DynastyId, state.SeasonYear, state.Week,
            prospect.Id.ToString("N"), "portal-offer-decision") % 100 < chance;
    }

    private static int ChoiceScore(DynastyState state, RecruitingOffer offer) => offer.Interest +
        SimulationSeed.Create(state.DynastyId, state.SeasonYear, 0, offer.ProspectId.ToString("N"),
            $"school-choice-{offer.TeamName}") % 11;

    private static (int, RecruitingSource, Guid, string) Key(RecruitingOffer offer) =>
        (offer.SeasonYear, offer.Source, offer.ProspectId, offer.TeamName.ToUpperInvariant());

    private static RecruitingSource[] Sources(DynastyState state) => state.Phase switch
    {
        SeasonPhase.Recruiting => [RecruitingSource.HighSchool, RecruitingSource.TransferPortal],
        SeasonPhase.TransferPortal or SeasonPhase.RegularSeason => [RecruitingSource.TransferPortal],
        _ => []
    };

    private static HashSet<Guid> Occupied(DynastyState state) => state.ActiveRoster.Select(item => item.PlayerId)
        .Concat(state.PendingTransferCommitments.Select(item => item.ProspectId))
        .Concat(state.RecruitingCommitments.Where(item => item.SeasonYear == state.SeasonYear).Select(item => item.ProspectId))
        .ToHashSet();

    private static IEnumerable<DynastyPlayer> PlanningRoster(DynastyState state, string team) =>
        state.ActiveRoster.Where(item => item.TeamName.Equals(team, StringComparison.OrdinalIgnoreCase) &&
            (state.Phase != SeasonPhase.RegularSeason || item.ClassYear < 4 || item.IsRedshirted))
        .Concat(state.PendingTransferCommitments.Where(item => item.TeamName.Equals(team, StringComparison.OrdinalIgnoreCase))
            .Select(item => item.Player)).DistinctBy(item => item.PlayerId);

    private sealed record Prospect(RecruitingSource Source, Guid Id, DynastyPlayer Player, string Origin, int? HomeRegion = null);

    private static Dictionary<(RecruitingSource, Guid), Prospect> Prospects(DynastyState state) =>
        state.HighSchoolRecruitingPool.Where(item => (item.SeasonYear == 0 || item.SeasonYear == state.SeasonYear))
            .Select(item => new Prospect(RecruitingSource.HighSchool, item.RecruitId, new DynastyPlayer
            {
                PlayerId = item.RecruitId, FullName = item.FullName, Position = item.Position, TeamName = "",
                ClassYear = 1, OverallRating = item.TrueOverallRating, PotentialRating = item.PotentialRating,
                OriginalRecruitingStars = item.StarRating, HeightInches = item.HeightInches, WeightPounds = item.WeightPounds,
                TalentLevel = Math.Clamp(item.StarRating * 2, 1, 10)
            }, "", RecruitGeography.GetRegion(item)))
            .Concat(state.TransferPortalEntries.Where(item => item.Player.ClassYear <= 4)
                .Select(item => new Prospect(RecruitingSource.TransferPortal, item.Player.PlayerId, item.Player, item.OriginTeamName)))
            .GroupBy(item => (item.Source, item.Id)).ToDictionary(group => group.Key, group => group.First());
}
