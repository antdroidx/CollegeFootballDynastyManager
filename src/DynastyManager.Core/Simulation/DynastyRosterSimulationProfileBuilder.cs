using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class DynastyRosterSimulationProfileBuilder
{
    public static IReadOnlyDictionary<string, TeamSimulationProfile> Build(
        DynastyState state,
        IEnumerable<Team> teams)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(teams);

        var teamArray = teams.ToArray();

        var rosterByTeam = state.ActiveRoster
            .Where(player =>
                !player.IsRedshirted &&
                player.CurrentInjury is null)
            .GroupBy(
                player => player.TeamName,
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.ToArray(),
                StringComparer.OrdinalIgnoreCase);

        var profiles = new Dictionary<string, TeamSimulationProfile>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var team in teamArray)
        {
            rosterByTeam.TryGetValue(
                team.Name,
                out var roster);

            roster ??= Array.Empty<DynastyPlayer>();

            var prestige = ProgramPrestigeService.GetCurrentPrestige(state, team);
            var fallback = Math.Clamp(
                60.0 + (prestige - 50) * .5,
                55.0,
                95.0);

            var qb = AverageDepth(
                roster,
                fallback,
                Position.QB);
            var rb = AverageDepth(
                roster,
                fallback,
                Position.RB);
            var wr = AverageDepth(
                roster,
                fallback,
                Position.WR);
            var te = AverageDepth(
                roster,
                fallback,
                Position.TE);
            var ol = AverageDepth(
                roster,
                fallback,
                Position.OL);
            var de = AverageDepth(
                roster,
                fallback,
                Position.DE);
            var dt = AverageDepth(
                roster,
                fallback,
                Position.DT);
            var olb = AverageDepth(
                roster,
                fallback,
                Position.OLB);
            var mlb = AverageDepth(
                roster,
                fallback,
                Position.MLB);
            var cb = AverageDepth(
                roster,
                fallback,
                Position.CB);
            var fs = AverageDepth(
                roster,
                fallback,
                Position.FS);
            var ss = AverageDepth(
                roster,
                fallback,
                Position.SS);
            var kicker = AverageDepth(
                roster,
                fallback,
                Position.K);

            var safety =
                (fs + ss) / 2.0;

            var headCoach = StaffManagementService.GetStaff(state, team.Name, StaffRole.HeadCoach);
            var offensiveCoordinator = StaffManagementService.GetStaff(state, team.Name, StaffRole.OffensiveCoordinator);
            var defensiveCoordinator = StaffManagementService.GetStaff(state, team.Name, StaffRole.DefensiveCoordinator);
            var specialTeamsCoordinator = StaffManagementService.GetStaff(state, team.Name, StaffRole.SpecialTeamsCoordinator);
            var headCoachModifier = Math.Clamp(((headCoach?.Leadership ?? 60) - 60) * .025 +
                ((headCoach?.GameManagement ?? 60) - 60) * .015, -1.5, 1.5);
            var offenseModifier = Math.Clamp(((offensiveCoordinator?.Scheme ?? 60) - 60) * .075, -3.0, 3.0) + headCoachModifier;
            var defenseModifier = Math.Clamp(((defensiveCoordinator?.Scheme ?? 60) - 60) * .075, -3.0, 3.0) + headCoachModifier;
            var specialTeamsModifier = Math.Clamp(((specialTeamsCoordinator?.SpecialTeams ?? 60) - 60) * .085, -3.0, 3.0) + headCoachModifier * .5;

            profiles[team.Name] = new TeamSimulationProfile
            {
                TeamName = team.Name,
                HeadCoachLeadership = headCoach?.Leadership ?? 60,
                HeadCoachGameManagement = headCoach?.GameManagement ?? 60,
                OffensiveCoordinatorScheme = offensiveCoordinator?.Scheme ?? 60,
                OffensiveCoordinatorGameManagement = offensiveCoordinator?.GameManagement ?? 60,
                DefensiveCoordinatorScheme = defensiveCoordinator?.Scheme ?? 60,
                DefensiveCoordinatorGameManagement = defensiveCoordinator?.GameManagement ?? 60,
                PassOffenseRating =
                    qb * .30 +
                    wr * .25 +
                    te * .10 +
                    ol * .30 +
                    rb * .05 +
                    offenseModifier,
                RushOffenseRating =
                    AverageDepth(roster, fallback, Position.RB, false) * .35 +
                    AverageDepth(roster, fallback, Position.OL, false) * .40 +
                    AverageDepth(roster, fallback, Position.QB, false) * .10 +
                    AverageDepth(roster, fallback, Position.TE, false) * .15 +
                    offenseModifier,
                PassDefenseRating =
                    cb * .30 +
                    safety * .20 +
                    de * .20 +
                    olb * .15 +
                    dt * .05 +
                    mlb * .10 +
                    defenseModifier,
                RushDefenseRating =
                    AverageDepth(roster, fallback, Position.DT, false) * .20 +
                    AverageDepth(roster, fallback, Position.DE, false) * .15 +
                    AverageDepth(roster, fallback, Position.MLB, false) * .25 +
                    AverageDepth(roster, fallback, Position.OLB, false) * .20 +
                    (AverageDepth(roster, fallback, Position.FS, false) + AverageDepth(roster, fallback, Position.SS, false)) / 2 * .15 +
                    AverageDepth(roster, fallback, Position.CB, false) * .05 +
                    defenseModifier,
                SpecialTeamsRating =
                    kicker * .85 +
                    fallback * .15 +
                    specialTeamsModifier,
                Morale = GameMoraleService.SeasonMorale(state, team.Name),
                RosterSize = roster.Length
            };
            profiles[team.Name] = profiles[team.Name] with { GamePlan = GamePlanService.GetPlan(state, profiles[team.Name]) };
        }

        return profiles;
    }

    private static double AverageDepth(
        IReadOnlyCollection<DynastyPlayer> roster,
        double fallback,
        Position position, bool passing = true)
    {
        var count =
            DynastyRosterRules.StarterPositionCounts.TryGetValue(
                position,
                out var starterCount)
                ? starterCount
                : 1;

        var values = roster
            .Where(player =>
                player.Position == position)
            .OrderBy(player =>
                player.DepthChartOrder > 0
                    ? player.DepthChartOrder
                    : int.MaxValue)
            .ThenByDescending(player =>
                player.OverallRating)
            .Take(count)
            .Select(player =>
                PlayerSkillService.GameRating(player, passing))
            .ToArray();

        return values.Length == 0
            ? fallback
            : values.Average();
    }
}
