using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public sealed record OpponentScoutingReport(int Wins, int Losses, int Games, double PointsFor, double PointsAgainst,
    double? PassShare, int Turnovers, IReadOnlyList<ScheduledGame> RecentGames, IReadOnlyList<DynastyPlayer> KeyPlayers);

public static class OpponentScoutingService
{
    public static OpponentScoutingReport Build(DynastyState state, string team)
    {
        var games = state.Schedule.Where(g => g.SeasonYear == state.SeasonYear && g.HasPlayed && g.HomeScore.HasValue && g.AwayScore.HasValue && g.InvolvesTeam(team))
            .OrderByDescending(g => g.Week).ToArray();
        int Own(ScheduledGame g) => g.HomeTeamName == team ? g.HomeScore.GetValueOrDefault() : g.AwayScore.GetValueOrDefault();
        int Against(ScheduledGame g) => g.HomeTeamName == team ? g.AwayScore.GetValueOrDefault() : g.HomeScore.GetValueOrDefault();
        var stats = games.Select(g => g.HomeTeamName == team ? g.HomeStats : g.AwayStats).OfType<GameTeamStats>().ToArray();
        var passes = stats.Sum(s => s.PassAttempts); var runs = stats.Sum(s => s.RushAttempts);
        return new(games.Count(g => Own(g) > Against(g)), games.Count(g => Own(g) < Against(g)), games.Length,
            games.Length == 0 ? 0 : games.Average(Own), games.Length == 0 ? 0 : games.Average(Against),
            passes + runs == 0 ? null : passes / (double)(passes + runs), stats.Sum(s => s.Turnovers), games.Take(3).ToArray(),
            state.ActiveRoster.Where(p => p.TeamName == team && p.CurrentInjury is null && !p.IsRedshirted)
                .OrderByDescending(p => p.OverallRating).DistinctBy(p => p.Position).Take(3).ToArray());
    }
}
