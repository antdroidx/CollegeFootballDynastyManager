using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public readonly record struct GameFieldPoint(double X, double Y);
public enum GameDayRouteFamily { Run, Verticals, Mesh, Screen, Flood, Smash, StandardPass }

public static class GameDayAnimationPlanner
{
    public static GameFieldPoint ContactPoint(GameSnapEvent snap, int direction, double carrierLane)
    {
        var yardLine = snap.BallEndYardLine != 0 || snap.StartYardLine + snap.Yards == 0
            ? snap.BallEndYardLine : Math.Clamp(snap.StartYardLine + snap.Yards, 0, 100);
        return new GameFieldPoint(ProjectYardLine(yardLine, direction), Math.Clamp(carrierLane, .07, .93));
    }

    public static double ProjectYardLine(double yardLine, int direction)
    {
        // The rendered field is 120 yards wide: ten-yard end zones surround the 100-yard field of play.
        var leftToRight = (10 + Math.Clamp(yardLine, 0, 100)) / 120d;
        return direction == 1 ? leftToRight : 1 - leftToRight;
    }

    public static GameDayRouteFamily RouteFamily(OffensivePlayDefinition play)
    {
        if (play.Concept is OffensiveConcept.InsideRun or OffensiveConcept.OutsideRun) return GameDayRouteFamily.Run;
        if (Contains(play.Name, "Vertical", "All Go")) return GameDayRouteFamily.Verticals;
        if (Contains(play.Name, "Mesh", "Cross")) return GameDayRouteFamily.Mesh;
        if (Contains(play.Name, "Screen", "Bubble")) return GameDayRouteFamily.Screen;
        if (Contains(play.Name, "Flood", "Sail", "Boot")) return GameDayRouteFamily.Flood;
        if (Contains(play.Name, "Smash")) return GameDayRouteFamily.Smash;
        return GameDayRouteFamily.StandardPass;
    }

    public static double PassReleaseProgress(OffensivePlayDefinition play) => play.Concept switch
    {
        OffensiveConcept.QuickPass => RouteFamily(play) == GameDayRouteFamily.Screen ? .48 : .43,
        OffensiveConcept.PlayAction => .64,
        OffensiveConcept.DeepPass => .69,
        _ => .54
    };

    public static double ReturnProgress(double progress) => Math.Clamp((progress - .65) / .35, 0, 1);

    public static IReadOnlyList<Position> EligiblePositions(OffensivePersonnel personnel) => personnel switch
    {
        OffensivePersonnel.Empty => [Position.WR, Position.WR, Position.WR, Position.WR, Position.WR],
        OffensivePersonnel.Ten => [Position.RB, Position.WR, Position.WR, Position.WR, Position.WR],
        OffensivePersonnel.Twelve => [Position.RB, Position.WR, Position.WR, Position.TE, Position.TE],
        OffensivePersonnel.TwentyOne => [Position.RB, Position.RB, Position.WR, Position.WR, Position.TE],
        _ => [Position.RB, Position.WR, Position.WR, Position.WR, Position.TE]
    };

    public static double AlignmentLane(Position position, int roleIndex) => position switch
    {
        Position.RB => roleIndex % 2 == 0 ? .59 : .41,
        Position.TE => roleIndex % 2 == 0 ? .69 : .31,
        Position.WR => new[] { .10, .23, .77, .90, .84 }[Math.Clamp(roleIndex, 0, 4)],
        _ => .50
    };

    public static GameFieldPoint RoutePoint(GameFieldPoint start, GameFieldPoint end,
        GameDayRouteFamily family, double progress)
    {
        var p = Math.Clamp(progress, 0, 1);
        // Receivers release upfield before their break. Verticals retain their lane;
        // screens release immediately into the flat. Both art and live movement use this path.
        var laneProgress = family switch
        {
            GameDayRouteFamily.Verticals => 0,
            GameDayRouteFamily.Screen or GameDayRouteFamily.Run => p,
            _ => Math.Clamp((p - .35) / .65, 0, 1)
        };
        return new(start.X + (end.X - start.X) * p, start.Y + (end.Y - start.Y) * laneProgress);
    }

    public static double RouteEndLane(GameDayRouteFamily family, Position position, int eligibleIndex,
        double startLane, bool isTarget)
    {
        if (position is not (Position.WR or Position.TE or Position.RB)) return startLane;
        return family switch
        {
            GameDayRouteFamily.Verticals => startLane,
            GameDayRouteFamily.Mesh => eligibleIndex % 2 == 0
                ? Math.Clamp(1 - startLane, .16, .84) : Math.Clamp(.90 - startLane, .16, .84),
            GameDayRouteFamily.Screen =>
                startLane < .5 ? Math.Min(.38, startLane + .16) : Math.Max(.62, startLane - .16),
            GameDayRouteFamily.Flood => Math.Clamp(.16 + eligibleIndex * .07, .16, .38),
            GameDayRouteFamily.Smash => position == Position.WR ? startLane
                : startLane < .5 ? Math.Max(.12, startLane - .18) : Math.Min(.88, startLane + .18),
            GameDayRouteFamily.StandardPass => Math.Clamp(startLane + (eligibleIndex % 2 == 0 ? -.10 : .10), .10, .90),
            _ => startLane
        };
    }

    public static double RouteDepthShare(GameDayRouteFamily family, Position position, bool isTarget) =>
        isTarget ? 1 : family switch
        {
            GameDayRouteFamily.Verticals => position is Position.WR or Position.TE ? .90 : .30,
            GameDayRouteFamily.Mesh => position is Position.WR or Position.TE ? .55 : .28,
            GameDayRouteFamily.Screen => position == Position.RB ? .16 : .42,
            GameDayRouteFamily.Flood => position is Position.WR or Position.TE ? .72 : .28,
            GameDayRouteFamily.Smash => position is Position.WR or Position.TE ? .64 : .28,
            GameDayRouteFamily.StandardPass => position is Position.WR or Position.TE ? .65 : .30,
            _ => position == Position.QB ? .10 : .32
        };

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

    private static bool Contains(string value, params string[] terms) =>
        terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
}
