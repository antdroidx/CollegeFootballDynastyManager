using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

/// <summary>
/// First modernized offseason player-lifecycle layer. Seniors leave,
/// elite juniors can declare early, returning players advance a class,
/// and a deterministic national transfer-portal pool is generated while
/// protecting a basic starter/depth core at every position.
/// </summary>
public static class OffseasonPlayerLifecycleService
{
    public const int MinimumPortalSize = 1500;
    public const int MaximumPortalSize = 2500;
    private const int EarlyProMinimumOverall = 93;
    private const int EarlyProChancePercent = 66;

    private static readonly IReadOnlyDictionary<Position, int> ProtectedDepth =
        new Dictionary<Position, int>
        {
            [Position.QB] = 1,
            [Position.RB] = 2,
            [Position.WR] = 3,
            [Position.TE] = 1,
            [Position.OL] = 5,
            [Position.DE] = 2,
            [Position.DT] = 2,
            [Position.OLB] = 2,
            [Position.MLB] = 1,
            [Position.CB] = 3,
            [Position.FS] = 1,
            [Position.SS] = 1,
            [Position.K] = 1
        };

    public static DynastyState EnterTransferPortal(
        DynastyState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.Phase != SeasonPhase.TransferPortal ||
            state.ActiveRoster.Count == 0)
        {
            return state;
        }

        if (state.TransferPortalEntries.Any(entry =>
                entry.SeasonYear == state.SeasonYear))
        {
            return state;
        }

        var departures = new List<PlayerDepartureRecord>();
        var returning = new List<DynastyPlayer>();

        foreach (var player in state.ActiveRoster)
        {
            if (player.ClassYear >= 4)
            {
                departures.Add(ToDeparture(
                    state.SeasonYear,
                    player,
                    PlayerDepartureReason.Graduation));
                continue;
            }

            if (ShouldDeclareEarly(state, player))
            {
                departures.Add(ToDeparture(
                    state.SeasonYear,
                    player,
                    PlayerDepartureReason.EarlyProDeclaration));
                continue;
            }

            returning.Add(player);
        }

        var protectedPlayers = GetProtectedPlayerIds(returning);
        var portalCandidates = returning
            .Where(player => !protectedPlayers.Contains(player.PlayerId))
            .OrderBy(player => PortalOrderKey(
                state,
                player))
            .ThenByDescending(player => player.ClassYear)
            .ThenByDescending(player => player.OverallRating)
            .ThenBy(player => player.FullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var targetPortalSize = GetTargetPortalSize(
            state,
            portalCandidates.Length);

        var portalPlayers = portalCandidates
            .Take(targetPortalSize)
            .ToArray();

        var portalIds = portalPlayers
            .Select(player => player.PlayerId)
            .ToHashSet();

        var activeRoster = returning
            .Where(player => !portalIds.Contains(player.PlayerId))
            .Select(AdvanceClassYear)
            .ToArray();

        var portal = portalPlayers
            .Select(player => new TransferPortalEntry
            {
                SeasonYear = state.SeasonYear,
                OriginTeamName = player.TeamName,
                Player = AdvanceClassYear(player)
            })
            .ToArray();

        return state with
        {
            ActiveRoster = activeRoster,
            TransferPortalEntries = portal,
            RecentPlayerDepartures = departures
        };
    }

    private static HashSet<Guid> GetProtectedPlayerIds(
        IReadOnlyCollection<DynastyPlayer> players)
    {
        var protectedIds = new HashSet<Guid>();

        foreach (var group in players.GroupBy(player =>
                     (player.TeamName, player.Position)))
        {
            var protectCount = ProtectedDepth.TryGetValue(
                group.Key.Position,
                out var count)
                ? count
                : 1;

            foreach (var player in group
                         .OrderByDescending(player => player.OverallRating)
                         .ThenByDescending(player => player.TalentLevel)
                         .ThenBy(player => player.FullName, StringComparer.OrdinalIgnoreCase)
                         .Take(protectCount))
            {
                protectedIds.Add(player.PlayerId);
            }
        }

        return protectedIds;
    }

    private static bool ShouldDeclareEarly(
        DynastyState state,
        DynastyPlayer player)
    {
        if (player.ClassYear != 3 ||
            player.OverallRating < EarlyProMinimumOverall)
        {
            return false;
        }

        var roll = SimulationSeed.Create(
            state.DynastyId,
            state.SeasonYear,
            0,
            player.PlayerId.ToString("N"),
            "early-pro") % 100;

        return roll < EarlyProChancePercent;
    }

    private static int GetTargetPortalSize(
        DynastyState state,
        int candidateCount)
    {
        if (candidateCount == 0)
            return 0;

        var range =
            MaximumPortalSize - MinimumPortalSize + 1;

        var seed = SimulationSeed.Create(
            state.DynastyId,
            state.SeasonYear,
            0,
            "transfer-portal",
            "size");

        var requested =
            MinimumPortalSize + seed % range;

        return Math.Min(
            requested,
            candidateCount);
    }

    private static int PortalOrderKey(
        DynastyState state,
        DynastyPlayer player) =>
        SimulationSeed.Create(
            state.DynastyId,
            state.SeasonYear,
            player.ClassYear,
            player.PlayerId.ToString("N"),
            "transfer-portal-order");

    private static DynastyPlayer AdvanceClassYear(
        DynastyPlayer player) =>
        player with
        {
            ClassYear = Math.Min(
                4,
                player.ClassYear + 1)
        };

    private static PlayerDepartureRecord ToDeparture(
        int seasonYear,
        DynastyPlayer player,
        PlayerDepartureReason reason) =>
        new()
        {
            SeasonYear = seasonYear,
            PlayerId = player.PlayerId,
            FullName = player.FullName,
            TeamName = player.TeamName,
            Position = player.Position,
            OverallRating = player.OverallRating,
            Reason = reason
        };
}
