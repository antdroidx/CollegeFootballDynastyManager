using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class GameDayCommentaryService
{
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
            SnapOutcome.Kickoff when snap.PrimaryDefenderId.HasValue =>
                $"{kicker} kicks to {returner}, who returns it {snap.ReturnYards} yards before {defender} makes the tackle.",
            SnapOutcome.Kickoff => $"{kicker} kicks to {returner}, who returns it {snap.ReturnYards} yards.",
            SnapOutcome.Punt when snap.IsTouchback => $"{kicker}'s punt carries into the end zone for a touchback.",
            SnapOutcome.Punt when snap.IsFairCatch => $"{kicker} punts to {returner}, who signals for a fair catch.",
            SnapOutcome.Punt => snap.ReturnYards > 0
                ? $"{kicker} punts to {returner}, who returns it {snap.ReturnYards} yards before {defender} makes the tackle."
                : $"{kicker} punts. {returner} is stopped without a return.",
            SnapOutcome.FieldGoal => $"{kicker} makes the field goal.",
            SnapOutcome.MissedFieldGoal => $"{kicker} misses the field goal.",
            SnapOutcome.Sack => $"{defender} sacks {quarterback} {YardPhrase(snap.Yards)}.",
            SnapOutcome.Interception => $"{defender} intercepts {quarterback}'s pass intended for {target}.",
            SnapOutcome.Fumble => $"{carrier} fumbles after {gain}; {defender} recovers for the defense.",
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
