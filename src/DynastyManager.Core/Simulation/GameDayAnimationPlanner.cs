using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public readonly record struct GameFieldPoint(double X, double Y);
public enum GameDayRouteFamily { Run, Verticals, Mesh, Screen, Flood, Smash, Slants, Curls, Outs, Post, Corner, StandardPass }

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
        if (Contains(play.Name, "Slant")) return GameDayRouteFamily.Slants;
        if (Contains(play.Name, "Curl")) return GameDayRouteFamily.Curls;
        if (Contains(play.Name, "Out")) return GameDayRouteFamily.Outs;
        if (Contains(play.Name, "Post")) return GameDayRouteFamily.Post;
        if (Contains(play.Name, "Corner")) return GameDayRouteFamily.Corner;
        if (Contains(play.Name, "Vertical", "All Go", "Hail Mary")) return GameDayRouteFamily.Verticals;
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
        OffensivePersonnel.TripleBack => [Position.RB, Position.RB, Position.RB, Position.WR, Position.WR],
        OffensivePersonnel.Empty => [Position.WR, Position.WR, Position.WR, Position.WR, Position.WR],
        OffensivePersonnel.Ten => [Position.RB, Position.WR, Position.WR, Position.WR, Position.WR],
        OffensivePersonnel.Twelve => [Position.RB, Position.WR, Position.WR, Position.TE, Position.TE],
        OffensivePersonnel.TwentyOne => [Position.RB, Position.RB, Position.WR, Position.WR, Position.TE],
        _ => [Position.RB, Position.WR, Position.WR, Position.WR, Position.TE]
    };

    public static GameFieldPoint Alignment(OffensiveFormation formation, Position position, int roleIndex)
    {
        var lane = AlignmentLane(position, roleIndex);
        if (position == Position.QB)
            return new(formation switch { OffensiveFormation.Singleback or OffensiveFormation.IForm or OffensiveFormation.Flexbone => -1.5, OffensiveFormation.Pistol => -4, _ => -5 }, .50);
        if (position == Position.RB)
            return formation switch
            {
                OffensiveFormation.Singleback => new(-7, .50),
                OffensiveFormation.IForm => new(roleIndex == 0 ? -4 : -7, .50),
                OffensiveFormation.Pistol => new(-7, .50),
                OffensiveFormation.Flexbone => roleIndex == 0 ? new(-4, .50) : new(-2, roleIndex == 1 ? .27 : .73),
                _ => new(-5, lane)
            };
        return new(position == Position.TE ? -.5 : -.25, lane);
    }

    public static double PlaybackDurationSeconds(IEnumerable<(double Distance, int Speed, double ActiveFraction)> players)
    {
        var longest = players.Select(p => p.Distance / (6 + Math.Clamp(p.Speed, 1, 99) / 20d) /
            Math.Clamp(p.ActiveFraction, .1, 1)).DefaultIfEmpty(1).Max();
        return Math.Max(1.25, longest / 2.5);
    }

    public static double RouteLengthYards(GameFieldPoint from, GameFieldPoint to, GameDayRouteFamily family)
    {
        var distance = 0d;
        var previous = from;
        for (var i = 1; i <= 20; i++)
        {
            var next = RoutePoint(from, to, family, i / 20d);
            distance += DistanceYards(previous, next); previous = next;
        }
        return distance;
    }

    public static double DistanceYards(GameFieldPoint from, GameFieldPoint to) =>
        Math.Sqrt(Math.Pow((to.X - from.X) * 120, 2) + Math.Pow((to.Y - from.Y) * 53.333, 2));

    public static Guid? BallOwner(GameSnapEvent snap, double progress, double release)
    {
        if (snap.Outcome is SnapOutcome.Kickoff or SnapOutcome.Punt)
            return progress < .10 ? snap.KickerId : progress < .65 ? null : snap.ReturnerId;
        if (snap.Outcome is SnapOutcome.FieldGoal or SnapOutcome.MissedFieldGoal) return progress < .20 ? snap.KickerId : null;
        if (snap.IsQuarterbackKeep || snap.IsQuarterbackScramble || snap.Outcome == SnapOutcome.Sack) return snap.QuarterbackId;
        if (snap.TargetId.HasValue)
            return progress < release ? snap.QuarterbackId : progress < Math.Min(.90, release + .18) ? null :
                snap.Outcome == SnapOutcome.Interception ? snap.PrimaryDefenderId :
                snap.Outcome is SnapOutcome.Completion or SnapOutcome.Touchdown ? snap.TargetId : null;
        return progress < .25 ? snap.QuarterbackId : snap.BallCarrierId;
    }

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
        if (family == GameDayRouteFamily.Curls)
        {
            var depth = p <= 1.15 / 1.30 ? p * 1.30 : 1.15 - (p - 1.15 / 1.30) * 1.30;
            return new(start.X + (end.X - start.X) * depth, start.Y);
        }
        if (family == GameDayRouteFamily.Verticals) return new(start.X + (end.X - start.X) * p, start.Y);
        if (family is GameDayRouteFamily.Screen or GameDayRouteFamily.Run)
            return new(start.X + (end.X - start.X) * p, start.Y + (end.Y - start.Y) * p);
        var bend = new GameFieldPoint(start.X + (end.X - start.X) * .35, start.Y);
        var first = DistanceYards(start, bend);
        var second = DistanceYards(bend, end);
        var distance = p * (first + second);
        if (distance <= first && first > 0)
            return new(start.X + (bend.X - start.X) * distance / first, start.Y);
        var fraction = second > 0 ? Math.Clamp((distance - first) / second, 0, 1) : 1;
        return new(bend.X + (end.X - bend.X) * fraction, bend.Y + (end.Y - bend.Y) * fraction);
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
            GameDayRouteFamily.Slants or GameDayRouteFamily.Post => startLane < .5 ? Math.Min(.65, startLane + .30) : Math.Max(.35, startLane - .30),
            GameDayRouteFamily.Outs or GameDayRouteFamily.Corner => startLane < .5 ? Math.Max(.05, startLane - .20) : Math.Min(.95, startLane + .20),
            GameDayRouteFamily.Curls => startLane,
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
        return Math.Clamp(animationProgress, 0, 1);
    }

    private static bool Contains(string value, params string[] terms) =>
        terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
}
