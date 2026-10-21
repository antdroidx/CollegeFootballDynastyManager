using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public sealed record DynastyNewsStory(string Id, string Category, string Headline, string Body,
    string TeamName, int SeasonYear, int Week, Guid? PlayerId = null);

/// <summary>Deterministic stories derived from recorded events; no invented interviews or outcomes.</summary>
public static class DynastyNewsService
{
    private static int Variant(string value) => value.Aggregate(0, (total, character) => total + character) % 3;

    public static IReadOnlyList<DynastyNewsStory> BuildOverview(DynastyState state, int limit = 3)
    {
        if (limit <= 0) return [];
        var stories = Build(state, 48);
        var selected = new List<DynastyNewsStory>();
        void Add(DynastyNewsStory? story)
        {
            if (story is not null && selected.All(item => item.Id != story.Id)) selected.Add(story);
        }

        Add(stories.FirstOrDefault(story => story.Category is "Championships" or "Playoff" or "Upsets" or "Streaks" or "Shutouts" or "Blowouts" or "Bowls"));
        Add(stories.FirstOrDefault(story => story.TeamName == state.UserTeamName && story.Category != "Injuries"));
        foreach (var story in stories.Where(story => story.Category != "Injuries" || selected.All(item => item.Category != "Injuries")))
        {
            Add(story);
            if (selected.Count >= limit) break;
        }
        return selected.Take(limit).ToArray();
    }

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
            var earlier = state.Schedule.Where(g => g.SeasonYear == game.SeasonYear && g.Week < game.Week &&
                g.HasPlayed && g.HomeScore.HasValue && g.AwayScore.HasValue).ToArray();
            var winnerBefore = earlier.Where(g => g.InvolvesTeam(winner)).ToArray();
            var loserBefore = earlier.Where(g => g.InvolvesTeam(loser)).ToArray();
            var winnerRating = homeWon ? game.HomePregameRating : game.AwayPregameRating;
            var loserRating = homeWon ? game.AwayPregameRating : game.HomePregameRating;
            var strengthUpset = winnerRating.HasValue && loserRating.HasValue && loserRating - winnerRating >= 8;
            var recordUpset = !winnerRating.HasValue && !loserRating.HasValue && winnerBefore.Length >= 3 && loserBefore.Length >= 3 &&
                loserBefore.Count(g => g.HomeScore != g.AwayScore && g.WinnerTeamName == loser) / (double)loserBefore.Length >= .75 &&
                winnerBefore.Count(g => g.HomeScore != g.AwayScore && g.WinnerTeamName == winner) / (double)winnerBefore.Length <= .5;
            var upset = strengthUpset || recordUpset;
            var streak = winnerBefore.OrderByDescending(g => g.Week).TakeWhile(g => g.HomeScore != g.AwayScore && g.WinnerTeamName == winner).Count() + 1;
            var endedStreak = loserBefore.OrderByDescending(g => g.Week).TakeWhile(g => g.HomeScore != g.AwayScore && g.WinnerTeamName == loser).Count();
            var category = upset ? "Upsets" : low == 0 ? "Shutouts" : high - low >= 28 ? "Blowouts" : "Results";
            var verb = upset ? "upsets" : low == 0 ? "shuts out" : high - low >= 28 ? "rolls past" : high - low <= 7 ? "edges" : "beats";
            var headline = $"{winner} {verb} {loser}, {high}–{low}";
            if (upset)
                headline = Variant(game.GameId) switch
                {
                    0 => $"{winner} shocks {loser}, {high}–{low}",
                    1 => $"Upset alert: {winner} takes down {loser}, {high}–{low}",
                    _ => $"{winner} stuns {loser} in {high}–{low} upset"
                };
            var notable = strengthUpset ? $"The winner entered with a recorded team strength of {winnerRating:F1}, compared with {loserRating:F1} for {loser}. " :
                recordUpset ? "The winner entered at .500 or below against an opponent winning at least 75% of its earlier games. " : "";
            if (endedStreak >= 4) notable += $"The result ends {loser}'s {endedStreak}-game winning streak. ";
            if (streak >= 5)
            {
                notable += $"{winner} has won {streak} consecutive games this season. ";
                if (!upset && low != 0 && high - low < 28) { category = "Streaks"; headline = $"{winner} wins {streak} straight, beats {loser} {high}–{low}"; }
            }
            if (game.GameType == ScheduledGameType.ConferenceChampionship)
            { category = "Championships"; headline = $"{winner} wins conference title over {loser}, {high}–{low}"; }
            else if (game.GameType == ScheduledGameType.CollegeFootballPlayoff)
            { category = "Playoff"; headline = game.PostseasonRound == PostseasonRound.NationalChampionship ?
                $"{winner} wins national championship, beats {loser} {high}–{low}" : $"{winner} advances in playoff, beats {loser} {high}–{low}"; }
            else if (game.GameType == ScheduledGameType.Bowl)
            { category = "Bowls"; headline = $"{winner} wins {game.BowlName ?? "bowl matchup"}, beats {loser} {high}–{low}"; }
            stories.Add(new($"game/{game.GameId}", category, headline,
                $"{game.SeasonYear} · Week {game.Week}\n\n{winner} defeated {loser} {high}–{low}. " +
                notable + (game.IsNeutralSite ? "The matchup was played at a neutral site. " : $"The game was hosted by {game.HomeTeamName}. ") +
                (game.HomeStats is not null && game.AwayStats is not null ? $"{game.HomeTeamName} recorded {game.HomeStats.TotalYards} yards; {game.AwayTeamName} recorded {game.AwayStats.TotalYards}." : "The result is now recorded in the season schedule."),
                game.InvolvesTeam(state.UserTeamName) ? state.UserTeamName : winner, game.SeasonYear, game.Week));
        }
        foreach (var player in state.ActiveRoster.Where(p => p.CurrentInjury is not null))
        {
            var injury = player.CurrentInjury!;
            var injuryId = $"injury/{state.SeasonYear}/{state.Week}/{player.PlayerId}";
            var injuryHeadline = Variant(injuryId) switch
            {
                0 => $"{player.TeamName} monitors {player.FullName}'s recovery",
                1 => $"{player.FullName} works toward return for {player.TeamName}",
                _ => $"{player.TeamName} adjusts while {player.FullName} recovers"
            };
            stories.Add(new(injuryId, "Injuries", injuryHeadline,
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
        foreach (var incident in state.DisciplineHistory.Where(i => i.SeasonYear == state.SeasonYear).TakeLast(30))
            stories.Add(new($"conduct-{incident.SeasonYear}-{incident.Week}-{incident.TeamName}-{incident.EventId}", "Program conduct",
                $"{incident.TeamName}: {incident.Title}", incident.Description, incident.TeamName, incident.SeasonYear, incident.Week));
        foreach (var move in state.LeagueMovementHistory.Where(m => m.SeasonYear == state.SeasonYear))
            stories.Add(new($"move-{move.SeasonYear}-{move.TeamName}", "Realignment", $"{move.TeamName} joins {move.ToConference}",
                $"{move.TeamName} moves from {move.FromConference} to {move.ToConference}{(move.Promotion ? " as a new FBS member" : "")}. Conference strength and this season's schedule reflect the change.", move.TeamName, move.SeasonYear, 0));
        foreach (var award in state.PlayerAwardHistory.Where(a => a.SeasonYear == state.SeasonYear && !a.IsPreseason && !a.IsAllAmerican && a.ConferenceName is null))
            stories.Add(new($"award-{award.SeasonYear}-{award.AwardName}", "Awards", $"{award.PlayerName} wins the {award.AwardName}",
                $"{award.TeamName}'s {award.Position} earns national recognition. {award.Summary}.", award.TeamName, award.SeasonYear, state.Week, award.PlayerId));
        return stories.OrderByDescending(s => s.SeasonYear).ThenByDescending(s => s.Week)
            .ThenByDescending(s => s.Category is "Championships" or "Playoff" or "Upsets" or "Streaks" or "Shutouts" or "Blowouts" or "Bowls")
            .ThenByDescending(s => s.TeamName == state.UserTeamName)
            .ThenBy(s => s.Id, StringComparer.Ordinal)
            .DistinctBy(s => s.Id).Take(Math.Max(0, limit)).ToArray();
    }
}
