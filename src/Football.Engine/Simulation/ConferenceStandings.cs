using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

/// <summary>
/// Builds regular-season conference standings. The legacy game primarily
/// sorted by conference wins, used head-to-head for a two-team tie, and used
/// national poll position for larger/unresolved ties.
/// </summary>
public static class ConferenceStandings
{
    public static IReadOnlyList<ConferenceStanding> Build(
        DynastyState state,
        IReadOnlyDictionary<string, Team> teamsByName,
        string conferenceName,
        IReadOnlyDictionary<string, int>? nationalRanks = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(teamsByName);

        var members = teamsByName.Values
            .Where(team => team.ConferenceName.Equals(
                conferenceName,
                StringComparison.OrdinalIgnoreCase))
            .OrderBy(team => team.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var accumulators = members.ToDictionary(
            team => team.Name,
            team => new Accumulator(team),
            StringComparer.OrdinalIgnoreCase);

        foreach (var game in state.Schedule.Where(game =>
                     game.SeasonYear == state.SeasonYear &&
                     game.HasPlayed &&
                     game.GameType != ScheduledGameType.ConferenceChampionship))
        {
            if (game.HomeScore is not int homeScore ||
                game.AwayScore is not int awayScore)
            {
                continue;
            }

            var homeIsMember = accumulators.TryGetValue(
                game.HomeTeamName,
                out var home);
            var awayIsMember = accumulators.TryGetValue(
                game.AwayTeamName,
                out var away);

            if (homeIsMember)
                RecordOverall(home!, homeScore > awayScore);

            if (awayIsMember)
                RecordOverall(away!, awayScore > homeScore);

            if (game.GameType != ScheduledGameType.Conference ||
                !homeIsMember ||
                !awayIsMember)
            {
                continue;
            }

            home!.ConferencePointsFor += homeScore;
            home.ConferencePointsAgainst += awayScore;
            away!.ConferencePointsFor += awayScore;
            away.ConferencePointsAgainst += homeScore;

            if (homeScore > awayScore)
            {
                home.ConferenceWins++;
                away.ConferenceLosses++;
            }
            else
            {
                away.ConferenceWins++;
                home.ConferenceLosses++;
            }
        }

        var standings = accumulators.Values
            .Select(value => new ConferenceStanding
            {
                TeamName = value.Team.Name,
                ConferenceName = value.Team.ConferenceName,
                ConferenceWins = value.ConferenceWins,
                ConferenceLosses = value.ConferenceLosses,
                OverallWins = value.OverallWins,
                OverallLosses = value.OverallLosses,
                ConferencePointsFor = value.ConferencePointsFor,
                ConferencePointsAgainst = value.ConferencePointsAgainst
            })
            .ToArray();

        var ordered = new List<ConferenceStanding>();

        foreach (var winsGroup in standings
                     .GroupBy(standing => standing.ConferenceWins)
                     .OrderByDescending(group => group.Key))
        {
            var tied = winsGroup.ToList();

            if (tied.Count == 2)
            {
                var headToHeadWinner = FindHeadToHeadWinner(
                    state,
                    tied[0].TeamName,
                    tied[1].TeamName);

                if (headToHeadWinner is not null)
                {
                    tied = tied
                        .OrderByDescending(standing =>
                            standing.TeamName.Equals(
                                headToHeadWinner,
                                StringComparison.OrdinalIgnoreCase))
                        .ThenBy(
                            standing => standing.TeamName,
                            StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    ordered.AddRange(tied);
                    continue;
                }
            }

            tied.Sort((left, right) =>
                CompareFallback(
                    left,
                    right,
                    teamsByName,
                    nationalRanks));
            ordered.AddRange(tied);
        }

        return ordered;
    }

    private static void RecordOverall(
        Accumulator accumulator,
        bool won)
    {
        if (won)
            accumulator.OverallWins++;
        else
            accumulator.OverallLosses++;
    }

    private static string? FindHeadToHeadWinner(
        DynastyState state,
        string first,
        string second)
    {
        var game = state.Schedule.FirstOrDefault(game =>
            game.SeasonYear == state.SeasonYear &&
            game.HasPlayed &&
            game.GameType == ScheduledGameType.Conference &&
            ((game.HomeTeamName.Equals(
                  first,
                  StringComparison.OrdinalIgnoreCase) &&
              game.AwayTeamName.Equals(
                  second,
                  StringComparison.OrdinalIgnoreCase)) ||
             (game.HomeTeamName.Equals(
                  second,
                  StringComparison.OrdinalIgnoreCase) &&
              game.AwayTeamName.Equals(
                  first,
                  StringComparison.OrdinalIgnoreCase))));

        return game?.WinnerTeamName;
    }

    private static int CompareFallback(
        ConferenceStanding left,
        ConferenceStanding right,
        IReadOnlyDictionary<string, Team> teamsByName,
        IReadOnlyDictionary<string, int>? nationalRanks)
    {
        if (nationalRanks is not null &&
            nationalRanks.TryGetValue(left.TeamName, out var leftRank) &&
            nationalRanks.TryGetValue(right.TeamName, out var rightRank))
        {
            var poll = leftRank.CompareTo(rightRank);
            if (poll != 0)
                return poll;
        }

        var overallWins = right.OverallWins.CompareTo(left.OverallWins);
        if (overallWins != 0)
            return overallWins;

        var pointDifferential =
            right.ConferencePointDifferential.CompareTo(
                left.ConferencePointDifferential);
        if (pointDifferential != 0)
            return pointDifferential;

        var leftPrestige = teamsByName.TryGetValue(
            left.TeamName,
            out var leftTeam)
            ? leftTeam.Prestige
            : 0;
        var rightPrestige = teamsByName.TryGetValue(
            right.TeamName,
            out var rightTeam)
            ? rightTeam.Prestige
            : 0;

        var prestige = rightPrestige.CompareTo(leftPrestige);
        if (prestige != 0)
            return prestige;

        return StringComparer.OrdinalIgnoreCase.Compare(
            left.TeamName,
            right.TeamName);
    }

    private sealed class Accumulator
    {
        public Accumulator(Team team)
        {
            Team = team;
        }

        public Team Team { get; }
        public int ConferenceWins { get; set; }
        public int ConferenceLosses { get; set; }
        public int OverallWins { get; set; }
        public int OverallLosses { get; set; }
        public int ConferencePointsFor { get; set; }
        public int ConferencePointsAgainst { get; set; }
    }
}
