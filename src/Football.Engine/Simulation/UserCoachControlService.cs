using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class UserCoachControlService
{
    public static bool ControlsOffense(DynastyState state) => state.UserCoachRole != StaffRole.DefensiveCoordinator;
    public static bool ControlsDefense(DynastyState state) => state.UserCoachRole != StaffRole.OffensiveCoordinator;
    public static bool ControlsPosition(DynastyState state, Position position) => ControlsPosition(state.UserCoachRole, position);
    public static bool ControlsPosition(StaffRole role, Position position) => role == StaffRole.HeadCoach ||
        !position.IsSpecialist() && (GameDayCoachingService.IsOffense(position) ? role != StaffRole.DefensiveCoordinator : role != StaffRole.OffensiveCoordinator);
    public static string Label(StaffRole role) => role switch { StaffRole.OffensiveCoordinator => "Offensive coordinator", StaffRole.DefensiveCoordinator => "Defensive coordinator", _ => "Head coach" };
    public static CoachTalkAudience Audience(DynastyState state) => state.UserCoachRole switch {
        StaffRole.OffensiveCoordinator => CoachTalkAudience.Offense, StaffRole.DefensiveCoordinator => CoachTalkAudience.Defense, _ => CoachTalkAudience.Team };
    public static bool CanRecruit(DynastyState state, RecruitingSource source, Guid id)
    {
        if (state.UserCoachRole == StaffRole.HeadCoach) return true;
        var position = source == RecruitingSource.TransferPortal
            ? state.TransferPortalEntries.FirstOrDefault(p => p.Player.PlayerId == id)?.Player.Position
            : state.HighSchoolRecruitingPool.FirstOrDefault(p => p.RecruitId == id)?.Position;
        return position is { } value && ControlsPosition(state, value);
    }
    public static TeamGamePlan LimitPlan(DynastyState state, TeamGamePlan plan)
    {
        if (plan.TeamName != state.UserTeamName || state.UserCoachRole == StaffRole.HeadCoach) return plan;
        var previous = state.TeamGamePlans.LastOrDefault(p => p.TeamName == plan.TeamName);
        if (previous is null && state.LeagueTeams.FirstOrDefault(t => t.Name == plan.TeamName) is { } team)
            previous = GamePlanService.GetPlan(state, DynastyRosterSimulationProfileBuilder.Build(state, [team])[team.Name]);
        previous ??= new TeamGamePlan { TeamName = plan.TeamName };
        return ControlsOffense(state) ? plan with { Defense = previous.Defense, Approach = previous.Approach,
            DefensiveFormation = previous.DefensiveFormation, CustomDefensivePlayIds = previous.CustomDefensivePlayIds, CreatedDefensivePlays = previous.CreatedDefensivePlays }
            : plan with { Offense = previous.Offense, Tempo = previous.Tempo, OffensiveFormation = previous.OffensiveFormation,
                CustomOffensivePlayIds = previous.CustomOffensivePlayIds, CreatedOffensivePlays = previous.CreatedOffensivePlays,
                OpeningScript = previous.OpeningScript, OpeningPlayIds = previous.OpeningPlayIds };
    }
}
