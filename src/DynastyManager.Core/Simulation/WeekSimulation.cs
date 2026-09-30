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

        if (state.Phase != SeasonPhase.RegularSeason || state.Week < 1)
            return state;

        var changed = false;
        var schedule = state.Schedule
            .Select(game =>
            {
                if (game.Week != state.Week || game.HasPlayed)
                    return game;

                if (!teamsByName.TryGetValue(game.HomeTeamName, out var homeTeam))
                    throw new InvalidOperationException(
                        $"Scheduled home team '{game.HomeTeamName}' is not available.");

                if (!teamsByName.TryGetValue(game.AwayTeamName, out var awayTeam))
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
                return DeterministicGameSimulator.Simulate(
                    game,
                    homeTeam,
                    awayTeam,
                    homeProfile,
                    awayProfile);
            })
            .ToArray();

        return changed ? state with { Schedule = schedule } : state;
    }
}
