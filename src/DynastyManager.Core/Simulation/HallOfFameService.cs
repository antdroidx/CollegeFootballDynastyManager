using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class HallOfFameService
{
    // Original Java Team.checkHallofFame thresholds and award weights. Use
    // recorded career production only; never invent stats for imported careers.
    public static DynastyState Induct(DynastyState state, IEnumerable<PlayerDepartureRecord> departures)
    {
        var entries = state.HallOfFame.ToList();
        var statistics = state.PlayerSeasonStats.Concat(DynastyHistoryService.BuildCurrentPlayerStats(state))
            .GroupBy(p => (p.PlayerId, p.SeasonYear, p.TeamName)).Select(g => g.Last()).ToArray();
        foreach (var departure in departures.Where(d => d.Reason is PlayerDepartureReason.Graduation or PlayerDepartureReason.EarlyProDeclaration))
        {
            if (entries.Any(e => e.PlayerId == departure.PlayerId)) continue;
            var lines = statistics.Where(p => p.PlayerId == departure.PlayerId).ToArray();
            var honors = state.PlayerAwardHistory.Where(a => a.PlayerId == departure.PlayerId && !a.IsPreseason)
                .DistinctBy(a => (a.SeasonYear, a.AwardName, a.ConferenceName, a.SelectionTeam)).ToArray();
            var awardScore = honors.Sum(a => a.ConferenceName is not null ? 10 : a.IsAllAmerican ? 25 :
                a.AwardName.Contains("Heisman", StringComparison.OrdinalIgnoreCase) ? 60 : 30);
            var years = Math.Max(1, lines.Select(l => l.SeasonYear).Distinct().Count());
            var score = (int)lines.Sum(l => Production(l)) + departure.OverallRating * 10 * years;
            var total = score + awardScore * 10;
            if (lines.Length == 0 && honors.Length == 0) continue;
            if (awardScore <= 99 && score <= 28000 && total <= 28750 && (years < 2 || total / years <= 7450)) continue;
            entries.Add(new HallOfFameEntry { PlayerId = departure.PlayerId, PlayerName = departure.FullName,
                TeamName = departure.TeamName, Position = departure.Position, InductionYear = departure.SeasonYear,
                CareerScore = score, HonorsScore = awardScore, RecordedSeasons = years,
                Citation = $"{years} recorded seasons · {honors.Length} final honors · career score {score:N0} · honors {awardScore}" });
        }
        return state with { HallOfFame = entries };
    }

    private static double Production(PlayerSeasonStatLine p) => p.Position switch
    {
        Position.QB => p.PassYards + p.PassingTouchdowns * 150 - p.InterceptionsThrown * 200 + p.RushYards * 3 + p.RushingTouchdowns * 150,
        Position.RB or Position.WR or Position.TE => p.RushYards * 2.65 + p.ReceivingYards * 2.5 + p.Receptions * 2 +
            (p.RushingTouchdowns + p.ReceivingTouchdowns + p.KickReturnTouchdowns + p.PuntReturnTouchdowns) * 150 +
            p.KickReturnYards + p.PuntReturnYards - (p.RushingFumbles + p.ReceivingFumbles) * 75,
        Position.DE or Position.DT or Position.OLB or Position.MLB or Position.CB or Position.FS or Position.SS =>
            p.Tackles * 25 + (p.Sacks + p.Interceptions + p.FumbleRecoveries) * 425 + p.PassesDefended * 25,
        Position.K => (p.FieldGoalsMade * 25 + p.ExtraPointsMade * 5) * (p.FieldGoalsAttempted == 0 ? 1 : p.FieldGoalsMade / (double)p.FieldGoalsAttempted) +
            p.PuntYards * .10 + p.PuntsInside20 * 25 + p.PuntsInside10 * 40,
        _ => 0 // OL candidacy uses honors; pancake/blocking production is not recorded.
    };
}
