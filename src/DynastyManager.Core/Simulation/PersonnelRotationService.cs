using DynastyManager.Core.Models;

namespace DynastyManager.Core.Simulation;

/// <summary>Selects an eligible eleven for each snap and maintains in-game energy.</summary>
public static class PersonnelRotationService
{
    public static SnapPersonnel SelectOffense(IReadOnlyList<DynastyPlayer> roster, string teamName,
        OffensivePersonnel package, GameFatigueState fatigue, Random random) =>
        Select(BuildDepthChart(roster, teamName), teamName, OffensiveNeeds(package), fatigue, random);

    public static SnapPersonnel SelectDefense(IReadOnlyList<DynastyPlayer> roster, string teamName,
        DefensivePersonnel package, GameFatigueState fatigue, Random random) =>
        Select(BuildDepthChart(roster, teamName), teamName, DefensiveNeeds(package), fatigue, random);

    public static SnapPersonnel SelectOffense(
        IReadOnlyDictionary<Position, IReadOnlyList<DynastyPlayer>> depthChart, string teamName,
        OffensivePersonnel package, GameFatigueState fatigue, Random random) =>
        Select(depthChart, teamName, OffensiveNeeds(package), fatigue, random);

    public static SnapPersonnel SelectDefense(
        IReadOnlyDictionary<Position, IReadOnlyList<DynastyPlayer>> depthChart, string teamName,
        DefensivePersonnel package, GameFatigueState fatigue, Random random) =>
        Select(depthChart, teamName, DefensiveNeeds(package), fatigue, random);

    public static IReadOnlyDictionary<Position, IReadOnlyList<DynastyPlayer>> BuildDepthChart(
        IReadOnlyList<DynastyPlayer> roster, string teamName) => roster
        .Where(player => player.TeamName.Equals(teamName, StringComparison.OrdinalIgnoreCase) &&
            !player.IsRedshirted && player.CurrentInjury is null)
        .GroupBy(player => player.Position)
        .ToDictionary(group => group.Key, group => (IReadOnlyList<DynastyPlayer>)group
            .OrderBy(player => player.DepthChartOrder <= 0 ? int.MaxValue : player.DepthChartOrder)
            .ThenByDescending(player => player.OverallRating).ToArray());

    public static void ApplySnap(GameFatigueState fatigue, IReadOnlyList<DynastyPlayer> roster,
        SnapPersonnel active, bool longPlay)
    {
        var activeIds = active.PlayerIds.ToHashSet();
        foreach (var player in roster)
        {
            var energy = fatigue.Energy(player.PlayerId);
            if (!activeIds.Contains(player.PlayerId))
            {
                fatigue.Set(player.PlayerId, energy + 2.5);
                continue;
            }

            var load = player.Position switch
            {
                Position.RB => 4.2, Position.WR or Position.TE => 3.5,
                Position.DE or Position.DT => 3.8, Position.OLB or Position.MLB => 2.7,
                Position.CB or Position.FS or Position.SS => 2.4,
                Position.OL => 1.25, Position.QB => 1.0, _ => 1.5
            };
            var stamina = player.Skills?.Stamina ?? player.DurabilityRating;
            var durabilityFactor = 1.25 - Math.Clamp((stamina + player.DurabilityRating) / 200d, .35, 1);
            fatigue.Set(player.PlayerId, energy - load * (1 + durabilityFactor) * (longPlay ? 1.3 : 1));
        }
    }

    private static SnapPersonnel Select(IReadOnlyDictionary<Position, IReadOnlyList<DynastyPlayer>> depthChart,
        string teamName,
        IReadOnlyList<(Position Position, int Count)> needs, GameFatigueState fatigue, Random random)
    {
        var selected = new List<Guid>(11);
        var substitutes = new List<Guid>();
        foreach (var (position, count) in needs)
        {
            var pool = depthChart.TryGetValue(position, out var players)
                ? players : Array.Empty<DynastyPlayer>();
            for (var slot = 0; slot < Math.Min(count, pool.Count); slot++)
            {
                var starter = pool[Math.Min(slot, pool.Count - 1)];
                var choice = starter;
                var nextIndex = slot + count;
                if (nextIndex < pool.Count && ShouldRotate(starter, fatigue.Energy(starter.PlayerId), random))
                    choice = pool[nextIndex];
                selected.Add(choice.PlayerId);
                if (choice.PlayerId != starter.PlayerId) substitutes.Add(choice.PlayerId);
            }
        }
        return new SnapPersonnel { TeamName = teamName, PlayerIds = selected, SubstitutedPlayerIds = substitutes };
    }

    private static bool ShouldRotate(DynastyPlayer player, double energy, Random random)
    {
        if (energy < EmergencyThreshold(player.Position)) return true;
        var baseChance = player.Position switch
        {
            Position.RB => .34, Position.WR => .22, Position.TE => .15,
            Position.DE or Position.DT => .32,
            Position.OLB or Position.MLB => .07,
            Position.CB or Position.FS or Position.SS => .05,
            Position.QB => .01, Position.OL => 0, _ => .02
        };
        var fatigueChance = Math.Max(0, 75 - energy) / 90;
        return random.NextDouble() < Math.Clamp(baseChance + fatigueChance, 0, .9);
    }

    private static double EmergencyThreshold(Position position) => position switch
    {
        Position.OL => 12, Position.QB => 15,
        Position.OLB or Position.MLB or Position.CB or Position.FS or Position.SS => 24,
        Position.DE or Position.DT => 36,
        _ => 32
    };

    private static (Position, int)[] OffensiveNeeds(OffensivePersonnel package) => package switch
    {
        OffensivePersonnel.Ten => [(Position.QB, 1), (Position.RB, 1), (Position.WR, 4), (Position.OL, 5)],
        OffensivePersonnel.Twelve => [(Position.QB, 1), (Position.RB, 1), (Position.WR, 2), (Position.TE, 2), (Position.OL, 5)],
        OffensivePersonnel.TwentyOne => [(Position.QB, 1), (Position.RB, 2), (Position.WR, 2), (Position.TE, 1), (Position.OL, 5)],
        OffensivePersonnel.Empty => [(Position.QB, 1), (Position.WR, 5), (Position.OL, 5)],
        _ => [(Position.QB, 1), (Position.RB, 1), (Position.WR, 3), (Position.TE, 1), (Position.OL, 5)]
    };

    private static (Position, int)[] DefensiveNeeds(DefensivePersonnel package) => package switch
    {
        DefensivePersonnel.BaseThreeFour => [(Position.DE, 2), (Position.DT, 1), (Position.OLB, 2), (Position.MLB, 2), (Position.CB, 2), (Position.FS, 1), (Position.SS, 1)],
        DefensivePersonnel.Nickel => [(Position.DE, 2), (Position.DT, 2), (Position.OLB, 1), (Position.MLB, 1), (Position.CB, 3), (Position.FS, 1), (Position.SS, 1)],
        DefensivePersonnel.Dime => [(Position.DE, 2), (Position.DT, 1), (Position.MLB, 1), (Position.CB, 4), (Position.FS, 2), (Position.SS, 1)],
        DefensivePersonnel.GoalLine => [(Position.DE, 2), (Position.DT, 3), (Position.OLB, 2), (Position.MLB, 2), (Position.CB, 2)],
        _ => [(Position.DE, 2), (Position.DT, 2), (Position.OLB, 2), (Position.MLB, 1), (Position.CB, 2), (Position.FS, 1), (Position.SS, 1)]
    };
}
