using DynastyManager.Core.Models;
namespace DynastyManager.Core.Simulation;

public static partial class GameDayEngine
{
    /// <summary>Read-only snapshot; never finishes/saves the game or consumes simulation randomness.</summary>
    public static GameDayBoxScore BoxScore(GameDaySession session)
    {
        return new GameDayBoxScore
        {
            HomeTeamName = session.Game.HomeTeamName, AwayTeamName = session.Game.AwayTeamName,
            Weather = session.Weather,
            HomeMorale = GameMoraleService.InGame(session, session.Game.HomeTeamName), AwayMorale = GameMoraleService.InGame(session, session.Game.AwayTeamName),
            PlayerOfGame = session.IsComplete ? PlayerOfGameService.Select(session.Game with { HasPlayed=true, HomeScore=session.HomeScore, AwayScore=session.AwayScore }, BuildPlayerStats(session)) : null,
            HomeScore = session.HomeScore, AwayScore = session.AwayScore,
            Quarter = session.Quarter, ClockSeconds = session.ClockSeconds, IsComplete = session.IsComplete,
            HomeStats = BuildStats(session, session.Game.HomeTeamName), AwayStats = BuildStats(session, session.Game.AwayTeamName),
            Players = BuildPlayerStats(session),
            Periods = BuildPeriodScores(session)
        };
    }
    private static IReadOnlyList<GamePeriodScore> BuildPeriodScores(GameDaySession session)
    {
        int Points(GameSnapEvent s) => s.Outcome == SnapOutcome.Touchdown ? 6 : s.Outcome == SnapOutcome.FieldGoal ? 3 : s.Points;
        return Enumerable.Range(1, Math.Max(4, session.Quarter)).Select(q => new GamePeriodScore(q,
            session.Events.Where(s => s.Quarter == q && (s.DefensiveScore ? s.DefenseTeamName : s.OffenseTeamName) == session.Game.HomeTeamName).Sum(Points),
            session.Events.Where(s => s.Quarter == q && (s.DefensiveScore ? s.DefenseTeamName : s.OffenseTeamName) == session.Game.AwayTeamName).Sum(Points))).ToArray();
    }

}
