using DynastyManager.Core.Models;
namespace DynastyManager.Core.Simulation;

public static partial class GameDayEngine
{
    /// <summary>Read-only snapshot; never finishes/saves the game or consumes simulation randomness.</summary>
    public static GameDayBoxScore BoxScore(GameDaySession session)
    {
        int Points(GameSnapEvent snap) => snap.Outcome == SnapOutcome.Touchdown ? 6
            : snap.Outcome == SnapOutcome.FieldGoal ? 3 : snap.Points;
        return new GameDayBoxScore
        {
            HomeTeamName = session.Game.HomeTeamName, AwayTeamName = session.Game.AwayTeamName,
            HomeScore = session.HomeScore, AwayScore = session.AwayScore,
            Quarter = session.Quarter, ClockSeconds = session.ClockSeconds, IsComplete = session.IsComplete,
            HomeStats = BuildStats(session, session.Game.HomeTeamName), AwayStats = BuildStats(session, session.Game.AwayTeamName),
            Players = BuildPlayerStats(session),
            Periods = Enumerable.Range(1, Math.Max(4, session.Quarter)).Select(period => new GamePeriodScore(period,
                session.Events.Where(s => s.Quarter == period && s.OffenseTeamName == session.Game.HomeTeamName).Sum(Points),
                session.Events.Where(s => s.Quarter == period && s.OffenseTeamName == session.Game.AwayTeamName).Sum(Points))).ToArray()
        };
    }
}
