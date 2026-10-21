using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public sealed record PassingRead(DynastyPlayer? Target, DynastyPlayer? Defender, double Separation,
    double Protection, double ReleaseSeconds, double CatchChance, double InterceptionChance)
{
    public int RusherCount { get; init; }
}

/// <summary>Uses the actual package and assigned routes to choose a read before resolving the throw.</summary>
public static class PassingReadService
{
    public static PassingRead Read(OffensivePlayDefinition play, DefensivePlayDefinition defense,
        IReadOnlyList<DynastyPlayer> offense, IReadOnlyList<DynastyPlayer> defenders, DynastyPlayer? quarterback,
        int coaching, Random random)
    {
        var offenseRoles = new Dictionary<Guid, PlayAssignment?>(); var slotCounts = new Dictionary<Position, int>();
        foreach (var p in offense)
        { var role = slotCounts.GetValueOrDefault(p.Position); slotCounts[p.Position] = role + 1;
            offenseRoles[p.PlayerId] = play.Assignments.FirstOrDefault(a => a.Position == p.Position && a.RoleIndex == role); }
        var defenseRoles = new Dictionary<Guid, PlayAssignment?>(); slotCounts.Clear();
        foreach (var p in defenders)
        { var role = slotCounts.GetValueOrDefault(p.Position); slotCounts[p.Position] = role + 1;
            defenseRoles[p.PlayerId] = defense.Assignments.FirstOrDefault(a => a.Position == p.Position && a.RoleIndex == role); }
        var blockers = offense.Where(p => offenseRoles[p.PlayerId]?.Task == AssignmentTask.Block ||
            p.Position == Position.OL && offenseRoles[p.PlayerId]?.Task != AssignmentTask.Route).ToArray();
        var rushers = defenders.Where(p => defenseRoles[p.PlayerId] is { } assignment ? assignment.Task == AssignmentTask.Rush
            : p.Position is Position.DE or Position.DT || defense.Approach == DefensiveApproach.Pressure && p.Position is Position.MLB or Position.OLB).ToArray();
        double Mean(IEnumerable<DynastyPlayer> players, Func<DynastyPlayer, double> rating) => players.Select(rating).DefaultIfEmpty(60).Average();
        var protection = Mean(blockers, p => ((p.Skills?.PassBlocking ?? p.OverallRating) + p.StrengthRating) / 2d) -
            Mean(rushers, p => (p.StrengthRating + p.AgilityRating) / 2d) - Math.Max(0, rushers.Length - blockers.Length) * 5;
        var coverage = defenders.Where(p => !rushers.Contains(p) && p.Position is Position.CB or Position.FS or Position.SS or Position.OLB or Position.MLB).ToArray();
        var eligible = offense.Where(p => (p.Position is Position.WR or Position.TE or Position.RB) && offenseRoles[p.PlayerId]?.Task != AssignmentTask.Block).ToArray();
        if (GameDayAnimationPlanner.RouteFamily(play) == GameDayRouteFamily.Screen)
        {
            var receiverScreen = play.Name.Contains("Bubble", StringComparison.OrdinalIgnoreCase) || play.Name.Contains("WR", StringComparison.OrdinalIgnoreCase);
            var screens = eligible.Where(p => receiverScreen ? p.Position == Position.WR : p.Position == Position.RB).ToArray();
            if (screens.Length > 0) eligible = screens;
        }
        var counts = new Dictionary<Position, int>();
        var reads = eligible.Select((receiver, index) =>
        {
            var role = counts.GetValueOrDefault(receiver.Position); counts[receiver.Position] = role + 1;
            var route = GameDayAnimationPlanner.AssignedRoute(play, receiver.Position, role);
            var custom = offenseRoles[receiver.PlayerId];
            var lane = custom?.Lane ?? GameDayAnimationPlanner.Alignment(play.Formation, receiver.Position, role).Y;
            // Match outside receivers to corners; safeties and linebackers handle inside routes.
            var defender = coverage.OrderBy(p => receiver.Position == Position.WR
                    ? p.Position == Position.CB ? 0 : 1 : p.Position is Position.MLB or Position.OLB ? 0 : 1)
                .ThenBy(p => Math.Abs(p.DepthChartOrder - role - 1)).ElementAtOrDefault(index % Math.Max(1, coverage.Length));
            var defenderAssignment = defender is null ? null : defenseRoles[defender.PlayerId];
            var defenderAbility = defender is null ? 60 : (defender.SpeedRating + defender.AgilityRating + defender.AwarenessRating) / 3d;
            var routeAbility = (receiver.SpeedRating + receiver.AgilityRating + receiver.AwarenessRating) / 3d;
            var zones = defenderAssignment?.Task == AssignmentTask.Zone || defense.Approach == DefensiveApproach.ProtectPass || defense.Name.Contains("Cover", StringComparison.OrdinalIgnoreCase);
            var leverage = zones && route is GameDayRouteFamily.Curls or GameDayRouteFamily.Outs ? .35
                : !zones && route is GameDayRouteFamily.Mesh or GameDayRouteFamily.Slants ? .4 : 0;
            var spacing = Math.Abs(lane - .5) * .3;
            var separation = Math.Clamp(.9 + (routeAbility - defenderAbility) * .045 + leverage + spacing + random.NextDouble() * 1.4, .1, 4.5);
            return (Receiver: receiver, Defender: defender, Separation: separation, Role: role);
        }).OrderByDescending(r => r.Separation).ToArray();
        if (reads.Length == 0) return new(null, coverage.FirstOrDefault(), 0, protection, 2.5, .3, .04) { RusherCount = rushers.Length };
        var awareness = quarterback?.AwarenessRating ?? 60;
        var readIndex = random.NextDouble() < Math.Clamp((awareness + coaching) / 220d, .35, .9) ? 0 : random.Next(reads.Length);
        var chosen = reads[readIndex];
        var accuracy = quarterback?.Skills?.PassingAccuracy ?? quarterback?.OverallRating ?? 60;
        var hands = chosen.Receiver.Skills?.Catching ?? chosen.Receiver.OverallRating;
        var release = play.Concept == OffensiveConcept.QuickPass ? 1.6 : play.Concept == OffensiveConcept.PlayAction ? 2.8 : 3.1;
        release += readIndex * .3;
        var catchChance = Math.Clamp(.36 + chosen.Separation * .10 + (accuracy - 60) * .004 + (hands - 60) * .002 + protection * .0015
            - (play.Concept == OffensiveConcept.DeepPass ? .17 : 0), .08, .88);
        return new(chosen.Receiver, chosen.Defender, chosen.Separation, protection, release, catchChance,
            Math.Clamp(.045 - chosen.Separation * .009 + (65 - awareness) * .0004 - protection * .0002, .006, .09)) { RusherCount = rushers.Length };
    }
}
