using DynastyManager.Core.Models;
namespace DynastyManager.Core.Simulation;

public static class OffensiveLineService
{
    public static string Label(DynastyPlayer p) => p.Position != Position.OL ? p.Position.ToString() : p.NcaaPositionCode switch
    { 5 or 9 => "OT", 6 or 8 => "OG", 7 => "OC", _ => "OL" };

    public static IReadOnlyList<DynastyPlayer> AssignMissingRoles(IEnumerable<DynastyPlayer> players)
    {
        var result = players.ToArray();
        var indices = result.Select((p, index) => (p.PlayerId, index)).GroupBy(p => p.PlayerId).ToDictionary(g => g.Key, g => g.First().index);
        foreach (var team in result.Where(p => p.Position == Position.OL).GroupBy(p => p.TeamName))
        {
            var counts = Enumerable.Range(5, 5).ToDictionary(c => c, c => team.Count(p => p.NcaaPositionCode == c));
            foreach (var p in team.Where(p => p.NcaaPositionCode is not (>= 5 and <= 9)).OrderByDescending(p => p.OverallRating).ThenBy(p => p.PlayerId))
            {
                var code = counts.OrderBy(c => c.Value).ThenBy(c => c.Key).First().Key;
                counts[code]++; var index = indices[p.PlayerId];
                result[index] = p with { NcaaPositionCode = code };
            }
        }
        return result;
    }

    // LT, LG, center, RG, RT. Role depth order controls starters; preserve imported side codes as source data.
    public static IReadOnlyList<DynastyPlayer> Lineup(IReadOnlyList<DynastyPlayer> pool)
    {
        var chosen = new List<DynastyPlayer>();
        foreach (var code in Enumerable.Range(5, 5))
        {
            var role = code is 5 or 9 ? "OT" : code == 7 ? "OC" : "OG";
            var next = pool.Where(p => p.Position == Position.OL && chosen.All(c => c.PlayerId != p.PlayerId))
                .OrderBy(p => Label(p) == role ? 0 : 1).FirstOrDefault();
            if (next is not null) chosen.Add(next);
        }
        return chosen;
    }
}
