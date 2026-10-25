using DynastyManager.Core.Simulation;
namespace ProFootball.Core;

public static class ProDepthChart
{
    public static ProFranchise MakeStarter(ProFranchise state, string team, Guid playerId)
    {
        var player = state.Players.Single(p => p.Football.PlayerId == playerId);
        if (player.Football.TeamName != team || player.PracticeSquad || player.Reserve || player.Retired || player.Football.CurrentInjury is { WeeksRemaining: > 0 })
            throw new InvalidOperationException("Only an available active-roster player can start.");
        var label = OffensiveLineService.Label(player.Football);
        var group = state.Players.Where(p => p.Football.TeamName == team && !p.PracticeSquad && !p.Reserve && !p.Retired && OffensiveLineService.Label(p.Football) == label)
            .OrderBy(p => p.Football.PlayerId == playerId ? 0 : 1).ThenBy(p => p.Football.DepthChartOrder).ThenByDescending(p => p.Football.OverallRating)
            .Select((p, index) => (p.Football.PlayerId, Rank: index + 1)).ToDictionary(x => x.PlayerId, x => x.Rank);
        return state with { Players = state.Players.Select(p => group.TryGetValue(p.Football.PlayerId, out var rank) ? p with { Football = p.Football with { DepthChartOrder = rank } } : p).ToList() };
    }
}
