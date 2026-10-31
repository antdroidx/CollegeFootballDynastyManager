using DynastyManager.Core.Models;
namespace DynastyManager.Core.Simulation;

public sealed record PursuitDecision(DynastyPlayer? Tackler, IReadOnlyList<Guid> AttemptIds);

/// <summary>Ranks actual package defenders by estimated arrival, reaction and tackling ability.</summary>
public static class DefensivePursuitService
{
    public static bool IsPassRusher(DefensivePlayDefinition? play, Position position, int roleIndex)
    {
        var assignment = play?.Assignments.FirstOrDefault(a => a.Position == position && a.RoleIndex == roleIndex);
        return assignment is not null ? assignment.Task == AssignmentTask.Rush :
            position is Position.DE or Position.DT ||
            play?.Approach == DefensiveApproach.Pressure && position is Position.MLB or Position.OLB;
    }

    // X is depth in yards relative to the line; Y is the fraction across the field.
    public static IReadOnlyDictionary<Guid, GameFieldPoint> Alignments(IReadOnlyList<DynastyPlayer> defenders, DefensivePlayDefinition play)
    {
        var roles = new Dictionary<Position, int>();
        var edges = 0; var tackles = 0; var corners = 0; var safeties = 0;
        var edgeCount = defenders.Count(p => p.Position == Position.DE);
        var tackleCount = defenders.Count(p => p.Position == Position.DT);
        var outsideCount = defenders.Count(p => p.Position == Position.OLB);
        var insideCount = defenders.Count(p => p.Position == Position.MLB);
        var linebackerCount = outsideCount + insideCount;
        var safetyCount = defenders.Count(p => p.Position is Position.FS or Position.SS);
        static double Spread(int index, int count, double low, double high) => count <= 1 ? .50 : low + (high-low)*index/(count-1);
        var result = new Dictionary<Guid, GameFieldPoint>();
        foreach (var player in defenders)
        {
            var role = roles.GetValueOrDefault(player.Position); roles[player.Position] = role + 1;
            var custom = play.Assignments.FirstOrDefault(a => a.Position == player.Position && a.RoleIndex == role);
            var goalLine = play.Formation == DefensiveFormation.GoalLine;
            var point = player.Position switch
            {
                Position.DE => new GameFieldPoint(goalLine ? .5 : 1, Spread(edges++, edgeCount, .30, .70)),
                Position.DT => new GameFieldPoint(goalLine ? .5 : 1, Spread(tackles++, tackleCount, .43, .57)),
                Position.MLB or Position.OLB => new GameFieldPoint(goalLine ? 2 : 4.5,
                    LinebackerLane(player.Position, role)),
                Position.CB => Corner(corners++, goalLine),
                Position.FS or Position.SS => new GameFieldPoint(goalLine ? 5 : 13, Spread(safeties++, safetyCount, .32, .68)),
                _ => new GameFieldPoint(6, .5)
            };
            result[player.PlayerId] = custom is null ? point : new(custom.Depth, custom.Lane);
        }
        return result;

        double LinebackerLane(Position position, int role)
        {
            var outside = position == Position.OLB;
            var count = outside ? outsideCount : insideCount;
            if (linebackerCount <= 2)
                return outsideCount == 1 && insideCount == 1 ? outside ? .40 : .60 : Spread(role, count, .40, .60);
            return Spread(role, count, outside ? .26 : .42, outside ? .74 : .58);
        }

        static GameFieldPoint Corner(int role, bool goalLine) => new(goalLine ? 2 : role < 2 ? 5 : 4,
            role switch { 0 => .14, 1 => .86, 2 => .34, 3 => .66, _ => .50 });
    }

    public static PursuitDecision Select(IReadOnlyList<DynastyPlayer> defenders, DefensivePlayDefinition play,
        double contactDepth, double lane, bool pass, bool sack, int airYards, double releaseSeconds,
        Guid? coverageDefender, double separation, Random random)
    {
        var starts = Alignments(defenders, play);
        var candidates = defenders.Where(p => p.CurrentInjury is null && !p.IsRedshirted && !p.Position.IsSpecialist()).ToArray();
        if (sack)
        {
            var roles = new Dictionary<Position, int>();
            var rusherIds = new HashSet<Guid>();
            foreach (var player in defenders)
            {
                var role = roles.GetValueOrDefault(player.Position); roles[player.Position] = role + 1;
                var rushing = IsPassRusher(play, player.Position, role);
                if (rushing) rusherIds.Add(player.PlayerId);
            }
            candidates = candidates.Where(p => rusherIds.Contains(p.PlayerId)).ToArray();
        }
        double Distance(GameFieldPoint a, GameFieldPoint b) => Math.Sqrt(Math.Pow(a.X-b.X,2)+Math.Pow((a.Y-b.Y)*53.333,2));
        var contact = new GameFieldPoint(contactDepth, lane);
        var ranked = candidates.Select(player =>
        {
            var start = starts[player.PlayerId]; var atRead = start;
            var speed = 4.2 + (player.SpeedRating + (player.Skills?.Acceleration ?? player.SpeedRating) + player.AgilityRating) / 3d * .055;
            var reaction = .15 + (99-player.AwarenessRating)*.006;
            var readTime = pass && !sack ? Math.Max(1.5, releaseSeconds) + Math.Max(0, airYards)/22d : 0;
            if (readTime > 0)
            {
                if (player.PlayerId == coverageDefender)
                    atRead = new(airYards - separation * .5, Math.Clamp(lane + separation/53.333, .05, .95));
                else if (player.Position is Position.CB or Position.FS or Position.SS)
                    atRead = start with { X = start.X + Math.Max(0, airYards-start.X)*.6 };
                else if (player.Position is Position.MLB or Position.OLB) atRead = start with {X=start.X+3};
            }
            var arrival = Math.Max(readTime, Distance(start,atRead)/speed) + Distance(atRead,contact)/speed + reaction;
            if (player.Position is Position.DE or Position.DT) arrival += .30; // Work through the blocking front.
            return (Player:player, Arrival:arrival);
        }).OrderBy(c=>c.Arrival).ThenBy(c=>c.Player.PlayerId).ToArray();
        if (ranked.Length == 0) return new(null, []);
        var fastest = ranked[0].Arrival;
        var possible = ranked.Where(c=>c.Arrival <= fastest+.65).Take(3).ToArray();
        double Weight((DynastyPlayer Player,double Arrival) candidate) =>
            (25+(candidate.Player.Skills?.Tackling ?? candidate.Player.OverallRating)) / Math.Pow(.35+candidate.Arrival-fastest,2);
        var choice = random.NextDouble()*possible.Sum(Weight); var tackler = possible[0].Player;
        foreach(var candidate in possible) { choice-=Weight(candidate); if(choice<=0) {tackler=candidate.Player;break;} }
        return new(tackler, ranked.Where(c=>c.Arrival<=fastest+1).Take(3).Select(c=>c.Player.PlayerId).ToArray());
    }
}
