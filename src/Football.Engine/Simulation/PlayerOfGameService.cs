using DynastyManager.Core.Models;
namespace DynastyManager.Core.Simulation;

public static class PlayerOfGameService
{
    public static double Score(PlayerSeasonStatLine s) => s.PassYards * .035 + s.PassingTouchdowns * 5 - s.InterceptionsThrown * 4 +
        (s.RushYards + s.ReceivingYards) * .09 + (s.RushingTouchdowns + s.ReceivingTouchdowns) * 6 - s.RushingFumbles * 3 +
        s.Tackles * .35 + s.TacklesForLoss + s.Sacks * 3 + s.PassesDefended * 1.5 + s.Interceptions * 7 + s.FumbleRecoveries * 4 +
        s.FieldGoalsMade * 3 - (s.FieldGoalsAttempted-s.FieldGoalsMade) +
        (s.KickReturnTouchdowns+s.PuntReturnTouchdowns)*7 + (s.KickReturnYards+s.PuntReturnYards)*.025;
    public static PlayerGameAward? Select(ScheduledGame game, IReadOnlyList<PlayerSeasonStatLine> players)
    {
        if (!game.HasPlayed) return null;
        var best = players.Where(s => Score(s) > 0).OrderByDescending(s => Score(s) * (s.TeamName == game.WinnerTeamName ? 1.08 : 1))
            .ThenBy(s => s.TeamName,StringComparer.Ordinal).ThenBy(s => s.PlayerName,StringComparer.Ordinal).ThenBy(s => s.PlayerId).FirstOrDefault();
        if (best is null) return null;
        var parts = new List<string>();
        if (best.PassAttempts > 0) parts.Add($"{best.PassCompletions}/{best.PassAttempts}, {best.PassYards} pass yds, {best.PassingTouchdowns} TD");
        if (best.RushAttempts > 0) parts.Add($"{best.RushYards} rush yds, {best.RushingTouchdowns} TD");
        if (best.Receptions > 0) parts.Add($"{best.Receptions} rec, {best.ReceivingYards} yds, {best.ReceivingTouchdowns} TD");
        if (best.Tackles + best.Sacks + best.Interceptions + best.PassesDefended > 0) parts.Add($"{best.Tackles} tackles, {best.Sacks} sacks, {best.Interceptions} INT, {best.PassesDefended} PBU");
        if (best.FieldGoalsAttempted > 0) parts.Add($"{best.FieldGoalsMade}/{best.FieldGoalsAttempted} FG");
        if (best.KickReturns + best.PuntReturns > 0) parts.Add($"{best.KickReturnYards+best.PuntReturnYards} return yds");
        return new() { GameId=game.GameId, SeasonYear=game.SeasonYear, Week=game.Week, PlayerId=best.PlayerId, PlayerName=best.PlayerName,
            TeamName=best.TeamName, Position=best.Position, Summary=string.Join(" · ",parts.Take(3)) };
    }
    public static IReadOnlyList<PlayerGameAward> All(DynastyState state) => state.PlayerGameAwardHistory
        .Concat(state.Schedule.Where(g=>g.HasPlayed).Select(g=>g.PlayerOfGame ?? Select(g,g.PlayerStats)).OfType<PlayerGameAward>())
        .DistinctBy(a=>a.GameId).ToArray();
}
