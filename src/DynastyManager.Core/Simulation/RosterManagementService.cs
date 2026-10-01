using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class RosterManagementService
{
    public static DynastyState NormalizeAllDepthCharts(
        DynastyState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var normalized = state.ActiveRoster
            .GroupBy(player => new
            {
                player.TeamName,
                player.Position
            })
            .SelectMany(group =>
                group
                    .OrderBy(player => player.IsRedshirted ? 1 : 0)
                    .ThenBy(player =>
                        player.DepthChartOrder > 0
                            ? player.DepthChartOrder
                            : int.MaxValue)
                    .ThenByDescending(player => player.OverallRating)
                    .ThenByDescending(player => player.TalentLevel)
                    .ThenBy(player =>
                        player.FullName,
                        StringComparer.OrdinalIgnoreCase)
                    .Select((player, index) =>
                        player with
                        {
                            DepthChartOrder = index + 1
                        }))
            .ToArray();

        return state with
        {
            ActiveRoster = normalized
        };
    }

    public static DynastyState MovePlayer(
        DynastyState state,
        string teamName,
        Guid playerId,
        int direction)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (direction == 0)
            return state;

        state = NormalizeAllDepthCharts(state);

        var player = state.ActiveRoster.FirstOrDefault(candidate =>
            candidate.PlayerId == playerId &&
            candidate.TeamName.Equals(
                teamName,
                StringComparison.OrdinalIgnoreCase));

        if (player is null || player.IsRedshirted)
            return state;

        var positionPlayers = state.ActiveRoster
            .Where(candidate =>
                candidate.TeamName.Equals(
                    teamName,
                    StringComparison.OrdinalIgnoreCase) &&
                candidate.Position == player.Position &&
                !candidate.IsRedshirted)
            .OrderBy(candidate => candidate.DepthChartOrder)
            .ToArray();

        var index = Array.FindIndex(
            positionPlayers,
            candidate => candidate.PlayerId == playerId);

        if (index < 0)
            return state;

        var targetIndex = Math.Clamp(
            index + Math.Sign(direction),
            0,
            positionPlayers.Length - 1);

        if (targetIndex == index)
            return state;

        var other = positionPlayers[targetIndex];

        var roster = state.ActiveRoster
            .Select(candidate =>
            {
                if (candidate.PlayerId == player.PlayerId)
                {
                    return candidate with
                    {
                        DepthChartOrder = other.DepthChartOrder
                    };
                }

                if (candidate.PlayerId == other.PlayerId)
                {
                    return candidate with
                    {
                        DepthChartOrder = player.DepthChartOrder
                    };
                }

                return candidate;
            })
            .ToArray();

        return NormalizeAllDepthCharts(
            state with
            {
                ActiveRoster = roster
            });
    }

    public static DynastyState ToggleRedshirt(
        DynastyState state,
        string teamName,
        Guid playerId)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.Phase != SeasonPhase.RosterManagement)
            return state;

        var player = state.ActiveRoster.FirstOrDefault(candidate =>
            candidate.PlayerId == playerId &&
            candidate.TeamName.Equals(
                teamName,
                StringComparison.OrdinalIgnoreCase));

        if (player is null)
            return state;

        if (!player.IsRedshirted && player.HasRedshirted)
            return state;

        var roster = state.ActiveRoster
            .Select(candidate =>
                candidate.PlayerId == playerId
                    ? candidate with
                    {
                        IsRedshirted = !candidate.IsRedshirted
                    }
                    : candidate)
            .ToArray();

        return NormalizeAllDepthCharts(
            state with
            {
                ActiveRoster = roster
            });
    }

    public static DynastyState ReleasePlayer(
        DynastyState state,
        string teamName,
        Guid playerId)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.Phase != SeasonPhase.RosterManagement)
            return state;

        var roster = state.ActiveRoster
            .Where(player =>
                player.PlayerId != playerId ||
                !player.TeamName.Equals(
                    teamName,
                    StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (roster.Length == state.ActiveRoster.Count)
            return state;

        return NormalizeAllDepthCharts(
            state with
            {
                ActiveRoster = roster
            });
    }

    public static bool IsStarter(
        DynastyPlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (player.IsRedshirted ||
            player.DepthChartOrder <= 0)
        {
            return false;
        }

        return DynastyRosterRules.StarterPositionCounts.TryGetValue(
                   player.Position,
                   out var starters) &&
               player.DepthChartOrder <= starters;
    }

    public static bool CanRedshirt(
        DynastyPlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);

        return player.IsRedshirted ||
               !player.HasRedshirted;
    }

    public static IReadOnlyList<string> GetPositionWarnings(
        DynastyState state,
        string teamName)
    {
        ArgumentNullException.ThrowIfNull(state);

        var playable = state.ActiveRoster
            .Where(player =>
                player.TeamName.Equals(
                    teamName,
                    StringComparison.OrdinalIgnoreCase) &&
                !player.IsRedshirted &&
                player.CurrentInjury is null)
            .GroupBy(player => player.Position)
            .ToDictionary(
                group => group.Key,
                group => group.Count());

        var warnings = new List<string>();

        foreach (var pair in DynastyRosterRules.MinimumPositionCounts)
        {
            var count = playable.TryGetValue(
                pair.Key,
                out var value)
                ? value
                : 0;

            if (count >= pair.Value)
                continue;

            warnings.Add(
                $"{pair.Key}: {count}/{pair.Value} playable " +
                $"({pair.Value - count} short)");
        }

        return warnings;
    }
}
