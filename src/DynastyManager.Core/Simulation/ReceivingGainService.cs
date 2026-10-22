using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public readonly record struct ReceivingGain(int AirYards, int YardsAfterCatch, bool BrokeTackle = false)
{
    public int TotalYards => AirYards + YardsAfterCatch;
}

public static class ReceivingGainService
{
    public static ReceivingGain Resolve(OffensivePlayDefinition play, DynastyPlayer receiver, DynastyPlayer? defender,
        double separation, double advantage, int start, Random random)
    {
        var family = GameDayAnimationPlanner.RouteFamily(play);
        var air = play.Name == "Hail Mary" ? 100 - start + random.Next(-5, 2)
            : family == GameDayRouteFamily.Screen ? random.Next(-2, 3)
            : play.Concept == OffensiveConcept.DeepPass ? random.Next(9, 24)
            : play.Concept == OffensiveConcept.PlayAction ? random.Next(2, 13) : random.Next(-1, 8);
        air = Math.Clamp(air, 1 - start, 100 - start);
        var elusiveness = (receiver.SpeedRating + receiver.AgilityRating + (receiver.Skills?.Acceleration ?? receiver.SpeedRating) +
            (receiver.Skills?.BreakTackle ?? receiver.OverallRating)) / 4d;
        var pursuit = defender is null ? 65 : (defender.SpeedRating + defender.AgilityRating +
            (defender.Skills?.Tackling ?? defender.OverallRating)) / 3d;
        // Most catches end promptly; space, missed tackles and speed supply a long tail.
        var yac = Math.Max(0, (int)Math.Round(random.NextDouble() * 5 + separation * .6 + (elusiveness - pursuit) * .07));
        var escape = Math.Clamp(.065 + (elusiveness - pursuit) * .003 + (separation - 2) * .02 + advantage * .0005, .015, .20);
        var brokeTackle = random.NextDouble() < escape;
        if (brokeTackle)
            yac += random.Next(9, 24) + (random.NextDouble() < .20 ? random.Next(15, 46) : 0);
        if (family == GameDayRouteFamily.Screen) yac += random.Next(0, 5);
        yac = Math.Clamp(yac, 0, 100 - start - air);
        return new(air, yac, brokeTackle);
    }
}
