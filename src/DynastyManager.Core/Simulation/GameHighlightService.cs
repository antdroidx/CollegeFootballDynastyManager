using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class GameHighlightService
{
    public static IReadOnlyList<ReplayPlayer> Participants(GameDaySession session)
    {
        var ids = Select(session.Events).SelectMany(e => e.OffensePersonnel.Concat(e.DefensePersonnel)).ToHashSet();
        return session.Roster.Where(p => ids.Contains(p.PlayerId)).Select(p => new ReplayPlayer(p.PlayerId, p.FullName, p.TeamName,
            p.Position, p.JerseyNumber, p.OverallRating, p.SpeedRating, p.AgilityRating)).ToArray();
    }
    public static IReadOnlyList<DynastyPlayer> ReplayRoster(ScheduledGame game) => game.HighlightPlayers.Select(p => new DynastyPlayer
    {
        PlayerId = p.PlayerId, FullName = p.FullName, TeamName = p.TeamName, Position = p.Position, JerseyNumber = p.JerseyNumber,
        OverallRating = p.OverallRating, SpeedRating = p.SpeedRating, AgilityRating = p.AgilityRating
    }).ToArray();
    public static IReadOnlyList<GameSnapEvent> Select(IEnumerable<GameSnapEvent> events) => events
        .Where(e => Value(e) > 0).OrderByDescending(Value).ThenByDescending(e => e.Quarter).ThenBy(e => e.ClockSeconds)
        .Take(5).OrderBy(e => e.Sequence).ToArray();

    public static double Value(GameSnapEvent e) => (e.Outcome switch
    {
        _ when e.Points >= 6 => 110,
        SnapOutcome.Touchdown => 100,
        SnapOutcome.Interception or SnapOutcome.Fumble or SnapOutcome.Turnover => 85,
        SnapOutcome.FieldGoal when e.Quarter >= 4 && e.ClockSeconds < 120 => 90,
        SnapOutcome.Sack => 25,
        _ => e.Yards >= 20 ? 35 : 0
    }) + Math.Max(0, e.Yards) * .3;

    public static IReadOnlyList<ScheduledGame> WeeklyGames(DynastyState state, int week) => state.Schedule
        .Where(g => g.SeasonYear == state.SeasonYear && g.Week == week && g.HasPlayed)
        .OrderByDescending(g => g.Highlights.Sum(Value) + UpsetValue(g) +
            (Math.Abs((g.HomeScore ?? 0) - (g.AwayScore ?? 0)) <= 8 ? 100 : 0) +
            (Math.Min(g.HomePregameRating ?? 0,g.AwayPregameRating ?? 0) >= 80 ? 50 : 0))
        .ThenBy(g => g.GameId).ToArray();

    public static bool IsUpset(ScheduledGame g) => g.HasPlayed && g.HomePregameRating.HasValue && g.AwayPregameRating.HasValue &&
        (g.WinnerTeamName == g.HomeTeamName ? g.AwayPregameRating - g.HomePregameRating : g.HomePregameRating - g.AwayPregameRating) >= 5;
    private static double UpsetValue(ScheduledGame g) => IsUpset(g) ? 150 : 0;
}
