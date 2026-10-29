using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public enum RecommendedCallType { Scrimmage, Punt, FieldGoal }
public sealed record GameDayRecommendation(RecommendedCallType Type, string PlayId,
    OffensivePlayDefinition? Play, string Reason, int KickDistance = 0, double KickProbability = 0);

public static class GameDayRecommendationService
{
    public static double PassingUrgency(GameDaySession session)
    {
        if (session.OvertimeRound > 0) return 1;
        var margin = session.PossessionTeamName == session.Game.HomeTeamName
            ? session.HomeScore - session.AwayScore : session.AwayScore - session.HomeScore;
        if (margin >= 0) return 1;
        var deficit = -margin;
        var remaining = Math.Max(1, (4 - session.Quarter) * 900 + session.ClockSeconds);
        // Score and time both matter: preserve an offense's identity early, accelerate when possessions run short.
        var possessionsNeeded = Math.Ceiling(deficit / 8d);
        var pressure = possessionsNeeded * 240 / remaining;
        var lateWeight = session.Quarter >= 4 ? 1.0 : session.Quarter == 3 ? .55 : .18;
        return Math.Clamp(1 + deficit / 7d * lateWeight + pressure * 2, 1, 10);
    }

    public static GameDayRecommendation Recommend(GameDaySession session, TeamGamePlan plan)
    {
        var home = session.PossessionTeamName.Equals(session.Game.HomeTeamName, StringComparison.OrdinalIgnoreCase);
        var margin = home ? session.HomeScore - session.AwayScore : session.AwayScore - session.HomeScore;
        var late = session.Quarter >= 4 && session.ClockSeconds <= 180;
        var needTouchdown = session.Quarter == 4 && margin < -3 &&
            session.ClockSeconds <= Math.Max(180, Math.Ceiling(-margin / 8d) * 150);
        var fieldGoalChance = GameDayEngine.FieldGoalProbability(session);
        var kickDistance = 117 - session.YardLine;
        GameDayRecommendation FieldGoal(string reason) => new(RecommendedCallType.FieldGoal, "special-field-goal", null,
            reason, kickDistance, fieldGoalChance);
        if (session.ClockSeconds <= 8 && session.Quarter is 2 or 4 && !needTouchdown &&
            fieldGoalChance >= .50 && (session.Quarter == 2 || margin is >= -3 and <= 0))
            return FieldGoal("Use the remaining time to attempt points before the half ends.");
        if (session.OvertimeRound == 0 && session.ClockSeconds <= 8 && session.ClockSeconds > 0 &&
            session.Quarter is 2 or 4 && session.YardLine >= 35 && (session.Quarter == 2 || margin <= 0))
        {
            var heave = GamePlanService.AvailableOffensivePlays(plan).FirstOrDefault(p => p.Name == "Hail Mary");
            if (heave is not null) return new(RecommendedCallType.Scrimmage, heave.Id, heave,
                "One final shot at the end zone before time expires.");
        }
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
            session.Events.Count, recent, session.Game.SimulationSeed, hurryUp, PassingUrgency(session)*session.Rules.BasePassingBias);
        var reason = needTouchdown ? "A field goal is not enough; keep the drive alive."
            : session.Down == 4 ? "Short fourth down in opponent territory; consider going for the first down."
            : hurryUp ? "Trailing late; favor the passing game."
            : PassingUrgency(session) >= 2 ? "The deficit and remaining time call for a stronger passing mix."
            : session.Down >= 3 && session.Distance >= 3 ? "Attack the distance needed for a first down."
            : session.Distance <= 2 ? "Short yardage: favor the run while keeping complementary calls available."
            : "Mix the coach's preferred concepts and formations, with less weight on recent repeats.";
        return new(RecommendedCallType.Scrimmage, play.Id, play, reason);
    }
}
