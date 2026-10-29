using DynastyManager.Core.Models;
using DynastyManager.Core.Simulation;
namespace ProFootball.Core;
public static class ProLiveControl
{
    public static bool CanCall(GameDaySession session, bool defending) => session.Mode == GameDayMode.Play && !session.IsComplete && !session.KickoffPending
        && (session.ControlledTeamName == session.Game.HomeTeamName || session.ControlledTeamName == session.Game.AwayTeamName)
        && (defending ? session.PossessionTeamName != session.ControlledTeamName && session.UserCoachRole != StaffRole.OffensiveCoordinator
            : session.PossessionTeamName == session.ControlledTeamName && session.UserCoachRole != StaffRole.DefensiveCoordinator);
    public static GameSnapEvent Step(GameDaySession session, string? offense, string? defense) =>
        GameDayEngine.Step(session, CanCall(session, false) ? offense : null, CanCall(session, true) ? defense : null);
}
