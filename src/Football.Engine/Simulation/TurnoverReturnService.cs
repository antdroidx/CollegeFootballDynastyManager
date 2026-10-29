using DynastyManager.Core.Models;
namespace DynastyManager.Core.Simulation;

public static class TurnoverReturnService
{
    public static GameSnapEvent Resolve(GameSnapEvent snap, DynastyPlayer returner, IReadOnlyList<DynastyPlayer> offense, OffensiveConcept concept, Random random)
    {
        if (snap.Outcome is not (SnapOutcome.Interception or SnapOutcome.Fumble)) return snap;
        var interception = snap.Outcome == SnapOutcome.Interception;
        var depth = interception ? concept == OffensiveConcept.DeepPass ? random.Next(15, 40) : random.Next(2, 16) : Math.Max(0, snap.Yards);
        var spot = Math.Clamp(snap.StartYardLine + depth, 1, 100);
        var touchback = spot >= 100;
        var skill = (returner.SpeedRating + returner.AgilityRating + (returner.Skills?.BreakTackle ?? returner.OverallRating)) / 3d;
        var tackler = offense.OrderByDescending(p => p.SpeedRating + (p.Skills?.Tackling ?? p.OverallRating) * .3 + random.Next(0, 20)).FirstOrDefault();
        var pursuit = tackler?.SpeedRating ?? 70;
        // Many recoveries are downed in traffic; open-field returns have a long, rare tail.
        var yards = touchback || random.NextDouble() < (interception ? .25 : .62) ? 0 : Math.Max(1, (int)(random.Next(2, 19) + (skill - pursuit) * .3));
        if (yards > 0 && random.NextDouble() < Math.Clamp(.04 + (skill - pursuit) * .003, .01, .15)) yards += random.Next(20, 81);
        yards = Math.Min(spot, yards);
        return snap with { TurnoverYardLine = spot, BallEndYardLine = spot - yards, ReturnYards = yards,
            ReturnerId = returner.PlayerId, ReturnTacklerId = yards < spot && !touchback ? tackler?.PlayerId : null,
            IsTouchback = touchback, LiveBallSeconds = snap.LiveBallSeconds + (int)Math.Ceiling(yards / 7d), Points = yards >= spot ? 6 : 0 };
    }
}
