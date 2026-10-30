using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
namespace ProFootball.Core;

public static class ProDepthChart
{
    public static bool Active(ProPlayer player) => !player.PracticeSquad && !player.Reserve && !player.Retired;
    static DynastyState Bridge(ProFranchise state) => new()
    {
        DynastyName = state.Name, UserTeamName = state.UserTeam, UserCoachRole = ProCareer.Role(state),
        ActiveRoster = state.Players.Where(Active).Select(p => p.Football).ToArray(),
        DepthChartAssignments = state.DepthChartAssignments
    };
    public static int AssignedCode(ProFranchise state, ProPlayer player) => DepthChartService.AssignedCode(Bridge(state), player.Football);
    public static string Label(int code) => DepthChartService.PositionNames[code];
    public static IReadOnlyList<ProPlayer> AtPosition(ProFranchise state, string team, int code)
    {
        var players = state.Players.ToDictionary(p => p.Football.PlayerId);
        return DepthChartService.AtPosition(Bridge(state), team, code).Select(p => players[p.PlayerId]).ToArray();
    }
    public static ProFranchise Assign(ProFranchise state, string team, Guid playerId, int code, int order)
    {
        if (code < 0 || code >= DepthChartService.PositionNames.Count) throw new ArgumentOutOfRangeException(nameof(code));
        var player = state.Players.FirstOrDefault(p => p.Football.PlayerId == playerId);
        if (team != state.UserTeam || player is null || player.Football.TeamName != team || !Active(player))
            throw new InvalidOperationException("Choose an active player on your team.");
        if (!ProCareer.Controls(state, player.Football.Position) || !ProCareer.Controls(state, DepthChartService.PositionForCode(AssignedCode(state, player))) || !ProCareer.Controls(state, DepthChartService.PositionForCode(code)))
            throw new InvalidOperationException("Your coaching role does not manage both positions.");
        var next = DepthChartService.Assign(Bridge(state), team, playerId, code, order);
        return state with { DepthChartAssignments = next.DepthChartAssignments.ToList() };
    }
    // Trade acquisition is an authorized GM/CPU operation, independent of the user's coaching unit.
    internal static ProFranchise MakeCpuStarter(ProFranchise state, string team, Guid playerId)
    {
        var player = state.Players.Single(p => p.Football.PlayerId == playerId);
        if (player.Football.TeamName != team || !Active(player) || !ProReserve.MedicallyAvailable(state, player))
            throw new InvalidOperationException("Only an available active-roster player can start.");
        var label = OffensiveLineService.Label(player.Football);
        var group = state.Players.Where(p => p.Football.TeamName == team && Active(p) && OffensiveLineService.Label(p.Football) == label)
            .OrderBy(p => p.Football.PlayerId == playerId ? 0 : 1).ThenBy(p => p.Football.DepthChartOrder).ThenByDescending(p => p.Football.OverallRating)
            .Select((p, index) => (p.Football.PlayerId, Rank: index + 1)).ToDictionary(x => x.PlayerId, x => x.Rank);
        var next = state with { Players = state.Players.Select(p => group.TryGetValue(p.Football.PlayerId, out var rank) ? p with { Football = p.Football with { DepthChartOrder = rank } } : p).ToList() };
        if (state.DepthChartAssignments.Any(a => a.TeamName == team))
        {
            var projected = DepthChartService.Assign(Bridge(next) with { UserTeamName = "" }, team, playerId, Ncaa06RatingService.PositionCode(player.Football), 1);
            next = next with { DepthChartAssignments = projected.DepthChartAssignments.ToList() };
        }
        return next;
    }
    public static ProFranchise MakeStarter(ProFranchise state, string team, Guid playerId)
    {
        var player = state.Players.Single(p => p.Football.PlayerId == playerId);
        if (!ProReserve.MedicallyAvailable(state, player)) throw new InvalidOperationException("Only an available active-roster player can start.");
        return Assign(state, team, playerId, AssignedCode(state, player), 1);
    }
}
