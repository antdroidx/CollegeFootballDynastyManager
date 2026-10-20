using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

public static class PlayerJerseyService
{
    public static IReadOnlyList<DynastyPlayer> Assign(IReadOnlyList<DynastyPlayer> roster)
    {
        var numbers = new Dictionary<Guid, int>();
        foreach (var team in roster.GroupBy(p => p.TeamName, StringComparer.OrdinalIgnoreCase))
        {
            // Imported duplicates are legal across offensive/defensive units; never rewrite explicit source numbers.
            var used = team.Where(p => p.JerseyNumber is >= 0 and <= 99).Select(p => p.JerseyNumber!.Value).ToHashSet();
            foreach (var player in team.Where(p => p.JerseyNumber is not (>= 0 and <= 99)).OrderBy(p => p.PlayerId))
            {
                var first = player.Position == Position.OL ? 50 : player.Position is Position.DT or Position.DE ? 90 : 0;
                var offset = player.PlayerId.ToByteArray()[0] % (first == 50 ? 30 : first == 90 ? 10 : 50);
                var preferred = first + offset;
                var number = Enumerable.Range(0, 100).Select(i => (preferred + i) % 100).FirstOrDefault(n => !used.Contains(n), preferred);
                numbers[player.PlayerId] = number; used.Add(number);
            }
        }
        return roster.Select(p => numbers.TryGetValue(p.PlayerId, out var number) ? p with { JerseyNumber = number } : p).ToArray();
    }
}
