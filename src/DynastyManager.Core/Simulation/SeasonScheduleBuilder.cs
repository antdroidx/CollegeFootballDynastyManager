using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class SeasonScheduleBuilder
{
    public const int DefaultRegularSeasonWeeks = 13;
    public const int DefaultGamesPerTeam = 12;

    public static IReadOnlyList<ScheduledGame> BuildRegularSeason(
        IEnumerable<Team> teams,
        Guid dynastyId,
        int seasonYear,
        int weeks = DefaultRegularSeasonWeeks,
        int gamesPerTeam = DefaultGamesPerTeam)
    {
        ArgumentNullException.ThrowIfNull(teams);

        if (weeks < 1)
            throw new ArgumentOutOfRangeException(nameof(weeks));

        if (gamesPerTeam < 1)
            throw new ArgumentOutOfRangeException(nameof(gamesPerTeam));

        var ordered = teams
            .GroupBy(team => team.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(team => SimulationSeed.Create(
                dynastyId,
                seasonYear,
                0,
                team.Name,
                "schedule"))
            .ThenBy(team => team.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (ordered.Count < 2)
            return Array.Empty<ScheduledGame>();

        Team? bye = null;
        var rotation = ordered.Cast<Team?>().ToList();
        if (rotation.Count % 2 != 0)
            rotation.Add(bye);

        var availableRounds = rotation.Count - 1;
        var rounds = Math.Min(
            Math.Min(weeks, gamesPerTeam),
            availableRounds);
        var games = new List<ScheduledGame>(rounds * rotation.Count / 2);

        for (var round = 0; round < rounds; round++)
        {
            var week = round + 1;

            for (var pairing = 0; pairing < rotation.Count / 2; pairing++)
            {
                var left = rotation[pairing];
                var right = rotation[rotation.Count - 1 - pairing];

                if (left is null || right is null)
                    continue;

                // Alternating the first pairing each round avoids a persistent
                // home/away bias for the fixed team in the circle method.
                var swap = pairing == 0
                    ? round % 2 != 0
                    : (round + pairing) % 2 != 0;

                var home = swap ? right : left;
                var away = swap ? left : right;
                var seed = SimulationSeed.Create(
                    dynastyId,
                    seasonYear,
                    week,
                    home.Name,
                    away.Name);

                games.Add(new ScheduledGame
                {
                    GameId = $"{seasonYear}-{week:D2}-{seed:X8}",
                    SeasonYear = seasonYear,
                    Week = week,
                    HomeTeamName = home.Name,
                    AwayTeamName = away.Name,
                    SimulationSeed = seed
                });
            }

            Rotate(rotation);
        }

        return games;
    }

    private static void Rotate(List<Team?> rotation)
    {
        if (rotation.Count <= 2)
            return;

        var last = rotation[^1];
        rotation.RemoveAt(rotation.Count - 1);
        rotation.Insert(1, last);
    }
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
