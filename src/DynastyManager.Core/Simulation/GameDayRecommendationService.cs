using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public enum RecommendedCallType { Scrimmage, Punt, FieldGoal }
public sealed record GameDayRecommendation(RecommendedCallType Type, string PlayId,
    OffensivePlayDefinition? Play, string Reason, int KickDistance = 0, double KickProbability = 0);

public static class GameDayRecommendationService
{
    public static GameDayRecommendation Recommend(GameDaySession session, TeamGamePlan plan)
    {
        var home = session.PossessionTeamName.Equals(session.Game.HomeTeamName, StringComparison.OrdinalIgnoreCase);
        var margin = home ? session.HomeScore - session.AwayScore : session.AwayScore - session.HomeScore;
        var late = session.Quarter >= 4 && session.ClockSeconds <= 180;
        var needTouchdown = late && margin < -3;
        var fieldGoalChance = GameDayEngine.FieldGoalProbability(session);
        var kickDistance = 117 - session.YardLine;
        GameDayRecommendation FieldGoal(string reason) => new(RecommendedCallType.FieldGoal, "special-field-goal", null,
            reason, kickDistance, fieldGoalChance);
        if (session.ClockSeconds <= 8 && session.Quarter is 2 or 4 && !needTouchdown &&
            fieldGoalChance >= .50 && (session.Quarter == 2 || margin is >= -3 and <= 0))
            return FieldGoal("Use the remaining time to attempt points before the half ends.");
        if (session.Down == 4 && !needTouchdown)
        {
            if (fieldGoalChance >= .55 || late && margin is >= -3 and < 0 && fieldGoalChance >= .40)
                return FieldGoal(late && margin < 0 ? "A field goal can tie the game or take the lead."
                    : "The kicker is within a reasonable field-goal range.");
            if (!(late && margin < 0) && !(session.Distance <= 2 && session.YardLine >= 45))
                return new(RecommendedCallType.Punt, "special-punt", null, "Outside reliable field-goal range; protect field position on fourth down.");
        }
        var recent = session.Events.Where(e => e.OffenseTeamName == session.PossessionTeamName &&
                e.Outcome is not (SnapOutcome.Kickoff or SnapOutcome.Punt or SnapOutcome.FieldGoal or SnapOutcome.MissedFieldGoal))
            .TakeLast(4).Select(e => e.OffensivePlayId).ToArray();
        var hurryUp = late && margin < 0;
        var play = GamePlanService.RecommendOffensivePlay(plan, session.Down, session.Distance, session.YardLine,
            session.Events.Count, recent, session.Game.SimulationSeed, hurryUp);
        if (hurryUp && session.ClockSeconds <= 8 && session.YardLine >= 40)
            play = GamePlanService.AvailableOffensivePlays(plan).FirstOrDefault(p => p.Name == "Hail Mary") ?? play;
        var reason = needTouchdown ? "A field goal is not enough; keep the drive alive."
            : session.Down == 4 ? "Short fourth down in opponent territory; consider going for the first down."
            : hurryUp ? "Trailing late; favor the passing game."
            : session.Down >= 3 && session.Distance >= 6 ? "Attack the distance needed for a first down."
            : session.Distance <= 2 ? "Short yardage: favor the run while keeping complementary calls available."
            : "Mix the coach's preferred concepts and formations, with less weight on recent repeats.";
        return new(RecommendedCallType.Scrimmage, play.Id, play, reason);
    }
}
