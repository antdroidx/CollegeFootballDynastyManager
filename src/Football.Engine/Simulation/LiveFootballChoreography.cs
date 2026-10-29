using DynastyManager.Core.Models;
namespace DynastyManager.Core.Simulation;

public static class LiveFootballChoreography
{
    public static IReadOnlySet<Guid> AssistIds(GameSnapEvent snap)
    {
        var count=snap.Outcome switch{SnapOutcome.Sack=>0,SnapOutcome.Run when snap.Yards<0=>1,SnapOutcome.Run or SnapOutcome.Scramble or SnapOutcome.Completion or SnapOutcome.Fumble=>snap.Yards>=8?2:1,_=>0};
        return snap.TackleAttemptIds.Where(id=>id!=snap.PrimaryDefenderId).Take(count).ToHashSet();
    }
    public static GameFieldPoint DefensiveDestination(GameFieldPoint start,GameFieldPoint contact,double line,double destination,int index,bool primary,bool assist)
    {
        if(primary||assist)return GameDayAnimationPlanner.PursuitTarget(contact,index,primary);
        return new(Math.Clamp(start.X+(destination-line)*.72,.025,.96),Math.Clamp(start.Y+((index%3)-1)*.025,.07,.93));
    }

    public static bool Zone(DefensivePlayDefinition defense, PlayAssignment? assignment)
    {
        if (assignment is not null) return assignment.Task == AssignmentTask.Zone;
        var name = defense.Name;
        if (name.Contains("Man", StringComparison.OrdinalIgnoreCase) || name.Contains("Cover 0") || name.Contains("Cover 1")) return false;
        return name.Contains("Zone", StringComparison.OrdinalIgnoreCase) || name.Contains("Cover 2") || name.Contains("Cover 3") ||
            name.Contains("Cover 4") || name.Contains("Cover 6") || name.Contains("Quarters", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Tampa", StringComparison.OrdinalIgnoreCase) || defense.Approach == DefensiveApproach.ProtectPass;
    }

    public static string NotificationTeam(GameSnapEvent snap) => snap.Outcome is SnapOutcome.Interception or SnapOutcome.Fumble ||
        snap.Outcome == SnapOutcome.Punt && (snap.IsTouchback || snap.Points >= 6) ? snap.DefenseTeamName : snap.OffenseTeamName;

    public static int KickLandingYardLine(GameSnapEvent snap) => snap.IsTouchback
        ? snap.Outcome == SnapOutcome.Kickoff ? -3 : 103
        : snap.KickLandingYardLine > 0 ? snap.KickLandingYardLine : snap.BallEndYardLine;

    public static GameFieldPoint Coverage(GameFieldPoint start, GameFieldPoint receiver, double progress, bool zone, int direction)
    {
        var reaction = Math.Clamp(progress / .20, 0, 1);
        var lane = zone ? start.Y + (receiver.Y - start.Y) * .35 : receiver.Y;
        // Backpedal/turn and carry deep threats rather than stopping at the statistical gain.
        var x = direction > 0 ? Math.Max(start.X, receiver.X + 2 / 120d) : Math.Min(start.X, receiver.X - 2 / 120d);
        return new(Math.Clamp(start.X + (x - start.X) * reaction, .02, .98), Math.Clamp(start.Y + (lane - start.Y) * reaction, .06, .94));
    }

    public static (GameFieldPoint From, GameFieldPoint To) Kickoff(bool receiving, bool specialist, int slot, int landing, int end, int direction)
    {
        var lane = specialist ? .5 : .08 + slot % 10 * .093;
        var fromYard = receiving ? specialist ? landing : slot < 5 ? 43 : 25 : specialist ? 70 : 65;
        var toYard = receiving ? specialist ? end : Math.Clamp(end + 9 + slot % 3 * 4, 12, 62) : specialist ? 52 : Math.Clamp(end + (slot % 3) * 4, 1, 75);
        return (new(GameDayAnimationPlanner.ProjectFieldYardLine(fromYard, direction), lane),
            new(GameDayAnimationPlanner.ProjectFieldYardLine(toYard, direction), specialist && receiving ? .5 : lane));
    }
}
