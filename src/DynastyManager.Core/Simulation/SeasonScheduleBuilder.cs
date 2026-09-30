using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class SeasonScheduleBuilder
{
    public const int DefaultRegularSeasonWeeks = 13;
    public const int DefaultGamesPerTeam = 12;
    public const int DefaultWeekZeroGames = 5;
    private const int MinimumConferenceSize = 8;
    private const int MaximumPairingAttempts = 200;
    private const int MaximumWeekAssignmentAttempts = 200;

    public static IReadOnlyList<ScheduledGame> BuildRegularSeason(
        IEnumerable<Team> teams,
        Guid dynastyId,
        int seasonYear,
        int weeks = DefaultRegularSeasonWeeks,
        int gamesPerTeam = DefaultGamesPerTeam,
        int weekZeroGames = DefaultWeekZeroGames)
    {
        ArgumentNullException.ThrowIfNull(teams);

        if (weeks < 1)
            throw new ArgumentOutOfRangeException(nameof(weeks));

        if (gamesPerTeam < 1)
            throw new ArgumentOutOfRangeException(nameof(gamesPerTeam));

        if (weekZeroGames < 0)
            throw new ArgumentOutOfRangeException(nameof(weekZeroGames));

        if (gamesPerTeam > weeks + (weekZeroGames > 0 ? 1 : 0))
        {
            throw new ArgumentOutOfRangeException(
                nameof(gamesPerTeam),
                "There are not enough calendar weeks for the requested number of games.");
        }

        var ordered = teams
            .GroupBy(team => team.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(team => SimulationSeed.Create(
                dynastyId,
                seasonYear,
                0,
                team.Name,
                "schedule-order"))
            .ThenBy(team => team.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (ordered.Length < 2)
            return Array.Empty<ScheduledGame>();

        if (gamesPerTeam >= ordered.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(gamesPerTeam),
                "A team cannot play more unique opponents than exist in the universe.");
        }

        var conferenceGames = BuildConferenceGames(
            ordered,
            dynastyId,
            seasonYear,
            gamesPerTeam);

        var nonConferenceGames = BuildNonConferenceGames(
            ordered,
            conferenceGames,
            dynastyId,
            seasonYear,
            gamesPerTeam);

        var drafts = conferenceGames
            .Concat(nonConferenceGames)
            .ToArray();

        ValidateGameCounts(ordered, drafts, gamesPerTeam);

        var oriented = OrientForBalancedHomeAway(ordered, drafts);
        var weekAssignments = AssignWeeks(
            oriented,
            dynastyId,
            seasonYear,
            weeks,
            weekZeroGames);

        return oriented
            .Select((game, index) =>
            {
                var week = weekAssignments[index];
                var seed = SimulationSeed.Create(
                    dynastyId,
                    seasonYear,
                    week,
                    game.HomeTeam.Name,
                    game.AwayTeam.Name);

                return new ScheduledGame
                {
                    GameId = $"{seasonYear}-{week:D2}-{seed:X8}",
                    SeasonYear = seasonYear,
                    Week = week,
                    HomeTeamName = game.HomeTeam.Name,
                    AwayTeamName = game.AwayTeam.Name,
                    GameType = game.GameType,
                    SimulationSeed = seed
                };
            })
            .OrderBy(game => game.Week)
            .ThenBy(game => game.HomeTeamName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(game => game.AwayTeamName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<DraftGame> BuildConferenceGames(
        IReadOnlyList<Team> teams,
        Guid dynastyId,
        int seasonYear,
        int gamesPerTeam)
    {
        var games = new List<DraftGame>();

        foreach (var conference in teams
                     .GroupBy(team => team.ConferenceName, StringComparer.OrdinalIgnoreCase)
                     .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            var members = conference
                .OrderBy(team => SimulationSeed.Create(
                    dynastyId,
                    seasonYear,
                    0,
                    team.Name,
                    $"conference-{conference.Key}"))
                .ThenBy(team => team.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var targetGames = GetLegacyConferenceGames(
                members.Length,
                gamesPerTeam);

            if (targetGames == 0)
                continue;

            var rotation = members.Cast<Team?>().ToList();
            if (rotation.Count % 2 != 0)
                rotation.Add(null);

            var rounds = Math.Min(
                targetGames,
                rotation.Count - 1);

            for (var round = 0; round < rounds; round++)
            {
                for (var pairing = 0; pairing < rotation.Count / 2; pairing++)
                {
                    var left = rotation[pairing];
                    var right = rotation[rotation.Count - 1 - pairing];

                    if (left is null || right is null)
                        continue;

                    games.Add(new DraftGame(
                        left,
                        right,
                        ScheduledGameType.Conference));
                }

                Rotate(rotation);
            }
        }

        return games;
    }

    private static IReadOnlyList<DraftGame> BuildNonConferenceGames(
        IReadOnlyList<Team> teams,
        IReadOnlyList<DraftGame> conferenceGames,
        Guid dynastyId,
        int seasonYear,
        int gamesPerTeam)
    {
        var baseCounts = teams.ToDictionary(
            team => team.Name,
            _ => 0,
            StringComparer.OrdinalIgnoreCase);

        var basePairs = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var game in conferenceGames)
        {
            baseCounts[game.TeamA.Name]++;
            baseCounts[game.TeamB.Name]++;
            basePairs.Add(PairKey(game.TeamA.Name, game.TeamB.Name));
        }

        for (var attempt = 0; attempt < MaximumPairingAttempts; attempt++)
        {
            var remaining = teams.ToDictionary(
                team => team.Name,
                team => gamesPerTeam - baseCounts[team.Name],
                StringComparer.OrdinalIgnoreCase);

            var pairs = new HashSet<string>(
                basePairs,
                StringComparer.OrdinalIgnoreCase);

            var result = new List<DraftGame>();
            var failed = false;

            while (remaining.Values.Any(value => value > 0))
            {
                var teamA = teams
                    .Where(team => remaining[team.Name] > 0)
                    .OrderByDescending(team => remaining[team.Name])
                    .ThenBy(team => SimulationSeed.Create(
                        dynastyId,
                        seasonYear,
                        attempt,
                        team.Name,
                        "ooc-team-a"))
                    .ThenBy(team => team.Name, StringComparer.OrdinalIgnoreCase)
                    .First();

                var teamB = teams
                    .Where(team =>
                        remaining[team.Name] > 0 &&
                        !team.Name.Equals(teamA.Name, StringComparison.OrdinalIgnoreCase) &&
                        !team.ConferenceName.Equals(
                            teamA.ConferenceName,
                            StringComparison.OrdinalIgnoreCase) &&
                        !pairs.Contains(PairKey(teamA.Name, team.Name)))
                    .OrderByDescending(team => remaining[team.Name])
                    .ThenBy(team => SimulationSeed.Create(
                        dynastyId,
                        seasonYear,
                        attempt,
                        $"{teamA.Name}|{team.Name}",
                        "ooc-team-b"))
                    .ThenBy(team => team.Name, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();

                if (teamB is null)
                {
                    failed = true;
                    break;
                }

                result.Add(new DraftGame(
                    teamA,
                    teamB,
                    ScheduledGameType.NonConference));

                pairs.Add(PairKey(teamA.Name, teamB.Name));
                remaining[teamA.Name]--;
                remaining[teamB.Name]--;
            }

            if (!failed)
                return result;
        }

        throw new InvalidOperationException(
            "Unable to construct a complete nonconference schedule.");
    }

    private static IReadOnlyList<OrientedDraftGame> OrientForBalancedHomeAway(
        IReadOnlyList<Team> teams,
        IReadOnlyList<DraftGame> games)
    {
        var adjacency = teams.ToDictionary(
            team => team.Name,
            _ => new List<int>(),
            StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < games.Count; index++)
        {
            adjacency[games[index].TeamA.Name].Add(index);
            adjacency[games[index].TeamB.Name].Add(index);
        }

        var used = new bool[games.Count];
        var oriented = new OrientedDraftGame?[games.Count];

        foreach (var start in teams)
        {
            while (adjacency[start.Name].Any(index => !used[index]))
            {
                var stack = new Stack<Team>();
                stack.Push(start);

                while (stack.Count > 0)
                {
                    var current = stack.Peek();
                    var nextEdge = -1;

                    foreach (var edgeIndex in adjacency[current.Name])
                    {
                        if (!used[edgeIndex])
                        {
                            nextEdge = edgeIndex;
                            break;
                        }
                    }

                    if (nextEdge < 0)
                    {
                        stack.Pop();
                        continue;
                    }

                    used[nextEdge] = true;
                    var draft = games[nextEdge];
                    var other = draft.TeamA.Name.Equals(
                        current.Name,
                        StringComparison.OrdinalIgnoreCase)
                        ? draft.TeamB
                        : draft.TeamA;

                    oriented[nextEdge] = new OrientedDraftGame(
                        current,
                        other,
                        draft.GameType);

                    stack.Push(other);
                }
            }
        }

        if (oriented.Any(game => game is null))
        {
            throw new InvalidOperationException(
                "Unable to orient every scheduled matchup.");
        }

        return oriented
            .Select(game => game!)
            .ToArray();
    }

    private static IReadOnlyDictionary<int, int> AssignWeeks(
        IReadOnlyList<OrientedDraftGame> games,
        Guid dynastyId,
        int seasonYear,
        int weeks,
        int requestedWeekZeroGames)
    {
        var fixedAssignments = new Dictionary<int, int>();
        var weekZeroTeams = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        if (requestedWeekZeroGames > 0)
        {
            foreach (var candidate in Enumerable.Range(0, games.Count)
                         .Where(index =>
                             games[index].GameType ==
                             ScheduledGameType.NonConference)
                         .OrderBy(index => SimulationSeed.Create(
                             dynastyId,
                             seasonYear,
                             0,
                             $"{games[index].HomeTeam.Name}|{games[index].AwayTeam.Name}",
                             "week-zero"))
                         .ThenBy(index => index))
            {
                if (fixedAssignments.Count >= requestedWeekZeroGames)
                    break;

                var game = games[candidate];

                if (weekZeroTeams.Contains(game.HomeTeam.Name) ||
                    weekZeroTeams.Contains(game.AwayTeam.Name))
                {
                    continue;
                }

                fixedAssignments[candidate] = 0;
                weekZeroTeams.Add(game.HomeTeam.Name);
                weekZeroTeams.Add(game.AwayTeam.Name);
            }
        }

        for (var attempt = 0;
             attempt < MaximumWeekAssignmentAttempts;
             attempt++)
        {
            var assignments = new Dictionary<int, int>(
                fixedAssignments);

            var teamWeeks = games
                .SelectMany(game => new[]
                {
                    game.HomeTeam.Name,
                    game.AwayTeam.Name
                })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    name => name,
                    _ => new HashSet<int>(),
                    StringComparer.OrdinalIgnoreCase);

            foreach (var fixedGame in fixedAssignments)
            {
                var game = games[fixedGame.Key];
                teamWeeks[game.HomeTeam.Name].Add(0);
                teamWeeks[game.AwayTeam.Name].Add(0);
            }

            var weekLoads = new int[weeks + 1];
            var unassigned = Enumerable.Range(0, games.Count)
                .Where(index => !fixedAssignments.ContainsKey(index))
                .ToHashSet();

            var failed = false;

            while (unassigned.Count > 0)
            {
                var bestIndex = -1;
                List<int>? bestAvailable = null;
                var bestAvailableCount = int.MaxValue;
                var bestOccupiedCount = int.MinValue;
                var bestTie = int.MaxValue;

                foreach (var index in unassigned)
                {
                    var game = games[index];
                    var available = Enumerable.Range(1, weeks)
                        .Where(week =>
                            !teamWeeks[game.HomeTeam.Name].Contains(week) &&
                            !teamWeeks[game.AwayTeam.Name].Contains(week))
                        .ToList();

                    var occupiedCount =
                        teamWeeks[game.HomeTeam.Name].Count +
                        teamWeeks[game.AwayTeam.Name].Count;

                    var tie = SimulationSeed.Create(
                        dynastyId,
                        seasonYear,
                        attempt,
                        $"{game.HomeTeam.Name}|{game.AwayTeam.Name}",
                        "week-edge-order");

                    if (available.Count < bestAvailableCount ||
                        (available.Count == bestAvailableCount &&
                         occupiedCount > bestOccupiedCount) ||
                        (available.Count == bestAvailableCount &&
                         occupiedCount == bestOccupiedCount &&
                         tie < bestTie) ||
                        (available.Count == bestAvailableCount &&
                         occupiedCount == bestOccupiedCount &&
                         tie == bestTie &&
                         index < bestIndex))
                    {
                        bestIndex = index;
                        bestAvailable = available;
                        bestAvailableCount = available.Count;
                        bestOccupiedCount = occupiedCount;
                        bestTie = tie;
                    }
                }

                if (bestIndex < 0 ||
                    bestAvailable is null ||
                    bestAvailable.Count == 0)
                {
                    failed = true;
                    break;
                }

                var bestGame = games[bestIndex];
                var selectedWeek = bestAvailable
                    .OrderBy(week => weekLoads[week])
                    .ThenBy(week =>
                        bestGame.GameType == ScheduledGameType.Conference
                            ? -week
                            : week)
                    .ThenBy(week => SimulationSeed.Create(
                        dynastyId,
                        seasonYear,
                        attempt * 100 + week,
                        $"{bestGame.HomeTeam.Name}|{bestGame.AwayTeam.Name}",
                        "week-choice"))
                    .First();

                assignments[bestIndex] = selectedWeek;
                teamWeeks[bestGame.HomeTeam.Name].Add(selectedWeek);
                teamWeeks[bestGame.AwayTeam.Name].Add(selectedWeek);
                weekLoads[selectedWeek]++;
                unassigned.Remove(bestIndex);
            }

            if (!failed)
                return assignments;
        }

        throw new InvalidOperationException(
            "Unable to place every game into the regular-season calendar.");
    }

    private static int GetLegacyConferenceGames(
        int conferenceSize,
        int gamesPerTeam)
    {
        if (conferenceSize < MinimumConferenceSize)
            return 0;

        var outOfConferenceGames =
            conferenceSize == 8 ? 5 : 4;

        return Math.Clamp(
            gamesPerTeam - outOfConferenceGames,
            0,
            Math.Min(gamesPerTeam, conferenceSize - 1));
    }

    private static void ValidateGameCounts(
        IReadOnlyList<Team> teams,
        IReadOnlyList<DraftGame> games,
        int gamesPerTeam)
    {
        foreach (var team in teams)
        {
            var count = games.Count(game =>
                game.TeamA.Name.Equals(
                    team.Name,
                    StringComparison.OrdinalIgnoreCase) ||
                game.TeamB.Name.Equals(
                    team.Name,
                    StringComparison.OrdinalIgnoreCase));

            if (count != gamesPerTeam)
            {
                throw new InvalidOperationException(
                    $"{team.Name} received {count} games instead of {gamesPerTeam}.");
            }
        }
    }

    private static string PairKey(string first, string second) =>
        string.Compare(
            first,
            second,
            StringComparison.OrdinalIgnoreCase) <= 0
            ? $"{first}\u001F{second}"
            : $"{second}\u001F{first}";

    private static void Rotate(List<Team?> rotation)
    {
        if (rotation.Count <= 2)
            return;

        var last = rotation[^1];
        rotation.RemoveAt(rotation.Count - 1);
        rotation.Insert(1, last);
    }

    private sealed record DraftGame(
        Team TeamA,
        Team TeamB,
        ScheduledGameType GameType);

    private sealed record OrientedDraftGame(
        Team HomeTeam,
        Team AwayTeam,
        ScheduledGameType GameType);
}

internal static class SimulationSeed
{
    public static int Create(
        Guid dynastyId,
        int seasonYear,
        int week,
        string first,
        string second)
    {
        var text =
            $"{dynastyId:N}|{seasonYear}|{week}|{first.Trim()}|{second.Trim()}";

        unchecked
        {
            uint hash = 2166136261;

            foreach (var character in text)
            {
                hash ^= character;
                hash *= 16777619;
            }

            return (int)(hash & 0x7FFFFFFF);
        }
    }
}
