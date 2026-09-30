using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class ConferenceChampionshipService
{
    public const int MinimumConferenceTeams = 8;

    public static DynastyState ScheduleChampionships(
        DynastyState state,
        IReadOnlyDictionary<string, Team> teamsByName)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(teamsByName);

        if (state.Phase != SeasonPhase.ConferenceChampionship)
            return state;

        if (state.Schedule.Any(game =>
                game.SeasonYear == state.SeasonYear &&
                game.GameType == ScheduledGameType.ConferenceChampionship))
        {
            return state;
        }

        var championshipGames = new List<ScheduledGame>();

        foreach (var conference in teamsByName.Values
                     .GroupBy(
                         team => team.ConferenceName,
                         StringComparer.OrdinalIgnoreCase)
                     .Where(group =>
                         group.Count() >= MinimumConferenceTeams &&
                         !group.Key.Equals(
                             "Independent",
                             StringComparison.OrdinalIgnoreCase))
                     .OrderBy(
                         group => group.Key,
                         StringComparer.OrdinalIgnoreCase))
        {
            var standings = ConferenceStandings.Build(
                state,
                teamsByName,
                conference.Key);

            if (standings.Count < 2)
                continue;

            var home = standings[0].TeamName;
            var away = standings[1].TeamName;
            var seed = SimulationSeed.Create(
                state.DynastyId,
                state.SeasonYear,
                state.Week,
                conference.Key,
                $"{home}|{away}|CCG");

            championshipGames.Add(new ScheduledGame
            {
                GameId =
                    $"{state.SeasonYear}-CCG-{seed:X8}",
                SeasonYear = state.SeasonYear,
                Week = state.Week,
                HomeTeamName = home,
                AwayTeamName = away,
                GameType =
                    ScheduledGameType.ConferenceChampionship,
                SimulationSeed = seed
            });
        }

        return championshipGames.Count == 0
            ? state
            : state with
            {
                Schedule = state.Schedule
                    .Concat(championshipGames)
                    .ToArray()
            };
    }

    public static DynastyState SimulateChampionships(
        DynastyState state,
        IReadOnlyDictionary<string, Team> teamsByName,
        IReadOnlyDictionary<string, TeamSimulationProfile>? profilesByTeam = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(teamsByName);

        if (state.Phase != SeasonPhase.ConferenceChampionship)
            return state;

        var changed = false;
        var schedule = state.Schedule
            .Select(game =>
            {
                if (game.SeasonYear != state.SeasonYear ||
                    game.GameType !=
                        ScheduledGameType.ConferenceChampionship ||
                    game.Week != state.Week ||
                    game.HasPlayed)
                {
                    return game;
                }

                if (!teamsByName.TryGetValue(
                        game.HomeTeamName,
                        out var homeTeam))
                {
                    throw new InvalidOperationException(
                        $"Championship home team '{game.HomeTeamName}' is not available.");
                }

                if (!teamsByName.TryGetValue(
                        game.AwayTeamName,
                        out var awayTeam))
                {
                    throw new InvalidOperationException(
                        $"Championship away team '{game.AwayTeamName}' is not available.");
                }

                TeamSimulationProfile? homeProfile = null;
                TeamSimulationProfile? awayProfile = null;

                if (profilesByTeam is not null)
                {
                    profilesByTeam.TryGetValue(
                        game.HomeTeamName,
                        out homeProfile);
                    profilesByTeam.TryGetValue(
                        game.AwayTeamName,
                        out awayProfile);
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

        if (!changed)
            return state;

        var history = state.ConferenceChampionshipHistory.ToList();

        foreach (var game in schedule.Where(game =>
                     game.SeasonYear == state.SeasonYear &&
                     game.GameType ==
                         ScheduledGameType.ConferenceChampionship &&
                     game.HasPlayed))
        {
            if (!teamsByName.TryGetValue(
                    game.HomeTeamName,
                    out var homeTeam) ||
                game.HomeScore is not int homeScore ||
                game.AwayScore is not int awayScore)
            {
                continue;
            }

            var conferenceName = homeTeam.ConferenceName;

            if (history.Any(record =>
                    record.SeasonYear == state.SeasonYear &&
                    record.ConferenceName.Equals(
                        conferenceName,
                        StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var homeWon = homeScore > awayScore;

            history.Add(new ConferenceChampionRecord
            {
                SeasonYear = state.SeasonYear,
                ConferenceName = conferenceName,
                ChampionTeamName =
                    homeWon
                        ? game.HomeTeamName
                        : game.AwayTeamName,
                RunnerUpTeamName =
                    homeWon
                        ? game.AwayTeamName
                        : game.HomeTeamName,
                ChampionScore =
                    homeWon ? homeScore : awayScore,
                RunnerUpScore =
                    homeWon ? awayScore : homeScore
            });
        }

        return state with
        {
            Schedule = schedule,
            ConferenceChampionshipHistory = history
        };
    }
}
