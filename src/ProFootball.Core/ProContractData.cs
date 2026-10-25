using System.Reflection;
using System.Text.Json;
namespace ProFootball.Core;

/// <summary>Source snapshot only applies to newly created real-team franchises.</summary>
public static class ProContractData
{
    public static IReadOnlyList<ProContract> Load()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ProFootball.Core.nfl-contracts.json")!;
        return JsonSerializer.Deserialize<List<ProContract>>(stream)!;
    }
    public static ProFranchise Apply(ProFranchise state)
    {
        if (state.Year != 2026) return state;
        var source = Load().ToDictionary(c => c.PlayerId);
        return state with { Contracts = state.Contracts.Select(c => source.TryGetValue(c.PlayerId, out var actual) && actual.Team == c.Team ? actual : c).ToList() };
    }
}
