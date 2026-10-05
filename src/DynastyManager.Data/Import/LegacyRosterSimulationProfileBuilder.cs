using DynastyManager.Core.Models;

namespace DynastyManager.Data.Import;

/// <summary>
/// Converts the legacy roster's imported talent values into stable team-level
/// simulation ratings. The legacy player generator used 60 + stars * 4.5
/// before randomized penalties; using 50 + stars * 4.5 approximates the
/// expected post-penalty level while keeping this first port deterministic.
/// </summary>
public static class LegacyRosterSimulationProfileBuilder
{
    public static IReadOnlyDictionary<string, TeamSimulationProfile> Build(
        IEnumerable<ImportedPlayerRow> players,
        IEnumerable<Team> teams)
    {
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(teams);

        var rosterByTeam = players
            .GroupBy(player => player.TeamName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.ToArray(),
                StringComparer.OrdinalIgnoreCase);

        var profiles = new Dictionary<string, TeamSimulationProfile>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var team in teams)
        {
            rosterByTeam.TryGetValue(team.Name, out var roster);
            roster ??= Array.Empty<ImportedPlayerRow>();

            var prestigeFallback = Math.Clamp(
                60.0 + (team.Prestige - 50) * 0.5,
                55.0,
                95.0);

            var rosterFallback = roster.Length == 0
                ? prestigeFallback
                : roster.Average(PlayerRating);

            var qb = AverageTop(roster, rosterFallback, 2, Position.QB);
            var rb = AverageTop(roster, rosterFallback, 3, Position.RB);
            var wr = AverageTop(roster, rosterFallback, 5, Position.WR);
            var te = AverageTop(roster, rosterFallback, 2, Position.TE);
            var ol = AverageTop(roster, rosterFallback, 7, Position.OL);
            var de = AverageTop(roster, rosterFallback, 4, Position.DE);
            var dt = AverageTop(roster, rosterFallback, 4, Position.DT);
            var olb = AverageTop(roster, rosterFallback, 4, Position.OLB);
            var mlb = AverageTop(roster, rosterFallback, 3, Position.MLB);
            var cb = AverageTop(roster, rosterFallback, 5, Position.CB);
            var safety = AverageTop(
                roster,
                rosterFallback,
                4,
                Position.FS,
                Position.SS);
            var kicker = AverageTop(roster, rosterFallback, 1, Position.K);

            profiles[team.Name] = new TeamSimulationProfile
            {
                TeamName = team.Name,
                PassOffenseRating =
                    qb * 0.30 +
                    wr * 0.25 +
                    te * 0.10 +
                    ol * 0.30 +
                    rb * 0.05,
                RushOffenseRating =
                    rb * 0.35 +
                    ol * 0.40 +
                    qb * 0.10 +
                    te * 0.15,
                PassDefenseRating =
                    cb * 0.30 +
                    safety * 0.20 +
                    de * 0.20 +
                    olb * 0.15 +
                    dt * 0.05 +
                    mlb * 0.10,
                RushDefenseRating =
                    dt * 0.20 +
                    de * 0.15 +
                    mlb * 0.25 +
                    olb * 0.20 +
                    safety * 0.15 +
                    cb * 0.05,
                SpecialTeamsRating =
                    kicker * 0.85 +
                    rosterFallback * 0.15,
                RosterSize = roster.Length
            };
        }

        return profiles;
    }

    private static double AverageTop(
        IReadOnlyCollection<ImportedPlayerRow> roster,
        double fallback,
        int count,
        params Position[] positions)
    {
        var allowed = positions.ToHashSet();
        var values = roster
            .Where(player => allowed.Contains(player.Position))
            .Select(PlayerRating)
            .OrderByDescending(value => value)
            .Take(count)
            .ToArray();

        return values.Length == 0 ? fallback : values.Average();
    }

    private static double PlayerRating(ImportedPlayerRow player) =>
        Math.Clamp(
            50.0 + player.LegacyTalentLevel * 4.5,
            50.0,
            99.0);
}
