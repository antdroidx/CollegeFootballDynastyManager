using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public sealed record PenaltyPresentation(string TeamName, string Foul, int Yards)
{
    public string Banner => $"PENALTY\n{Yards} {(Yards == 1 ? "yard" : "yards")} {Foul}\n{TeamName}";
    public string Commentary => $"{Foul} on {TeamName}: {Yards} {(Yards == 1 ? "yard" : "yards")}.";
}

public static class GameDayCommentaryService
{
    public static PenaltyPresentation? Penalty(GameSnapEvent snap)
    {
        if (snap.Outcome != SnapOutcome.Penalty) return null;
        var delta = snap.BallEndYardLine - snap.StartYardLine;
        var team = snap.PenalizedTeamName ?? (delta > 0 ? snap.DefenseTeamName : delta < 0 ? snap.OffenseTeamName : "Team not recorded");
        var foul = snap.PenaltyName ?? (snap.Description?.Contains("offside", StringComparison.OrdinalIgnoreCase) == true ? "Offside" :
            snap.Description?.Contains("false start", StringComparison.OrdinalIgnoreCase) == true ? "False start" : "Penalty");
        return new(team, foul, snap.PenaltyYards ?? Math.Abs(delta));
    }

    public static string Describe(GameSnapEvent snap, IReadOnlyDictionary<Guid, DynastyPlayer> players)
    {
        string Name(Guid? id, string fallback) => id is Guid value && players.TryGetValue(value, out var player)
            ? player.FullName : fallback;
        var quarterback = Name(snap.QuarterbackId, "The quarterback");
        var carrier = Name(snap.BallCarrierId, "The ball carrier");
        var target = Name(snap.TargetId, "the intended receiver");
        var defender = Name(snap.PrimaryDefenderId, "the defense");
        var kicker = Name(snap.KickerId, "The kicker");
        var returner = Name(snap.ReturnerId, "the returner");
        var gain = YardPhrase(snap.Yards);

        return snap.Outcome switch
        {
            SnapOutcome.Kickoff when snap.Points >= 6 => $"{returner} takes the kickoff {snap.ReturnYards} yards for a TOUCHDOWN!",
            SnapOutcome.Kickoff when snap.IsOnsideKick => snap.KickingTeamRecovered ? $"{snap.DefenseTeamName} recovers the onside kick!" : $"{snap.OffenseTeamName} secures the onside kick.",
            SnapOutcome.Kickoff when snap.PrimaryDefenderId.HasValue =>
                $"{kicker} kicks to {returner}, who returns it {snap.ReturnYards} yards before {defender} makes the tackle.",
            SnapOutcome.Kickoff => $"{kicker} kicks to {returner}, who returns it {snap.ReturnYards} yards.",
            SnapOutcome.Punt when snap.IsTouchback => $"{kicker}'s punt carries into the end zone for a touchback.",
            SnapOutcome.Punt when snap.IsFairCatch => $"{kicker} punts to {returner}, who signals for a fair catch.",
            SnapOutcome.Punt => snap.ReturnYards > 0
                ? $"{kicker} punts to {returner}, who returns it {snap.ReturnYards} yards before {defender} makes the tackle."
                : $"{kicker} punts. {returner} is stopped without a return.",
            SnapOutcome.ExtraPoint => $"{kicker} makes the extra point.",
            SnapOutcome.MissedExtraPoint => $"{kicker} misses the extra point.",
            SnapOutcome.TwoPointConversion => "Two-point conversion is good.",
            SnapOutcome.FailedTwoPointConversion => "The two-point conversion is stopped.",
            SnapOutcome.Kneel => $"{quarterback} takes a knee.",
            SnapOutcome.Spike => $"{quarterback} spikes the ball to stop the clock.",
            SnapOutcome.ClockStoppage => snap.Description ?? "Clock stopped.",
            SnapOutcome.Penalty => Penalty(snap)!.Commentary,
            SnapOutcome.FieldGoal => $"{kicker} makes the field goal.",
            SnapOutcome.MissedFieldGoal => $"{kicker} misses the field goal.",
            SnapOutcome.Sack => $"{defender} sacks {quarterback} {YardPhrase(snap.Yards)}.",
            SnapOutcome.Interception => $"{defender} intercepts {quarterback}'s pass intended for {target}.",
            SnapOutcome.Fumble => $"{carrier} fumbles after {gain}; {defender} recovers for the defense.",
            SnapOutcome.Incompletion when snap.IsPassBreakup => $"{defender} breaks up {quarterback}'s pass intended for {target}.",
            SnapOutcome.Incompletion when snap.IsDrop => $"{target} drops {quarterback}'s pass. Incomplete.",
            SnapOutcome.Incompletion => $"{quarterback}'s pass for {target} is incomplete with {defender} in coverage.",
            SnapOutcome.Completion => $"{quarterback} completes to {target} {gain}; {defender} makes the tackle.",
            SnapOutcome.Touchdown when snap.IsQuarterbackScramble => $"{quarterback} escapes the pocket and scrambles {Math.Max(0, snap.Yards)} yards for a touchdown.",
            SnapOutcome.Touchdown when snap.TargetId.HasValue => $"{quarterback} finds {target} for a touchdown.",
            SnapOutcome.Touchdown => $"{carrier} scores on a {Math.Max(0, snap.Yards)}-yard run.",
            SnapOutcome.Scramble when snap.Yards < 0 => $"{quarterback} scrambles but {defender} stops him for a loss of {Math.Abs(snap.Yards)} yards.",
            SnapOutcome.Scramble => $"{quarterback} escapes the pocket and scrambles {gain} before {defender} makes the tackle.",
            SnapOutcome.Run when snap.Yards < 0 => $"{carrier} is stopped by {defender} for a loss of {Math.Abs(snap.Yards)} yards.",
            SnapOutcome.Run => $"{carrier} runs {gain} and is tackled by {defender}.",
            _ => $"{carrier} gains {snap.Yards} yards."
        };
    }

    private static string YardPhrase(int yards) => yards switch
    {
        > 0 => $"for {yards} yards",
        < 0 => $"for a loss of {Math.Abs(yards)} yards",
        _ => "for no gain"
    };
}
