using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class DynastyRosterSimulationProfileBuilder
{
    public static IReadOnlyDictionary<string, TeamSimulationProfile> Build(
        DynastyState state,
        IEnumerable<Team> teams)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(teams);

        var teamArray = teams.ToArray();

        var rosterByTeam = state.ActiveRoster
            .Where(player =>
                !player.IsRedshirted &&
                player.CurrentInjury is null)
            .GroupBy(
                player => player.TeamName,
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.ToArray(),
                StringComparer.OrdinalIgnoreCase);

        var profiles = new Dictionary<string, TeamSimulationProfile>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var team in teamArray)
        {
            rosterByTeam.TryGetValue(
                team.Name,
                out var roster);

            roster ??= Array.Empty<DynastyPlayer>();

            var fallback = Math.Clamp(
                60.0 + (team.Prestige - 50) * .5,
                55.0,
                95.0);

            var qb = AverageDepth(
                roster,
                fallback,
                Position.QB);
            var rb = AverageDepth(
                roster,
                fallback,
                Position.RB);
            var wr = AverageDepth(
                roster,
                fallback,
                Position.WR);
            var te = AverageDepth(
                roster,
                fallback,
                Position.TE);
            var ol = AverageDepth(
                roster,
                fallback,
                Position.OL);
            var de = AverageDepth(
                roster,
                fallback,
                Position.DE);
            var dt = AverageDepth(
                roster,
                fallback,
                Position.DT);
            var olb = AverageDepth(
                roster,
                fallback,
                Position.OLB);
            var mlb = AverageDepth(
                roster,
                fallback,
                Position.MLB);
            var cb = AverageDepth(
                roster,
                fallback,
                Position.CB);
            var fs = AverageDepth(
                roster,
                fallback,
                Position.FS);
            var ss = AverageDepth(
                roster,
                fallback,
                Position.SS);
            var kicker = AverageDepth(
                roster,
                fallback,
                Position.K);

            var safety =
                (fs + ss) / 2.0;

            profiles[team.Name] = new TeamSimulationProfile
            {
                TeamName = team.Name,
                PassOffenseRating =
                    qb * .30 +
                    wr * .25 +
                    te * .10 +
                    ol * .30 +
                    rb * .05,
                RushOffenseRating =
                    rb * .35 +
                    ol * .40 +
                    qb * .10 +
                    te * .15,
                PassDefenseRating =
                    cb * .30 +
                    safety * .20 +
                    de * .20 +
                    olb * .15 +
                    dt * .05 +
                    mlb * .10,
                RushDefenseRating =
                    dt * .20 +
                    de * .15 +
                    mlb * .25 +
                    olb * .20 +
                    safety * .15 +
                    cb * .05,
                SpecialTeamsRating =
                    kicker * .85 +
                    fallback * .15,
                RosterSize = roster.Length
            };
        }

        return profiles;
    }

    private static double AverageDepth(
        IReadOnlyCollection<DynastyPlayer> roster,
        double fallback,
        Position position)
    {
        var count =
            DynastyRosterRules.StarterPositionCounts.TryGetValue(
                position,
                out var starterCount)
                ? starterCount
                : 1;

        var values = roster
            .Where(player =>
                player.Position == position)
            .OrderBy(player =>
                player.DepthChartOrder > 0
                    ? player.DepthChartOrder
                    : int.MaxValue)
            .ThenByDescending(player =>
                player.OverallRating)
            .Take(count)
            .Select(player =>
                (double)player.OverallRating)
            .ToArray();

        return values.Length == 0
            ? fallback
            : values.Average();
    }
}
