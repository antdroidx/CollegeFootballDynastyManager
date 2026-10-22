using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class GameDayLivePresentation
{
    // Replays consume only a recorded event: no random calls or simulation steps.
    public static bool InstantReplay(GameSnapEvent snap) => snap.Outcome is SnapOutcome.Touchdown or SnapOutcome.Interception or
        SnapOutcome.Fumble or SnapOutcome.TwoPointConversion ||
        (GameDayEngine.IsScrimmage(snap) && snap.Yards >= 25) ||
        (snap.Outcome is SnapOutcome.Kickoff or SnapOutcome.Punt && snap.ReturnYards >= 30);

    public static GameFieldPoint HuddlePoint(int yardLine, int direction, bool offense, int slot)
    {
        var center = GameDayAnimationPlanner.ProjectYardLine(Math.Clamp(yardLine + (offense ? -9 : 9), 5, 95), direction);
        var angle = Math.PI * 2 * slot / 11;
        return new(Math.Clamp(center + Math.Cos(angle) * .026, .10, .90), .5 + Math.Sin(angle) * .12);
    }

    public static GameFieldPoint DeflectedBallPoint(GameFieldPoint contact, int direction, double progress, double catchProgress)
    {
        var p = Math.Clamp((progress - catchProgress) / Math.Max(.01, 1 - catchProgress), 0, 1);
        return new(Math.Clamp(contact.X + direction * .025 * p, .02, .98),
            Math.Clamp(contact.Y + .07 * p - Math.Sin(p * Math.PI) * .055, .04, .96));
    }
}
