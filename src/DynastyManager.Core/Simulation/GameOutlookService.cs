using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class GameOutlookService
{
    public static GameOutlook Build(ScheduledGame game, TeamSimulationProfile? home, TeamSimulationProfile? away)
    {
        var homeRating = ((home?.OffenseRating ?? 70) + (home?.DefenseRating ?? 70)) / 2;
        var awayRating = ((away?.OffenseRating ?? 70) + (away?.DefenseRating ?? 70)) / 2;
        var margin = Math.Clamp((homeRating - awayRating) * .85 + (game.IsNeutralSite ? 0 : 2.5), -49, 49);
        var total = Math.Clamp(52 + ((home?.OffenseRating ?? 70) + (away?.OffenseRating ?? 70) - (home?.DefenseRating ?? 70) - (away?.DefenseRating ?? 70)) * .35, 28, 85);
        return new(margin >= 0 ? game.HomeTeamName : game.AwayTeamName, Math.Round(Math.Abs(margin) * 2) / 2,
            1 / (1 + Math.Exp(-margin / 10)), Math.Round(total * 2) / 2);
    }
}
