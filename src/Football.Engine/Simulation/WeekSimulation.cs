using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class WeekSimulation
{
    public static DynastyState SimulateCurrentRegularSeasonWeek(
        DynastyState state,
        IReadOnlyDictionary<string, Team> teamsByName,
        IReadOnlyDictionary<string, TeamSimulationProfile>? profilesByTeam = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(teamsByName);

        if (state.Phase != SeasonPhase.RegularSeason)
            return state;

        var changed = false;
        var schedule = state.Schedule
            .Select(game =>
            {
                if (game.SeasonYear != state.SeasonYear || game.Week != state.Week || game.HasPlayed)
                    return game;

                if (!teamsByName.TryGetValue(game.HomeTeamName, out var homeTeam) && (homeTeam = FcsLeagueService.Find(game.HomeTeamName)) is null)
                    throw new InvalidOperationException(
                        $"Scheduled home team '{game.HomeTeamName}' is not available.");

                if (!teamsByName.TryGetValue(game.AwayTeamName, out var awayTeam) && (awayTeam = FcsLeagueService.Find(game.AwayTeamName)) is null)
                    throw new InvalidOperationException(
                        $"Scheduled away team '{game.AwayTeamName}' is not available.");

                TeamSimulationProfile? homeProfile = null;
                TeamSimulationProfile? awayProfile = null;

                if (profilesByTeam is not null)
                {
                    profilesByTeam.TryGetValue(game.HomeTeamName, out homeProfile);
                    profilesByTeam.TryGetValue(game.AwayTeamName, out awayProfile);
                }

                changed = true;
                homeProfile = GamePlanService.Attach(state, homeTeam, homeProfile);
                awayProfile = GamePlanService.Attach(state, awayTeam, awayProfile);
                var roster = state.ActiveRoster;
                if (game.IsFcsGame)
                {
                    var external = teamsByName.ContainsKey(homeTeam.Name) ? awayTeam : homeTeam;
                    roster = roster.Concat(FcsLeagueService.CreateRoster(state, external)).ToArray();
                    var externalProfiles = DynastyRosterSimulationProfileBuilder.Build(state with { ActiveRoster = roster }, new[] { homeTeam, awayTeam });
                    if (!teamsByName.ContainsKey(homeTeam.Name)) homeProfile = externalProfiles[homeTeam.Name];
                    if (!teamsByName.ContainsKey(awayTeam.Name)) awayProfile = externalProfiles[awayTeam.Name];
                }
                var session = GameDayEngine.Start(game, homeProfile, awayProfile, roster, GameDayMode.Simulate, state.DepthChartAssignments);
                GameDayEngine.SimulateToEnd(session);
                return GameDayEngine.Finish(session);
            })
            .ToArray();

        return changed ? state with { Schedule = schedule } : state;
    }
}
