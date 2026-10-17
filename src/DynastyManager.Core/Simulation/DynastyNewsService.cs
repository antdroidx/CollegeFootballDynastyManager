using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public sealed record DynastyNewsStory(string Id, string Category, string Headline, string Body,
    string TeamName, int SeasonYear, int Week, Guid? PlayerId = null);

/// <summary>Deterministic stories derived from recorded events; no invented interviews or outcomes.</summary>
public static class DynastyNewsService
{
    public static IReadOnlyList<DynastyNewsStory> Build(DynastyState state, int limit = 24)
    {
        var stories = new List<DynastyNewsStory>();
        foreach (var game in state.Schedule.Where(g => g.SeasonYear == state.SeasonYear && g.HasPlayed && g.HomeScore.HasValue && g.AwayScore.HasValue))
        {
            if (game.HomeScore == game.AwayScore)
            {
                stories.Add(new($"game/{game.GameId}", "Results", $"{game.HomeTeamName} and {game.AwayTeamName} finish level, {game.HomeScore}–{game.AwayScore}",
                    $"The recorded Week {game.Week} result is tied. Neither school is reported as the winner.",
                    game.HomeTeamName, game.SeasonYear, game.Week));
                continue;
            }
            var homeWon = game.HomeScore > game.AwayScore;
            var winner = homeWon ? game.HomeTeamName : game.AwayTeamName;
            var loser = homeWon ? game.AwayTeamName : game.HomeTeamName;
            var high = Math.Max(game.HomeScore!.Value, game.AwayScore!.Value);
            var low = Math.Min(game.HomeScore.Value, game.AwayScore.Value);
            var verb = low == 0 ? "shuts out" : high - low <= 7 ? "edges" : "beats";
            stories.Add(new($"game/{game.GameId}", "Results", $"{winner} {verb} {loser}, {high}–{low}",
                $"{game.SeasonYear} · Week {game.Week}\n\n{winner} defeated {loser} {high}–{low}. " +
                (game.IsNeutralSite ? "The matchup was played at a neutral site. " : $"The game was hosted by {game.HomeTeamName}. ") +
                (game.HomeStats is not null && game.AwayStats is not null ? $"{game.HomeTeamName} recorded {game.HomeStats.TotalYards} yards; {game.AwayTeamName} recorded {game.AwayStats.TotalYards}." : "The result is now recorded in the season schedule."),
                game.InvolvesTeam(state.UserTeamName) ? state.UserTeamName : winner, game.SeasonYear, game.Week));
        }
        foreach (var player in state.ActiveRoster.Where(p => p.CurrentInjury is not null))
        {
            var injury = player.CurrentInjury!;
            stories.Add(new($"injury/{state.SeasonYear}/{state.Week}/{player.PlayerId}", "Injuries",
                $"{player.TeamName} monitors {player.FullName}'s recovery",
                $"{player.Position} {player.FullName} is listed with a {injury.Severity.ToString().ToLowerInvariant()} {injury.BodyArea} injury. " +
                $"The current estimate is {injury.WeeksRemaining} week(s) remaining. Check the injury report for the latest roster status.",
                player.TeamName, state.SeasonYear, state.Week, player.PlayerId));
        }
        foreach (var commitment in state.RecruitingCommitments.Where(c => c.SeasonYear == state.SeasonYear))
            stories.Add(new($"commit/{commitment.SeasonYear}/{commitment.ProspectId}", "Recruiting",
                $"{commitment.PlayerName} commits to {commitment.TeamName}",
                $"{commitment.TeamName} secured a commitment from {commitment.Position} {commitment.PlayerName}. " +
                $"The {(commitment.Source == RecruitingSource.HighSchool ? "high school recruit" : "transfer")} is scheduled to join in {commitment.JoinSeasonYear}.",
                commitment.TeamName, commitment.SeasonYear, commitment.CommittedWeek));
        var next = state.Schedule.Where(g => g.SeasonYear == state.SeasonYear && !g.HasPlayed && g.InvolvesTeam(state.UserTeamName)).OrderBy(g => g.Week).FirstOrDefault();
        if (next is not null)
        {
            var opponent = next.HomeTeamName == state.UserTeamName ? next.AwayTeamName : next.HomeTeamName;
            stories.Add(new($"preview/{next.GameId}", "Preview", $"Next up: {state.UserTeamName} faces {opponent}",
                $"Week {next.Week} pairs {next.AwayTeamName} with {next.HomeTeamName}. Review the schedule, scout the matchup and prepare your game plan before advancing.",
                state.UserTeamName, state.SeasonYear, state.Week));
        }
        return stories.OrderByDescending(s => s.SeasonYear).ThenByDescending(s => s.Week)
            .ThenByDescending(s => s.TeamName == state.UserTeamName).ThenBy(s => s.Id, StringComparer.Ordinal)
            .DistinctBy(s => s.Id).Take(Math.Max(0, limit)).ToArray();
    }
}
