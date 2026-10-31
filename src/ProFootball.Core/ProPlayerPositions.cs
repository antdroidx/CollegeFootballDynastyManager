using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;

namespace ProFootball.Core;

public static class ProPlayerPositions
{
    public static ProFranchise Convert(ProFranchise s, Guid playerId, Position target)
    {
        var player = s.Players.SingleOrDefault(p => p.Football.PlayerId == playerId)
            ?? throw new InvalidOperationException("Player not found.");
        if (player.Football.TeamName != s.UserTeam) throw new InvalidOperationException("Only your own players can change position.");
        if (!ProCareer.Controls(s, player.Football.Position) || !ProCareer.Controls(s, target))
            throw new InvalidOperationException("Your coaching role does not manage both positions.");
        if (player.Football.Position == target) return s;
        var converted = PlayerPositionConversion.Convert(player.Football, target);
        var players = s.Players.Select(p => p.Football.PlayerId == playerId ? player with { Football = converted } : p).ToList();
        var assignments = s.DepthChartAssignments.Where(a => a.PlayerId != playerId).ToList();
        return ProEconomy.Log(s with { Players = players, DepthChartAssignments = assignments },
            $"{s.UserTeam} moved {player.Football.FullName} from {player.Football.Position.DisplayName()} to {target.DisplayName()}.");
    }
}