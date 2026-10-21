using DynastyManager.Core.Models;
namespace DynastyManager.Core.Simulation;

public sealed record PursuitDecision(DynastyPlayer? Tackler, IReadOnlyList<Guid> AttemptIds);

/// <summary>Ranks actual package defenders by estimated arrival, reaction and tackling ability.</summary>
public static class DefensivePursuitService
{
    // X is depth in yards relative to the line; Y is the fraction across the field.
    public static IReadOnlyDictionary<Guid, GameFieldPoint> Alignments(IReadOnlyList<DynastyPlayer> defenders, DefensivePlayDefinition play)
    {
        var roles = new Dictionary<Position, int>(); var line = 0; var linebackers = 0; var corners = 0; var safeties = 0;
        var result = new Dictionary<Guid, GameFieldPoint>();
        foreach (var player in defenders)
        {
            var role = roles.GetValueOrDefault(player.Position); roles[player.Position] = role + 1;
            var custom = play.Assignments.FirstOrDefault(a => a.Position == player.Position && a.RoleIndex == role);
            if (custom is not null) { result[player.PlayerId] = new(custom.Depth, custom.Lane); continue; }
            var goalLine = play.Formation == DefensiveFormation.GoalLine;
            var point = player.Position switch
            {
                Position.DE or Position.DT => new GameFieldPoint(goalLine ? .5 : 1, .34 + line++ % 5 * .08),
                Position.MLB or Position.OLB => new GameFieldPoint(goalLine ? 2 : 4.5, .28 + linebackers++ % 5 * .11),
                Position.CB => new GameFieldPoint(goalLine ? 2 : 5, corners++ % 2 == 0 ? .14 : .86),
                Position.FS or Position.SS => new GameFieldPoint(goalLine ? 5 : 13, safeties++ % 2 == 0 ? .32 : .68),
                _ => new GameFieldPoint(6, .5)
            };
            result[player.PlayerId] = point;
        }
        return result;
    }

    public static PursuitDecision Select(IReadOnlyList<DynastyPlayer> defenders, DefensivePlayDefinition play,
        double contactDepth, double lane, bool pass, bool sack, int airYards, double releaseSeconds,
        Guid? coverageDefender, double separation, Random random)
    {
        var starts = Alignments(defenders, play);
        var candidates = defenders.Where(p => p.CurrentInjury is null && !p.IsRedshirted && p.Position != Position.K).ToArray();
        if (sack)
        {
            var rushers = candidates.Where(p => p.Position is Position.DE or Position.DT ||
                play.Assignments.Any(a => a.Position == p.Position && a.Task == AssignmentTask.Rush) ||
                play.Approach == DefensiveApproach.Pressure && p.Position is Position.MLB or Position.OLB).ToArray();
            if (rushers.Length > 0) candidates = rushers;
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
