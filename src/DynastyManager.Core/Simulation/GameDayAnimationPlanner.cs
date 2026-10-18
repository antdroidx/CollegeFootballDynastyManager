using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public readonly record struct GameFieldPoint(double X, double Y);

public static class GameDayAnimationPlanner
{
    public static GameFieldPoint ContactPoint(GameSnapEvent snap, int direction, double carrierLane)
    {
        var yardLine = snap.BallEndYardLine != 0 || snap.StartYardLine + snap.Yards == 0
            ? snap.BallEndYardLine : Math.Clamp(snap.StartYardLine + snap.Yards, 0, 100);
        var x = direction == 1 ? yardLine / 100d : 1 - yardLine / 100d;
        return new GameFieldPoint(Math.Clamp(x, .025, .975), Math.Clamp(carrierLane, .07, .93));
    }

    public static GameFieldPoint PursuitTarget(GameFieldPoint contact, int defenderIndex, bool primaryDefender)
    {
        if (primaryDefender) return contact;
        var xOffset = (defenderIndex % 3 - 1) * .009;
        var yOffset = ((defenderIndex * 2) % 5 - 2) * .012;
        return new GameFieldPoint(Math.Clamp(contact.X + xOffset, .025, .975),
            Math.Clamp(contact.Y + yOffset, .07, .93));
    }

    public static double PursuitProgress(double animationProgress, bool primaryDefender)
    {
        var delay = primaryDefender ? .06 : .16;
        return Math.Clamp((animationProgress - delay) / (1 - delay), 0, 1);
    }
}
