using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class BowlService
{
    public const int MinimumWins = 6;
    public const int FirstBowlWeek = 15;
    public const int LastBowlWeek = 17;

    private static readonly string[] BowlNames =
    {
        "Carnation Bowl", "Mandarin Bowl", "Honey Bowl", "Fiesta Bowl",
        "Nectarine Bowl", "Polyester Bowl", "Lemon-Lime Bowl", "Alligator Bowl",
        "Desert Bowl", "Fort Bowl", "Vacation Bowl", "Star Bowl",
        "Bell Bowl", "Freedom Bowl", "Casino Bowl", "American Bowl",
        "Island Bowl", "Philanthropy Bowl", "Steak Bowl", "Camping Bowl",
        "Spud Bowl", "Music Bowl", "New Orleans Bowl", "Cowboy Bowl",
        "Santa Fe Bowl", "Burrito Bowl", "Mexico Bowl", "Chick Bowl",
        "Rainbow Bowl", "Empire Bowl", "Mushroom Bowl", "Coffee Bowl"
    };

    public static DynastyState InitializeBowls(
        DynastyState state,
        IReadOnlyDictionary<string, Team> teamsByName,
        IReadOnlyDictionary<string, TeamSimulationProfile>? profilesByTeam = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(teamsByName);

        if (state.Phase != SeasonPhase.Postseason ||
            state.Week != CollegeFootballPlayoffService.FirstRoundWeek)
        {
            return state;
        }

        if (state.Schedule.Any(game =>
                game.SeasonYear == state.SeasonYear &&
                game.GameType == ScheduledGameType.Bowl))
        {
            return state;
        }

        var playoffTeams = state.CollegeFootballPlayoffHistory
            .Where(record => record.SeasonYear == state.SeasonYear)
            .Select(record => record.TeamName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var eligible = NationalRankingService
            .Build(state, teamsByName, profilesByTeam)
            .Where(ranking =>
                ranking.Wins >= MinimumWins &&
                !playoffTeams.Contains(ranking.TeamName))
            .Take(BowlNames.Length * 2)
            .ToList();

        if (eligible.Count % 2 != 0)
            eligible.RemoveAt(eligible.Count - 1);

        var games = new List<ScheduledGame>(eligible.Count / 2);

        for (var index = 0; index < eligible.Count / 2; index++)
        {
            var first = eligible[index * 2];
            var second = eligible[index * 2 + 1];
            var week = FirstBowlWeek +
                index % (LastBowlWeek - FirstBowlWeek + 1);
            var bowlName = BowlNames[index];

            var seed = SimulationSeed.Create(
                state.DynastyId,
                state.SeasonYear,
                week,
                bowlName,
                $"{first.TeamName}|{second.TeamName}");

            games.Add(new ScheduledGame
            {
                GameId = $"{state.SeasonYear}-BOWL-{index + 1:D2}-{seed:X8}",
                SeasonYear = state.SeasonYear,
                Week = week,
                HomeTeamName = first.TeamName,
                AwayTeamName = second.TeamName,
                GameType = ScheduledGameType.Bowl,
                BowlName = bowlName,
                IsNeutralSite = true,
                SimulationSeed = seed
            });
        }

        return games.Count == 0
            ? state
            : state with
            {
                Schedule = state.Schedule.Concat(games).ToArray()
            };
    }

    public static DynastyState SimulateCurrentWeek(
        DynastyState state,
        IReadOnlyDictionary<string, Team> teamsByName,
        IReadOnlyDictionary<string, TeamSimulationProfile>? profilesByTeam = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(teamsByName);

        if (state.Phase != SeasonPhase.Postseason)
            return state;

        var newlyPlayed = new List<ScheduledGame>();
        var schedule = state.Schedule.Select(game =>
        {
            if (game.SeasonYear != state.SeasonYear ||
                game.GameType != ScheduledGameType.Bowl ||
                game.Week != state.Week ||
                game.HasPlayed)
            {
                return game;
            }

            if (!teamsByName.TryGetValue(game.HomeTeamName, out var homeTeam) ||
                !teamsByName.TryGetValue(game.AwayTeamName, out var awayTeam))
            {
                throw new InvalidOperationException(
                    "Bowl game references a team that is not available.");
            }

            TeamSimulationProfile? homeProfile = null;
            TeamSimulationProfile? awayProfile = null;

            if (profilesByTeam is not null)
            {
                profilesByTeam.TryGetValue(game.HomeTeamName, out homeProfile);
                profilesByTeam.TryGetValue(game.AwayTeamName, out awayProfile);
            }

            var played = DeterministicGameSimulator.Simulate(
                game,
                homeTeam,
                awayTeam,
                homeProfile,
                awayProfile);

            newlyPlayed.Add(played);
            return played;
        }).ToArray();

        if (newlyPlayed.Count == 0)
            return state;

        var history = state.BowlHistory.ToList();

        foreach (var game in newlyPlayed)
        {
            if (game.HomeScore is not int homeScore ||
                game.AwayScore is not int awayScore ||
                string.IsNullOrWhiteSpace(game.BowlName))
            {
                continue;
            }

            var homeWon = homeScore > awayScore;

            history.Add(new BowlResultRecord
            {
                SeasonYear = state.SeasonYear,
                BowlName = game.BowlName,
                WinnerTeamName = homeWon ? game.HomeTeamName : game.AwayTeamName,
                LoserTeamName = homeWon ? game.AwayTeamName : game.HomeTeamName,
                WinnerScore = homeWon ? homeScore : awayScore,
                LoserScore = homeWon ? awayScore : homeScore
            });
        }

        return state with
        {
            Schedule = schedule,
            BowlHistory = history
        };
    }
}
